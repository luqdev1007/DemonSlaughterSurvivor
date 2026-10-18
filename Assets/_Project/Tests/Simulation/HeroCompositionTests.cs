using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using UnityEditor;

namespace Game.Simulation.Tests
{
    public sealed class HeroCompositionTests
    {
        private const string DatabasePath = "Assets/_Project/Configs/ContentDatabase.asset";

        private EcsWorld _world;
        private EcsSystems _systems;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
        }

        [Test]
        public void TheHuntressIsBuiltFromHerModulesAlone()
        {
            int hero = Spawn("character.huntress");

            Assert.IsTrue(Has<TorsoAim>(hero), "the torso aim module");
            Assert.IsTrue(Has<BackleapStats>(hero), "the backleap module");
            Assert.IsTrue(Has<Height>(hero));
            Assert.IsFalse(Has<DashStats>(hero), "she has no dash");
            Assert.IsFalse(Has<UltimateCharge>(hero), "she has no ultimate resource");
            Assert.AreEqual(1, Count<Bow>(), "her bow is equipped");
            Assert.AreEqual(0, Count<Weapon>(), "and no sword");
        }

        [Test]
        public void TheBerserkKeepsHisKit()
        {
            int hero = Spawn("character.berserk");

            Assert.IsTrue(Has<DashStats>(hero));
            Assert.IsTrue(Has<UltimateCharge>(hero));
            Assert.IsTrue(Has<Height>(hero), "every hero has a height, on the ground");
            Assert.IsFalse(Has<TorsoAim>(hero));
            Assert.IsFalse(Has<BackleapStats>(hero));
            Assert.AreEqual(1, Count<Weapon>());
            Assert.AreEqual(0, Count<Bow>());
        }

        [Test]
        public void NoBerserkPerkIsOfferedToTheHuntress()
        {
            ContentRegistry registry = Registry();

            Assert.AreEqual(0, PerkPool.ForCharacter(registry.All<PerkConfig>(), "character.huntress").Count);
            Assert.AreEqual(registry.All<PerkConfig>().Count, PerkPool.ForCharacter(registry.All<PerkConfig>(), "character.berserk").Count);
        }

        private int Spawn(string characterId)
        {
            ContentRegistry registry = Registry();
            RunContext context = new RunContext("level.gameplay", characterId, RunMode.Story, 1, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systems.Add(new SpawnPlayerSystem());
            _systems.Add(new EquipStartingWeaponSystem());
            _systems.Add(new EquipBowSystem());
            _systems.Inject(context, (IContentRegistry)registry, (IViewFactory)new RecordingViewFactory(), new StatModifiers(_world));
            _systems.Init();

            foreach (int hero in _world.Filter<Player>().End())
                return hero;

            Assert.Fail("no hero was spawned");

            return -1;
        }

        private static ContentRegistry Registry()
        {
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            Assert.IsNotNull(database, DatabasePath);

            IContentEntry[] entries = new IContentEntry[database.Entries.Count];

            for (int index = 0; index < entries.Length; index++)
                entries[index] = database.Entries[index];

            return new ContentRegistry(entries);
        }

        private bool Has<T>(int entity) where T : struct
        {
            return _world.GetPool<T>().Has(entity);
        }

        private int Count<T>() where T : struct
        {
            return _world.Filter<T>().End().GetEntitiesCount();
        }
    }
}

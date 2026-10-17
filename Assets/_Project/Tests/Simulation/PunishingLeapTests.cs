using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class PunishingLeapTests
    {
        private const float Tick = 1f / 60f;
        private const string LeapPerkId = "perk.heroic-leap.test";
        private const string PunishPerkId = "perk.punishing-leap.test";
        private const float MaxHealth = 100f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private HeroicLeapConfig _leap;
        private PunishingLeapConfig _punish;
        private PerkConfig _leapPerk;
        private PerkConfig _punishPerk;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            _leap = Own(ScriptableObject.CreateInstance<HeroicLeapConfig>());

            _punish = Own(ScriptableObject.CreateInstance<PunishingLeapConfig>());
            PunishingLeapLevel level = new PunishingLeapLevel();
            SetField(level, "_chance", 1f);
            SetField(_punish, "_levels", new[] { level });

            _leapPerk = Perk(LeapPerkId, _leap);
            _punishPerk = Perk(PunishPerkId, _punish);
            SetField(_punishPerk, "_requires", new[] { _leapPerk });

            _world = new EcsWorld();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;

            SimulationClock clock = new SimulationClock();
            clock.Advance(Tick);

            _systems = new EcsSystems(_world);
            _systems.Add(new PunishLeapLandingSystem());
            _systems.Inject(
                (IContentRegistry)new ContentRegistry(new IContentEntry[] { _leapPerk, _punishPerk }),
                (IViewFactory)new NullViewFactory(),
                new RunContext("level.test", "character.test", RunMode.Story, 987654321, Array.Empty<StatModifierSpec>()),
                clock);
            _systems.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void ALeapHitLeavingTheEnemyUnderTheThresholdFinishesIt()
        {
            Take(LeapPerkId);
            Take(PunishPerkId);

            int enemy = SpawnEnemy(health: 40f);
            Hit(enemy, 20f, _leap);

            _systems.Run();

            DamageEvent finish = Single(_punish);

            Assert.IsTrue(finish.Target.Unpack(_world, out int target) && target == enemy);
            Assert.AreEqual(40f, finish.Amount, "the finishing blow takes the whole health left before the landing");
            Assert.AreEqual(DamageKind.Perk, finish.Kind);
            Assert.IsTrue(finish.IsSlash);
        }

        [Test]
        public void AnEnemyLeftAboveTheThresholdIsNotFinished()
        {
            Take(LeapPerkId);
            Take(PunishPerkId);

            Hit(SpawnEnemy(health: 60f), 20f, _leap);

            _systems.Run();

            Assert.AreEqual(0, Count(_punish), "40 of 100 left is above 25%");
        }

        [Test]
        public void ABlowThatAlreadyKillsNeedsNoFinish()
        {
            Take(LeapPerkId);
            Take(PunishPerkId);

            Hit(SpawnEnemy(health: 15f), 20f, _leap);

            _systems.Run();

            Assert.AreEqual(0, Count(_punish));
        }

        [Test]
        public void TheBossIsNeverFinished()
        {
            Take(LeapPerkId);
            Take(PunishPerkId);

            int boss = SpawnEnemy(health: 30f);
            _world.GetPool<Boss>().Add(boss);
            Hit(boss, 20f, _leap);

            _systems.Run();

            Assert.AreEqual(0, Count(_punish));
        }

        [Test]
        public void WithoutThePerkOrFromAnotherSourceNothingHappens()
        {
            Take(LeapPerkId);

            Hit(SpawnEnemy(health: 40f), 20f, _leap);

            _systems.Run();

            Assert.AreEqual(0, Count(_punish), "the leap alone does not finish");

            foreach (int entity in _world.Filter<DamageEvent>().End())
                _world.DelEntity(entity);

            Take(PunishPerkId);
            Hit(SpawnEnemy(health: 40f), 20f, null);

            _systems.Run();

            Assert.AreEqual(0, Count(_punish), "a weapon hit is not a leap landing");
        }

        [Test]
        public void ThePerkRefusesToExistWithoutALeapParent()
        {
            SetField(_punishPerk, "_requires", Array.Empty<PerkConfig>());

            Assert.Throws<InvalidOperationException>(() => _punish.Validate(_punishPerk));
        }

        private void Take(string perkId)
        {
            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perkId;
            taken.Level = 1;
        }

        private int SpawnEnemy(float health)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = new Vector3(0f, 0f, 1f);
            _world.GetPool<Health>().Add(enemy).Current = health;
            _world.GetPool<MaxHealth>().Add(enemy).Value = MaxHealth;

            return enemy;
        }

        private void Hit(int target, float amount, PerkBehaviourConfig source)
        {
            ref DamageEvent hit = ref _world.GetPool<DamageEvent>().Add(_world.NewEntity());
            hit.Target = _world.PackEntity(target);
            hit.Source = _world.PackEntity(_hero);
            hit.Amount = amount;
            hit.Kind = source == null ? DamageKind.Weapon : DamageKind.Perk;
            hit.SourcePerk = source;
        }

        private int Count(PerkBehaviourConfig source)
        {
            int count = 0;

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (_world.GetPool<DamageEvent>().Get(entity).SourcePerk == source)
                    count++;
            }

            return count;
        }

        private DamageEvent Single(PerkBehaviourConfig source)
        {
            Assert.AreEqual(1, Count(source));

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                DamageEvent damageEvent = _world.GetPool<DamageEvent>().Get(entity);

                if (damageEvent.SourcePerk == source)
                    return damageEvent;
            }

            return default;
        }

        private PerkConfig Perk(string id, PerkBehaviourConfig behaviour)
        {
            PerkConfig perk = Own(ScriptableObject.CreateInstance<PerkConfig>());
            SetField(perk, "_id", id);
            SetField(perk, "_levels", new[] { new PerkLevel() });
            SetField(perk, "_behaviour", behaviour);

            return perk;
        }

        private T Own<T>(T asset) where T : Object
        {
            _assets.Add(asset);

            return asset;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class NullViewFactory : IViewFactory
        {
            public IView Create(GameObject prefab, Vector3 position)
            {
                return null;
            }

            public void Release(IView view)
            {
            }

            public void Retire(IView view, float seconds, DeathBlow blow)
            {
            }
        }
    }
}

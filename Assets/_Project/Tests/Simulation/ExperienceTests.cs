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
    public sealed class ExperienceTests
    {
        private const int Seed = 987654321;
        private const float PickupRadius = 1.5f;
        private const int FlightTicks = 30;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<Fixture> _fixtures = new List<Fixture>();

        [TearDown]
        public void TearDown()
        {
            foreach (Fixture fixture in _fixtures)
                fixture.Dispose();

            _fixtures.Clear();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void ExperienceAccumulatesAndCarriesOverTheLevel()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 5);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(1, fixture.Level());
            Assert.AreEqual(5, fixture.Current());

            fixture.SpawnGem(new Vector3(0f, 0f, 1f), 4);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(2, fixture.Level(), "level 1 needs 4 + 3 x 1 = 7");
            Assert.AreEqual(2, fixture.Current(), "9 - 7 carries over");
        }

        [Test]
        public void SeveralLevelsInOneTickQueueOneChoicePerLevel()
        {
            Fixture fixture = Build(withPerks: true);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 30);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(4, fixture.Level(), "7 + 10 + 13 = 30 raises three levels");
            Assert.AreEqual(0, fixture.Current());
            Assert.AreEqual(3, fixture.PendingLevelUps());
            Assert.IsTrue(fixture.Gate.IsAwaiting);
        }

        [Test]
        public void SeveralGemsArrivingInOneTickAddTheirSum()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 1);
            fixture.SpawnGem(new Vector3(-1f, 0f, 0f), 3);
            fixture.SpawnGem(new Vector3(0f, 0f, 1f), 10);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(0, fixture.GemCount());
            Assert.AreEqual(2, fixture.Level(), "14 experience: 7 for level 1, 7 left of the 10 for level 2");
            Assert.AreEqual(7, fixture.Current());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GemArrivesExactlyAfterTheFlightTicksWhateverTheHeroDoes(int motion)
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1.2f, 0f, 0f), 1);
            fixture.Step();

            Assert.IsTrue(fixture.AnyGemInFlight(), "the flight starts on the tick the gem enters the radius");

            for (int tick = 1; tick <= FlightTicks; tick++)
            {
                Assert.AreEqual(0, fixture.Current(), $"experience arrived early, {tick - 1} ticks into the flight");

                fixture.MoveHero(HeroPosition(motion, tick));
                fixture.Step();
            }

            Assert.AreEqual(1, fixture.Current(), $"the gem must land exactly {FlightTicks} ticks after its flight starts");
            Assert.AreEqual(0, fixture.GemCount());
        }

        [Test]
        public void FlightIsNotInterruptedWhenTheHeroLeavesTheRadius()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 1);
            fixture.Step();
            fixture.MoveHero(new Vector3(30f, 0f, -20f));
            fixture.Step(FlightTicks);

            Assert.AreEqual(1, fixture.Current(), "a started flight follows the hero anywhere");
        }

        [Test]
        public void DeadHeroGetsNoExperienceAndTheGemStops()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 1);
            fixture.Step(5);

            fixture.World.GetPool<Dead>().Add(fixture.Hero);
            Vector3 frozen = fixture.FirstGemPosition();

            fixture.Step(FlightTicks * 2);

            Assert.AreEqual(0, fixture.Current());
            Assert.AreEqual(1, fixture.GemCount());
            Assert.AreEqual(frozen, fixture.FirstGemPosition(), "a gem whose hero died stands still");
        }

        [Test]
        public void PickupRadiusComesFromTheStat()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(2f, 0f, 0f), 1);
            fixture.Step();

            Assert.IsFalse(fixture.AnyGemInFlight(), "2 m is outside the 1.5 m base radius");

            fixture.Modifiers.Add(fixture.Hero, StatId.PickupRadius, StatOp.Flat, 1f, "test.magnet");
            fixture.Step();

            Assert.IsTrue(fixture.AnyGemInFlight(), "a +1 m modifier must reach the pickup check through the pipeline");
        }

        [Test]
        public void GemsDropInTheSameOrderOnOneSeed()
        {
            Fixture first = Build(withPerks: false);
            Fixture second = Build(withPerks: false);

            List<string> firstDrops = DropFive(first);
            List<string> secondDrops = DropFive(second);

            Assert.AreEqual(5, firstDrops.Count);
            CollectionAssert.AreEqual(firstDrops, secondDrops);
        }

        [Test]
        public void DroppedGemCarriesTheConfigExperienceAndReleasesItsView()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.KillEnemy(new Vector3(1f, 0f, 0f), withGem: true);
            fixture.Step();

            Assert.AreEqual(1, fixture.GemCount());
            Assert.AreEqual(1, fixture.Views.Created);

            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(3, fixture.Current(), "the test gem config gives 3");
            Assert.AreEqual(1, fixture.Views.Released, "the collected gem must return its view to the pool");
        }

        [Test]
        public void EnemyWithoutAGemDropsNothing()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.KillEnemy(new Vector3(1f, 0f, 0f), withGem: false);
            fixture.Step();

            Assert.AreEqual(0, fixture.GemCount());
            Assert.AreEqual(0, fixture.Views.Created);
        }

        [Test]
        public void GemsStayOutOfTheSpatialGrid()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(10f, 0f, 10f), 1);
            fixture.Step();

            List<int> found = new List<int>();
            fixture.Grid.Query(new Vector3(10f, 0f, 10f), 0.5f, 0f, found);

            Assert.AreEqual(0, found.Count);
        }

        [Test]
        public void DebugKeyRaisesTheLevelByOneAndOpensTheChoice()
        {
            Fixture fixture = Build(withPerks: true);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 3);
            fixture.Step(FlightTicks + 1);

            fixture.DebugInput.Pressed = true;
            fixture.Step();

            Assert.AreEqual(2, fixture.Level());
            Assert.AreEqual(0, fixture.Current(), "the key tops the bar up to the next level, no more");
            Assert.IsTrue(fixture.Gate.IsAwaiting);
        }

        [Test]
        public void EmptyPerkPoolKeepsLevelingWithoutAWindow()
        {
            Fixture fixture = Build(withPerks: false);

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 30);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(4, fixture.Level());
            Assert.IsFalse(fixture.Gate.IsAwaiting);
            Assert.AreEqual(0, fixture.PendingLevelUps());

            fixture.SpawnGem(new Vector3(1f, 0f, 0f), 16);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(5, fixture.Level(), "level 4 needs 16 and still levels up without perks");
        }

        private static Vector3 HeroPosition(int motion, int tick)
        {
            switch (motion)
            {
                case 1:
                    return new Vector3(tick * 0.125f, 0f, 0f);
                case 2:
                    return new Vector3((tick % 2 == 0 ? 1f : -1f) * 0.8f, 0f, tick * 0.05f);
                default:
                    return Vector3.zero;
            }
        }

        private static List<string> DropFive(Fixture fixture)
        {
            for (int index = 0; index < 5; index++)
                fixture.KillEnemy(new Vector3(5f + index, 0f, -3f + index * 0.5f), withGem: true);

            fixture.Step();

            List<string> drops = new List<string>();
            EcsPool<Position> positions = fixture.World.GetPool<Position>();

            foreach (int gem in fixture.World.Filter<Gem>().End())
                drops.Add(gem + "@" + positions.Get(gem).Value.ToString("R"));

            return drops;
        }

        private Fixture Build(bool withPerks)
        {
            ExperienceConfig experience = ScriptableObject.CreateInstance<ExperienceConfig>();
            SetField(experience, "_id", "experience.test");
            SetField(experience, "_baseRequired", 4);
            SetField(experience, "_stepRequired", 3);
            SetField(experience, "_flightSeconds", 0.5f);
            SetField(experience, "_flightPower", 2f);
            _assets.Add(experience);

            CharacterConfig character = ScriptableObject.CreateInstance<CharacterConfig>();
            SetField(character, "_id", "character.test");
            SetField(character, "_experience", experience);
            _assets.Add(character);

            GameObject gemPrefab = new GameObject("GemPrefab");
            _assets.Add(gemPrefab);

            GemConfig gem = ScriptableObject.CreateInstance<GemConfig>();
            SetField(gem, "_id", "gem.test");
            SetField(gem, "_viewPrefab", gemPrefab);
            SetField(gem, "_experience", 3);
            _assets.Add(gem);

            List<IContentEntry> entries = new List<IContentEntry> { character, gem };

            if (withPerks)
                entries.Add(Perk());

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            _assets.Add(level);

            Fixture fixture = new Fixture(new ContentRegistry(entries), level, gem);
            _fixtures.Add(fixture);

            return fixture;
        }

        private PerkConfig Perk()
        {
            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            SetField(perk, "_id", "perk.test");

            PerkLevel[] levels = new PerkLevel[5];

            for (int index = 0; index < levels.Length; index++)
            {
                StatModifierEntry entry = new StatModifierEntry();
                SetField(entry, "_stat", StatId.MaxHealth);
                SetField(entry, "_op", StatOp.Flat);
                SetField(entry, "_value", 10f * (index + 1));

                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", new[] { entry });
            }

            SetField(perk, "_levels", levels);
            _assets.Add(perk);

            return perk;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class Fixture : IDisposable
        {
            private const float TickSeconds = 1f / 60f;

            public readonly EcsWorld World;
            public readonly StatModifiers Modifiers;
            public readonly SpatialGrid Grid;
            public readonly UpgradeChoiceGate Gate = new UpgradeChoiceGate();
            public readonly FakeDebugInput DebugInput = new FakeDebugInput();
            public readonly FakeViewFactory Views = new FakeViewFactory();
            public readonly int Hero;

            private readonly GemConfig _gem;
            private readonly SimulationClock _clock = new SimulationClock();
            private readonly EcsSystems _systems;
            private readonly EcsSystems _choiceSystems;

            public Fixture(ContentRegistry registry, LevelConfig level, GemConfig gem)
            {
                _gem = gem;

                RunContext context = new RunContext("level.test", "character.test", RunMode.Story, Seed, Array.Empty<StatModifierSpec>());

                World = new EcsWorld();
                Modifiers = new StatModifiers(World);
                Grid = new SpatialGrid(World, 45f, 1f);

                object[] injects =
                {
                    context, registry, level, _clock, Modifiers, Grid, Gate, Views, DebugInput,
                    new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f), new NullChoiceInput(), new NullSink()
                };

                _systems = new EcsSystems(World);
                _systems.Add(new RecomputeStatsSystem());
                _systems.Add(new ApplyGemFlightSystem());
                _systems.Add(new MoveSystem());
                _systems.Add(new StartGemFlightSystem());
                _systems.Add(new DropGemsSystem());
                _systems.Add(new DebugLevelUpSystem());
                _systems.Add(new CollectGemsSystem());
                _systems.Add(new AdvanceLevelSystem());
                _systems.Add(new OfferUpgradesSystem());
                _systems.Add(new CleanupEventsSystem());
                _systems.Add(new RebuildSpatialGridSystem());
                _systems.Inject(injects);
                _systems.Init();

                _choiceSystems = new EcsSystems(World);
                _choiceSystems.Add(new ApplyUpgradeChoiceSystem());
                _choiceSystems.Inject(injects);
                _choiceSystems.Init();

                Hero = World.NewEntity();
                World.GetPool<Player>().Add(Hero);
                World.GetPool<Position>().Add(Hero).Value = Vector3.zero;
                World.GetPool<Velocity>().Add(Hero).Value = Vector3.zero;

                ref PickupRadius radius = ref World.GetPool<PickupRadius>().Add(Hero);
                radius.Base = PickupRadius;
                radius.Value = PickupRadius;

                ref Experience experience = ref World.GetPool<Experience>().Add(Hero);
                experience.Level = 1;
                experience.Current = 0;

                Modifiers.MarkDirty(Hero);
            }

            public void Step(int ticks = 1)
            {
                for (int tick = 0; tick < ticks; tick++)
                {
                    if (Gate.IsAwaiting)
                        return;

                    _clock.Advance(TickSeconds);
                    _systems.Run();
                }
            }

            public void MoveHero(Vector3 position)
            {
                World.GetPool<Position>().Get(Hero).Value = position;
            }

            public void SpawnGem(Vector3 position, int experience)
            {
                int gem = World.NewEntity();
                World.GetPool<Gem>().Add(gem).Experience = experience;
                World.GetPool<Position>().Add(gem).Value = position;
                World.GetPool<Velocity>().Add(gem).Value = Vector3.zero;
            }

            public void KillEnemy(Vector3 position, bool withGem)
            {
                int enemy = World.NewEntity();
                World.GetPool<Enemy>().Add(enemy);
                World.GetPool<Position>().Add(enemy).Value = position;
                World.GetPool<DiedEvent>().Add(enemy);

                if (withGem)
                    World.GetPool<GemDrop>().Add(enemy).Config = _gem;
            }

            public int Level()
            {
                return World.GetPool<Experience>().Get(Hero).Level;
            }

            public int Current()
            {
                return World.GetPool<Experience>().Get(Hero).Current;
            }

            public int PendingLevelUps()
            {
                EcsPool<PendingLevelUps> pool = World.GetPool<PendingLevelUps>();

                return pool.Has(Hero) ? pool.Get(Hero).Count : 0;
            }

            public int GemCount()
            {
                int count = 0;

                foreach (int gem in World.Filter<Gem>().End())
                    count++;

                return count;
            }

            public bool AnyGemInFlight()
            {
                foreach (int gem in World.Filter<Gem>().Inc<GemFlight>().End())
                    return true;

                return false;
            }

            public Vector3 FirstGemPosition()
            {
                foreach (int gem in World.Filter<Gem>().End())
                    return World.GetPool<Position>().Get(gem).Value;

                throw new InvalidOperationException("fixture: no gem");
            }

            public void Dispose()
            {
                _choiceSystems.Destroy();
                _systems.Destroy();
                World.Destroy();
            }
        }

        private sealed class FakeDebugInput : IDebugLevelUpInput
        {
            public bool Pressed;

            public bool ConsumePressed()
            {
                bool pressed = Pressed;
                Pressed = false;

                return pressed;
            }
        }

        private sealed class NullChoiceInput : IUpgradeChoiceInput
        {
            public bool TryConsume(out int index)
            {
                index = -1;

                return false;
            }

            public void Clear()
            {
            }
        }

        private sealed class NullSink : IUpgradeChoiceSink
        {
            public void Show(UpgradeOffer first, UpgradeOffer second, UpgradeOffer third, int count)
            {
            }

            public void Hide()
            {
            }
        }

        private sealed class FakeViewFactory : IViewFactory
        {
            public int Created;
            public int Released;

            public IView Create(GameObject prefab, Vector3 position)
            {
                Created++;

                return new FakeView();
            }

            public void Release(IView view)
            {
                Released++;
            }

            public void Retire(IView view, float seconds, Vector3 knockbackDirection)
            {
            }
        }

        private sealed class FakeView : IView
        {
            public Transform Transform => null;

            public void SetPosition(Vector3 position)
            {
            }

            public void SetRotation(Quaternion rotation)
            {
            }

            public void PlayHit()
            {
            }

            public void SetInvulnerable(bool value)
            {
            }

            public void SetBerserk(bool value)
            {
            }

            public void SetDashing(bool value)
            {
            }

            public void SetRunning(bool value)
            {
            }

            public void PlayAttack(string trigger, float speed)
            {
            }

            public void PlayDeath()
            {
            }

            public void Dissolve()
            {
            }
        }
    }
}

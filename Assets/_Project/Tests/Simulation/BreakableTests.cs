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
    public sealed class BreakableTests
    {
        private const int Seed = 987654321;
        private const float HeroMaxHealth = 100f;
        private const float BreakableHealth = 40f;
        private const float HealFraction = 0.3f;
        private const float DropChance = 0.7f;
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
        public void SpawnPlacesTheCountInsideTheRingWithSpacing()
        {
            Fixture fixture = Build(count: 8);

            List<Vector3> placed = fixture.BreakablePositions();

            Assert.AreEqual(8, placed.Count);

            for (int index = 0; index < placed.Count; index++)
            {
                float radius = new Vector2(placed[index].x, placed[index].z).magnitude;

                Assert.GreaterOrEqual(radius, 4f - 1e-4f);
                Assert.LessOrEqual(radius, 15f + 1e-4f);

                for (int other = index + 1; other < placed.Count; other++)
                    Assert.GreaterOrEqual(Vector3.Distance(placed[index], placed[other]), 2f - 1e-4f);
            }

            Assert.AreEqual(8, fixture.Views.Created);
        }

        [Test]
        public void SpawnIsTheSameOnOneSeed()
        {
            List<Vector3> first = Build(count: 8).BreakablePositions();
            List<Vector3> second = Build(count: 8).BreakablePositions();

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void BrokenBreakableIsRemovedAndItsViewReleased()
        {
            Fixture fixture = Build(count: 0);

            int breakable = fixture.AddBreakable(new Vector3(5f, 0f, 0f), withDrop: false);

            fixture.Hit(breakable, BreakableHealth * 0.5f);
            fixture.Step();

            Assert.AreEqual(1, fixture.BreakableCount(), "half the health leaves it standing");

            fixture.Hit(breakable, BreakableHealth * 0.5f);
            fixture.Step();

            Assert.AreEqual(0, fixture.BreakableCount());
            Assert.AreEqual(1, fixture.Views.Released);
            Assert.AreEqual(0, fixture.OrbCount());
        }

        [Test]
        public void DropRateFollowsTheChance()
        {
            Fixture fixture = Build(count: 0);

            const int Total = 2000;

            for (int index = 0; index < Total; index++)
            {
                int breakable = fixture.AddBreakable(new Vector3(20f, 0f, 0f), withDrop: true);
                fixture.Hit(breakable, BreakableHealth);
            }

            fixture.Step();

            float rate = (float)fixture.OrbCount() / Total;

            Assert.AreEqual(0, fixture.BreakableCount());
            Assert.AreEqual(DropChance, rate, 0.03f, $"dropped {fixture.OrbCount()} of {Total}");
        }

        [Test]
        public void HitsOnABreakableGiveNoRageButHitsOnAnEnemyDo()
        {
            Fixture fixture = Build(count: 0);

            int breakable = fixture.AddBreakable(new Vector3(20f, 0f, 0f), withDrop: false);

            fixture.Hit(breakable, 1f);
            fixture.Step();

            Assert.AreEqual(0f, fixture.Rage(), "a hit that leaves the breakable standing");

            fixture.Hit(breakable, BreakableHealth);
            fixture.Step();

            Assert.AreEqual(0f, fixture.Rage(), "the breaking hit");

            int enemy = fixture.AddEnemy();

            fixture.Hit(enemy, 1f);
            fixture.Step();

            Assert.Greater(fixture.Rage(), 0f);
        }

        [Test]
        public void OrbWaitsWhileTheHeroIsUnhurt()
        {
            Fixture fixture = Build(count: 0);

            fixture.SpawnOrb(new Vector3(1f, 0f, 0f));
            fixture.Step(FlightTicks * 2);

            Assert.AreEqual(1, fixture.OrbCount());
            Assert.AreEqual(new Vector3(1f, 0f, 0f), fixture.FirstOrbPosition(), "a resting orb does not move");

            fixture.SetHealth(50f);
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(0, fixture.OrbCount());
            Assert.AreEqual(80f, fixture.Health(), 1e-4f, "30% of 100 max health");
        }

        [Test]
        public void HealStopsAtMaxHealth()
        {
            Fixture fixture = Build(count: 0);

            fixture.SetHealth(90f);
            fixture.SpawnOrb(new Vector3(1f, 0f, 0f));
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(HeroMaxHealth, fixture.Health());
        }

        [Test]
        public void TwoOrbsInOneTickAddUpAndStillStopAtMax()
        {
            Fixture fixture = Build(count: 0);

            fixture.SetHealth(20f);
            fixture.SpawnOrb(new Vector3(1f, 0f, 0f));
            fixture.SpawnOrb(new Vector3(-1f, 0f, 0f));
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(80f, fixture.Health(), 1e-4f);

            fixture.SetHealth(60f);
            fixture.SpawnOrb(new Vector3(1f, 0f, 0f));
            fixture.SpawnOrb(new Vector3(-1f, 0f, 0f));
            fixture.Step(FlightTicks + 1);

            Assert.AreEqual(HeroMaxHealth, fixture.Health());
        }

        [Test]
        public void DeadHeroIsNotHealed()
        {
            Fixture fixture = Build(count: 0);

            fixture.SetHealth(50f);
            fixture.SpawnOrb(new Vector3(1f, 0f, 0f));
            fixture.Step(5);

            fixture.World.GetPool<Dead>().Add(fixture.Hero);
            fixture.Step(FlightTicks * 2);

            Assert.AreEqual(50f, fixture.Health());
            Assert.AreEqual(1, fixture.OrbCount(), "the orb of a dead hero stays where it stopped");
        }

        [Test]
        public void HealEventOnADeadTargetChangesNothing()
        {
            Fixture fixture = Build(count: 0);

            fixture.SetHealth(0f);
            fixture.World.GetPool<Dead>().Add(fixture.Hero);
            fixture.Heal(50f);
            fixture.Step();

            Assert.AreEqual(0f, fixture.Health());
        }

        [Test]
        public void BreakableWiderThanTheWidestEnemyIsRejected()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Build(count: 1, bodyRadius: 0.6f));

            StringAssert.Contains("breakable.test", error.Message);
            StringAssert.Contains("widest enemy", error.Message);
        }

        private Fixture Build(int count, float bodyRadius = 0.4f)
        {
            ExperienceConfig experience = ScriptableObject.CreateInstance<ExperienceConfig>();
            SetField(experience, "_id", "experience.test");
            SetField(experience, "_flightSeconds", 0.5f);
            _assets.Add(experience);

            RageConfig rage = ScriptableObject.CreateInstance<RageConfig>();
            SetField(rage, "_id", "resource.rage.test");
            _assets.Add(rage);

            CharacterConfig character = ScriptableObject.CreateInstance<CharacterConfig>();
            SetField(character, "_id", "character.test");
            SetField(character, "_experience", experience);
            SetField(character, "_rage", rage);
            _assets.Add(character);

            GameObject prefab = new GameObject("BreakableTestPrefab");
            _assets.Add(prefab);

            HealOrbConfig orb = ScriptableObject.CreateInstance<HealOrbConfig>();
            SetField(orb, "_id", "pickup.test");
            SetField(orb, "_viewPrefab", prefab);
            SetField(orb, "_healFraction", HealFraction);
            _assets.Add(orb);

            BreakableConfig breakable = ScriptableObject.CreateInstance<BreakableConfig>();
            SetField(breakable, "_id", "breakable.test");
            SetField(breakable, "_viewPrefab", prefab);
            SetField(breakable, "_maxHealth", BreakableHealth);
            SetField(breakable, "_bodyRadius", bodyRadius);
            SetField(breakable, "_healOrb", orb);
            SetField(breakable, "_healOrbChance", DropChance);
            _assets.Add(breakable);

            FeedbackConfig feedback = ScriptableObject.CreateInstance<FeedbackConfig>();
            SetField(feedback, "_id", "feedback.test");
            _assets.Add(feedback);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_arenaRadius", 45f);
            SetField(level, "_feedback", feedback);
            SetField(level, "_breakable", breakable);
            SetField(level, "_breakableCount", count);
            SetField(level, "_breakableMinRadius", 4f);
            SetField(level, "_breakableMaxRadius", 15f);
            SetField(level, "_breakableSpacing", 2f);
            _assets.Add(level);

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { character, breakable, orb });

            Fixture fixture = new Fixture(registry, level, breakable);
            _fixtures.Add(fixture);

            return fixture;
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
            public readonly FakeViewFactory Views = new FakeViewFactory();
            public readonly int Hero;

            private readonly BreakableConfig _breakable;
            private readonly SimulationClock _clock = new SimulationClock();
            private readonly EcsSystems _systems;

            public Fixture(ContentRegistry registry, LevelConfig level, BreakableConfig breakable)
            {
                _breakable = breakable;

                RunContext context = new RunContext("level.test", "character.test", RunMode.Story, Seed, Array.Empty<StatModifierSpec>());

                World = new EcsWorld();

                Hero = World.NewEntity();
                World.GetPool<Player>().Add(Hero);
                World.GetPool<Position>().Add(Hero).Value = Vector3.zero;
                World.GetPool<Velocity>().Add(Hero).Value = Vector3.zero;

                ref PickupRadius radius = ref World.GetPool<PickupRadius>().Add(Hero);
                radius.Base = PickupRadius;
                radius.Value = PickupRadius;

                ref MaxHealth maxHealth = ref World.GetPool<MaxHealth>().Add(Hero);
                maxHealth.Base = HeroMaxHealth;
                maxHealth.Value = HeroMaxHealth;

                World.GetPool<Health>().Add(Hero).Current = HeroMaxHealth;
                World.GetPool<UltimateCharge>().Add(Hero).Max = 100f;
                World.GetPool<RageState>().Add(Hero);

                object[] injects =
                {
                    context, registry, level, _clock, Views, new SpatialGrid(World, 45f, 1f),
                    new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f)
                };

                _systems = new EcsSystems(World);
                _systems.Add(new SpawnBreakablesSystem());
                _systems.Add(new ApplyGemFlightSystem());
                _systems.Add(new MoveSystem());
                _systems.Add(new StartGemFlightSystem());
                _systems.Add(new ApplyDamageSystem());
                _systems.Add(new MarkDeadSystem());
                _systems.Add(new DropHealOrbsSystem());
                _systems.Add(new AccumulateRageSystem());
                _systems.Add(new CollectHealOrbsSystem());
                _systems.Add(new ApplyHealSystem());
                _systems.Add(new ReapBreakablesSystem());
                _systems.Add(new CleanupEventsSystem());
                _systems.Inject(injects);
                _systems.Init();
            }

            public void Step(int ticks = 1)
            {
                for (int tick = 0; tick < ticks; tick++)
                {
                    _clock.Advance(TickSeconds);
                    _systems.Run();
                }
            }

            public int AddBreakable(Vector3 position, bool withDrop)
            {
                int entity = World.NewEntity();
                World.GetPool<Breakable>().Add(entity);
                World.GetPool<Position>().Add(entity).Value = position;
                World.GetPool<Health>().Add(entity).Current = _breakable.MaxHealth;
                World.GetPool<View>().Add(entity).Value = Views.Create(null, position);

                if (withDrop)
                {
                    ref HealOrbDrop drop = ref World.GetPool<HealOrbDrop>().Add(entity);
                    drop.Config = _breakable.HealOrb;
                    drop.Chance = _breakable.HealOrbChance;
                }

                return entity;
            }

            public int AddEnemy()
            {
                int entity = World.NewEntity();
                World.GetPool<Enemy>().Add(entity);
                World.GetPool<Health>().Add(entity).Current = 1000f;

                return entity;
            }

            public void Hit(int target, float amount)
            {
                int entity = World.NewEntity();

                ref DamageEvent damageEvent = ref World.GetPool<DamageEvent>().Add(entity);
                damageEvent.Source = World.PackEntity(Hero);
                damageEvent.Target = World.PackEntity(target);
                damageEvent.Amount = amount;
                damageEvent.Kind = DamageKind.Weapon;
                damageEvent.IsSlash = true;
            }

            public void Heal(float amount)
            {
                int entity = World.NewEntity();

                ref HealEvent healEvent = ref World.GetPool<HealEvent>().Add(entity);
                healEvent.Target = World.PackEntity(Hero);
                healEvent.Amount = amount;
            }

            public void SpawnOrb(Vector3 position)
            {
                int orb = World.NewEntity();
                World.GetPool<HealOrb>().Add(orb).Fraction = HealFraction;
                World.GetPool<Position>().Add(orb).Value = position;
                World.GetPool<Velocity>().Add(orb).Value = Vector3.zero;
            }

            public void SetHealth(float value)
            {
                World.GetPool<Health>().Get(Hero).Current = value;
            }

            public float Health()
            {
                return World.GetPool<Health>().Get(Hero).Current;
            }

            public float Rage()
            {
                return World.GetPool<UltimateCharge>().Get(Hero).Value;
            }

            public List<Vector3> BreakablePositions()
            {
                List<Vector3> positions = new List<Vector3>();

                foreach (int entity in World.Filter<Breakable>().Inc<Position>().End())
                    positions.Add(World.GetPool<Position>().Get(entity).Value);

                return positions;
            }

            public int BreakableCount()
            {
                int count = 0;

                foreach (int entity in World.Filter<Breakable>().End())
                    count++;

                return count;
            }

            public int OrbCount()
            {
                int count = 0;

                foreach (int entity in World.Filter<HealOrb>().End())
                    count++;

                return count;
            }

            public Vector3 FirstOrbPosition()
            {
                foreach (int orb in World.Filter<HealOrb>().End())
                    return World.GetPool<Position>().Get(orb).Value;

                throw new InvalidOperationException("fixture: no orb");
            }

            public void Dispose()
            {
                _systems.Destroy();
                World.Destroy();
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

            public void Retire(IView view, float seconds, DeathBlow blow)
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

            public void PlayHit(Vector3 direction)
            {
            }

            public void PlayWeaponHit()
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

            public void SetSpecialAttack(bool value)
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

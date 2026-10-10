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
    public sealed class RangedAttackTests
    {
        private const float Tick = 1f / 60f;
        private const string EnemyId = "enemy.archer.test";
        private const string AttackId = "attack.archer_crossbow.test";
        private const int WindupTicks = 24;
        private const int CooldownTicks = 150;
        private const int FlightTicks = 84;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private CountingViewFactory _views;
        private RangedAttackConfig _attack;
        private EnemyConfig _enemy;
        private int _hero;
        private int _archer;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void TheArcherChasesOutsideTheRangeAndStandsInsideIt()
        {
            Build(heroAt: new Vector3(8.5f, 0f, 0f));

            Run(1);

            Assert.Greater(Intent().sqrMagnitude, 0.5f, "8.5 m is outside the 8 m range");
            Assert.IsFalse(Attack().IsHolding);

            MoveHero(new Vector3(7.9f, 0f, 0f));
            Run(1);

            Assert.AreEqual(Vector3.zero, Intent());
            Assert.IsTrue(Attack().IsHolding);
        }

        [Test]
        public void TheShotLeavesAfterTheWindupTowardsTheHeroOfThatTick()
        {
            Build(heroAt: new Vector3(5f, 0f, 0f));

            Run(1);

            Assert.AreEqual(WindupTicks, Attack().WindupTicks, "the windup starts on the first tick in range");

            Run(9);
            MoveHero(new Vector3(5f, 0f, 3f));
            Run(WindupTicks - 10);

            Assert.AreEqual(0, Bolts(), "no bolt one tick before the windup ends");

            Run(1);

            Assert.AreEqual(1, Bolts());

            int bolt = FirstBolt();
            Vector3 direction = _world.GetPool<Velocity>().Get(bolt).Value / 10f;
            Vector3 expected = new Vector3(5f, 0f, 3f).normalized;

            Assert.AreEqual(expected.x, direction.x, 1e-5f);
            Assert.AreEqual(expected.z, direction.z, 1e-5f);
            Assert.AreEqual(expected.x * 0.4f, _world.GetPool<Position>().Get(bolt).Value.x, 1e-5f, "the bolt leaves from the archer's body edge");
        }

        [Test]
        public void TheCooldownCountsFromTheShot()
        {
            Build(heroAt: new Vector3(0f, 0f, 6f));

            List<int> shots = RecordShots(ticks: 400);

            CollectionAssert.AreEqual(new[] { 1 + WindupTicks, 1 + WindupTicks + CooldownTicks + WindupTicks }, shots.GetRange(0, 2), "the windup starts on tick 1");
        }

        [Test]
        public void LeavingTheRangeDuringTheWindupDoesNotCancelTheShot()
        {
            Build(heroAt: new Vector3(6f, 0f, 0f));

            Run(1);
            MoveHero(new Vector3(20f, 0f, 0f));
            Run(WindupTicks);

            Assert.AreEqual(1, Bolts());
            Assert.AreEqual(Vector3.zero, Intent(), "the archer stands until the shot leaves");

            Run(1);

            Assert.Greater(Intent().sqrMagnitude, 0.5f, "after the shot an archer with the hero out of range chases again");
        }

        [Test]
        public void TheBoltHitsTheHeroOnceAndReturnsItsView()
        {
            Build(heroAt: new Vector3(3f, 0f, 0f));

            Run(WindupTicks + 1);

            int bolt = FirstBolt();
            EcsPackedEntity packedBolt = _world.PackEntity(bolt);

            int ticks = 0;

            while (DamageEvents().Count == 0 && ticks < 60)
            {
                Run(1);
                ticks++;
            }

            List<int> events = DamageEvents();

            Assert.AreEqual(1, events.Count);

            DamageEvent damageEvent = _world.GetPool<DamageEvent>().Get(events[0]);

            Assert.AreEqual(DamageKind.Projectile, damageEvent.Kind);
            Assert.AreEqual(10f, damageEvent.Amount);
            Assert.IsTrue(damageEvent.Target.Unpack(_world, out int target) && target == _hero);
            Assert.IsTrue(damageEvent.Source.Unpack(_world, out int source) && source == _archer, "the source is the archer");
            Assert.AreEqual(3f - 0.6f, damageEvent.SourcePosition.x, 0.17f, "the source position is the bolt where it touched");
            Assert.IsFalse(packedBolt.Unpack(_world, out _), "the bolt is gone");
            Assert.AreEqual(1, _views.Released);
            Assert.AreEqual(0, _views.DoubleReleases);
        }

        [Test]
        public void TheBoltFliesThroughEnemies()
        {
            Build(heroAt: new Vector3(7f, 0f, 0f));

            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = new Vector3(3f, 0f, 0f);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 20f;

            Run(WindupTicks + 60);

            List<int> events = DamageEvents();

            Assert.Greater(events.Count, 0, "the bolt reached the hero behind the enemy");

            for (int index = 0; index < events.Count; index++)
            {
                Assert.IsTrue(_world.GetPool<DamageEvent>().Get(events[index]).Target.Unpack(_world, out int target));
                Assert.AreEqual(_hero, target, "no event against an enemy");
            }
        }

        [Test]
        public void TheBoltVanishesAfterItsRange()
        {
            Build(heroAt: new Vector3(6f, 0f, 0f));

            Run(WindupTicks + 1);

            int bolt = FirstBolt();
            EcsPackedEntity packedBolt = _world.PackEntity(bolt);
            Vector3 start = _world.GetPool<Position>().Get(bolt).Value;

            MoveHero(new Vector3(0f, 0f, 7.9f));

            Vector3 last = start;
            int lived = 0;

            while (packedBolt.Unpack(_world, out int alive))
            {
                last = _world.GetPool<Position>().Get(alive).Value;
                Run(1);
                lived++;
            }

            Assert.AreEqual(FlightTicks, lived, "the bolt is removed on its 84th move");
            Assert.AreEqual(14f - 10f * Tick, Vector3.Distance(start, last), 1e-3f, "the last position seen alive is one move short of 14 m");
            Assert.AreEqual(0, DamageEvents().Count);
        }

        [Test]
        public void ADashThroughTheBoltIsCaughtByTheSweep()
        {
            Build(heroAt: new Vector3(30f, 0f, 30f));

            EcsPackedEntity bolt = _world.PackEntity(SpawnBolt(new Vector3(5f, 0f, 0f), Vector3.right));

            Vector3 heroFrom = new Vector3(5.65f, 0f, 0f);
            Vector3 heroTo = heroFrom + Vector3.left * 1.141f;
            Vector3 boltTo = new Vector3(5f + 10f * Tick, 0f, 0f);

            Assert.Greater(Mathf.Abs(heroFrom.x - 5f), 0.6f, "the start points do not touch");
            Assert.Greater(Mathf.Abs(heroTo.x - boltTo.x), 0.6f, "the end points do not touch either");

            _world.GetPool<PreviousPosition>().Get(_hero).Value = heroFrom;
            _world.GetPool<Position>().Get(_hero).Value = heroTo;
            _clock.Advance(Tick);
            _systems.Run();

            Assert.AreEqual(1, DamageEvents().Count);
            Assert.IsFalse(bolt.Unpack(_world, out _), "the bolt is gone");
        }

        [Test]
        public void AnInvulnerableHeroStillStopsTheBolt()
        {
            Build(heroAt: new Vector3(2f, 0f, 0f));

            _world.GetPool<Invulnerable>().Add(_hero).RemainingTicks = 1000;

            Run(WindupTicks + 20);

            Assert.AreEqual(1, DamageEvents().Count, "the event is created; ApplyDamageSystem is the one to drop it");
            Assert.AreEqual(0, Bolts());
        }

        [Test]
        public void AnArcherRemovedDuringTheWindupNeverShoots()
        {
            Build(heroAt: new Vector3(5f, 0f, 0f));

            Run(10);

            _world.DelEntity(_archer);

            Run(WindupTicks);

            Assert.AreEqual(0, Bolts());
        }

        [Test]
        public void ADeadArcherNeverShoots()
        {
            Build(heroAt: new Vector3(5f, 0f, 0f));

            Run(10);

            _world.GetPool<Dead>().Add(_archer);

            Run(WindupTicks);

            Assert.AreEqual(0, Bolts());
        }

        [Test]
        public void ADeadHeroIsNeitherAimedAtNorHit()
        {
            Build(heroAt: new Vector3(2f, 0f, 0f));

            _world.GetPool<Dead>().Add(_hero);

            Run(WindupTicks + 10);

            Assert.AreEqual(0, Attack().WindupTicks);
            Assert.AreEqual(0, Bolts());

            SpawnBolt(new Vector3(1f, 0f, 0f), Vector3.right);

            Run(10);

            Assert.AreEqual(0, DamageEvents().Count, "a bolt passes through the corpse");
        }

        [Test]
        public void TheArcherTurnsTowardsTheHeroWhileHolding()
        {
            Build(heroAt: new Vector3(-5f, 0f, 0f));

            Run(1);

            Vector3 facing = _world.GetPool<Facing>().Get(_archer).Value;

            Assert.AreEqual(6f, Vector3.Angle(Vector3.forward, facing), 0.01f, "360 degrees a second is 6 degrees a tick");
            Assert.Less(facing.x, 0f);
        }

        [Test]
        public void ValidationNamesTheEnemyAndTheField()
        {
            BuildConfigs();
            SetField(_attack, "_boltSpeed", 0f);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => BuildWorld(new Vector3(5f, 0f, 0f)));

            StringAssert.Contains(EnemyId, error.Message);
            StringAssert.Contains(AttackId, error.Message);
            StringAssert.Contains("_boltSpeed", error.Message);
        }

        private List<int> RecordShots(int ticks)
        {
            List<int> shots = new List<int>();
            int seen = 0;

            for (int tick = 1; tick <= ticks; tick++)
            {
                Run(1);

                int fired = _views.Created;

                if (fired > seen)
                    shots.Add(tick);

                seen = fired;
            }

            return shots;
        }

        private int SpawnBolt(Vector3 position, Vector3 direction)
        {
            int bolt = _world.NewEntity();

            ref Projectile projectile = ref _world.GetPool<Projectile>().Add(bolt);
            projectile.LastPosition = position;
            projectile.Damage = 10f;
            projectile.Radius = 0.2f;
            projectile.RemainingTicks = FlightTicks;

            _world.GetPool<Position>().Add(bolt).Value = position;
            _world.GetPool<Velocity>().Add(bolt).Value = direction * 10f;
            _world.GetPool<Facing>().Add(bolt).Value = direction;

            return bolt;
        }

        private void MoveHero(Vector3 position)
        {
            _world.GetPool<PreviousPosition>().Get(_hero).Value = position;
            _world.GetPool<Position>().Get(_hero).Value = position;
        }

        private Vector3 Intent()
        {
            return _world.GetPool<MoveIntent>().Get(_archer).Value;
        }

        private RangedAttack Attack()
        {
            return _world.GetPool<RangedAttack>().Get(_archer);
        }

        private int Bolts()
        {
            return _world.Filter<Projectile>().End().GetEntitiesCount();
        }

        private int FirstBolt()
        {
            EcsFilter filter = _world.Filter<Projectile>().End();

            foreach (int entity in filter)
                return entity;

            Assert.Fail("no bolt");

            return -1;
        }

        private List<int> DamageEvents()
        {
            List<int> events = new List<int>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
                events.Add(entity);

            return events;
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void Build(Vector3 heroAt)
        {
            BuildConfigs();
            BuildWorld(heroAt);
        }

        private void BuildConfigs()
        {
            GameObject prefab = new GameObject("StubBolt");
            _assets.Add(prefab);

            _attack = ScriptableObject.CreateInstance<RangedAttackConfig>();
            _assets.Add(_attack);
            SetField(_attack, "_id", AttackId);
            SetField(_attack, "_boltPrefab", prefab);

            _enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            _assets.Add(_enemy);
            SetField(_enemy, "_id", EnemyId);
            SetField(_enemy, "_rangedAttack", _attack);
        }

        private void BuildWorld(Vector3 heroAt)
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            _assets.Add(level);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _views = new CountingViewFactory();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = heroAt;
            _world.GetPool<PreviousPosition>().Add(_hero).Value = heroAt;
            _world.GetPool<BodyRadius>().Add(_hero).Value = 0.4f;
            _world.GetPool<Health>().Add(_hero).Current = 100f;

            _archer = _world.NewEntity();
            _world.GetPool<Enemy>().Add(_archer);
            _world.GetPool<Position>().Add(_archer).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_archer).Value = Vector3.forward;
            _world.GetPool<TurnSpeed>().Add(_archer).Value = 360f;
            _world.GetPool<BodyRadius>().Add(_archer).Value = 0.4f;
            _world.GetPool<MoveIntent>().Add(_archer);
            _world.GetPool<Velocity>().Add(_archer);
            _world.GetPool<ChaseTarget>().Add(_archer).Value = _world.PackEntity(_hero);
            _world.GetPool<RangedAttack>().Add(_archer).Config = _attack;

            _systems = new EcsSystems(_world);
            _systems.Add(new ChaseTargetSystem());
            _systems.Add(new HoldForRangedAttackSystem());
            _systems.Add(new FaceRangedTargetSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new ReleaseRangedAttackSystem());
            _systems.Add(new StartRangedAttackSystem());
            _systems.Add(new AdvanceProjectilesSystem());
            _systems.Inject(new ContentRegistry(new IContentEntry[] { _enemy, _attack }), (IViewFactory)_views, _clock, level);
            _systems.Init();
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class CountingViewFactory : IViewFactory
        {
            private readonly HashSet<IView> _live = new HashSet<IView>();

            public int Created;
            public int Released;
            public int DoubleReleases;

            public IView Create(GameObject prefab, Vector3 position)
            {
                Created++;
                StubView view = new StubView();
                _live.Add(view);

                return view;
            }

            public void Release(IView view)
            {
                if (_live.Remove(view) == false)
                {
                    DoubleReleases++;

                    return;
                }

                Released++;
            }

            public void Retire(IView view, float seconds, Vector3 knockbackDirection)
            {
                Release(view);
            }
        }

        private sealed class StubView : IView
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

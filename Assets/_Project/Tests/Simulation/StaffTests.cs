using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class StaffTests
    {
        private const float Tick = 1f / 60f;
        private const int CastTicks = 27;
        private const float Damage = 20f;
        private const float BlastScale = 0.6f;
        private const float BlastRadius = 2.5f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private RecordingView _heroView;
        private int _hero;
        private int _staff;

        [SetUp]
        public void SetUp()
        {
            TorsoAimConfig aim = Asset<TorsoAimConfig>();
            Set(aim, "_searchRadius", 16f);
            Set(aim, "_torsoTurnSpeed", 100000f);
            Set(aim, "_wholeBody", true);

            GameObject fx = new GameObject("Fx");
            _assets.Add(fx);

            StaffConfig staff = Asset<StaffConfig>();
            Set(staff, "_damage", Damage);
            Set(staff, "_cooldownSeconds", 1.2f);
            Set(staff, "_castSeconds", 0.45f);
            Set(staff, "_range", 15f);
            Set(staff, "_fireballSpeed", 14f);
            Set(staff, "_fireballRange", 16f);
            Set(staff, "_fireballRadius", 0.3f);
            Set(staff, "_muzzleDistance", 0.8f);
            Set(staff, "_explosionRadius", BlastRadius);
            Set(staff, "_explosionDamageScale", BlastScale);
            Set(staff, "_explosionPushSpeed", 6f);
            Set(staff, "_explosionPushSeconds", 0.33f);
            Set(staff, "_fireballPrefab", fx);
            Set(staff, "_explosionPrefab", fx);

            WaveTimelineConfig timeline = Asset<WaveTimelineConfig>();
            LevelConfig level = Asset<LevelConfig>();
            Set(level, "_waves", timeline);

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new AimTorsoSystem());
            _systems.Add(new FaceLegsSystem());
            _systems.Add(new ApplyPushSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new TickWeaponCooldownSystem());
            _systems.Add(new ReleaseStaffCastSystem());
            _systems.Add(new StartStaffCastSystem());
            _systems.Add(new AdvanceProjectilesSystem());
            _systems.Add(new AdvanceFireballsSystem());
            _systems.Add(new ResolveAreaStrikeSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(_clock, level, new SpatialGrid(_world, 45f, 1f), new EnemyMotionBounds(2.5f, 0.9f, 6f, 0.5f), (IViewFactory)_views);
            _systems.Init();

            _heroView = new RecordingView();
            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<TurnSpeed>().Add(_hero).Value = 720f;
            _world.GetPool<BodyRadius>().Add(_hero).Value = 0.4f;
            _world.GetPool<Health>().Add(_hero).Current = 100f;
            _world.GetPool<InputState>().Add(_hero);
            _world.GetPool<View>().Add(_hero).Value = _heroView;

            ref TorsoAim torso = ref _world.GetPool<TorsoAim>().Add(_hero);
            torso.Config = aim;
            torso.Direction = Vector3.forward;
            torso.Desired = Vector3.forward;

            _staff = _world.NewEntity();
            _world.GetPool<Staff>().Add(_staff).Config = staff;
            _world.GetPool<WeaponDamage>().Add(_staff).Value = Damage;
            _world.GetPool<WeaponCooldown>().Add(_staff).Value = 1.2f;
            _world.GetPool<AttackSpeed>().Add(_staff).Value = 1f;
            _world.GetPool<WeaponReady>().Add(_staff);
            _world.GetPool<OwnerLink>().Add(_staff).Owner = _world.PackEntity(_hero);
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
        public void TheFireballLeavesWhenTheCastEnds()
        {
            Enemy(new Vector3(0f, 0f, 10f));

            Step(1);

            Assert.IsTrue(_world.GetPool<StaffCast>().Has(_staff), "the cast starts on the first tick a target is in range");
            CollectionAssert.AreEqual(new[] { "CastFireball" }, _heroView.Triggers);

            Step(CastTicks - 1);

            Assert.AreEqual(0, Fireballs(), "no fireball one tick before the cast ends");

            Step(1);

            Assert.AreEqual(1, Fireballs());
        }

        [Test]
        public void TheFirstEnemyTakesTheHitAndTheBlastWhileItsNeighbourTakesOnlyTheBlast()
        {
            int first = Enemy(new Vector3(0f, 0f, 8f));
            int behind = Enemy(new Vector3(0f, 0f, 9.5f));
            int beside = Enemy(new Vector3(1.6f, 0f, 8.4f));
            int far = Enemy(new Vector3(4f, 0f, 8f));

            Step(CastTicks + 40);

            Assert.AreEqual(Damage + Damage * BlastScale, Total(first), 1e-3f, "the direct hit plus the blast");
            Assert.AreEqual(Damage * BlastScale, Total(behind), 1e-3f, "the fireball stops at the first enemy; the one behind only feels the blast");
            Assert.AreEqual(Damage * BlastScale, Total(beside), 1e-3f);
            Assert.AreEqual(0f, Total(far), "outside the blast radius");

            foreach (DamageEvent hit in Hits())
                Assert.IsTrue(hit.IsMagic, "the fireball and its blast scale with spell power");
        }

        [Test]
        public void TheBlastThrowsANeighbourAboutAMetre()
        {
            OnlyOneCast();
            Enemy(new Vector3(0f, 0f, 8f));
            int beside = Enemy(new Vector3(1.5f, 0f, 8f));
            Vector3 start = _world.GetPool<Position>().Get(beside).Value;

            Step(CastTicks + 80);

            float moved = Vector3.Distance(start, _world.GetPool<Position>().Get(beside).Value);

            TestContext.WriteLine($"neighbour thrown {moved:F3} m");
            Assert.That(moved, Is.InRange(0.7f, 1.3f), "a small knockback, in metres");
        }

        [Test]
        public void AFireballThatHitsNothingExplodesAtTheEndOfItsRange()
        {
            OnlyOneCast();

            ref InputState input = ref _world.GetPool<InputState>().Get(_hero);
            input.StandHeld = true;
            input.HasAim = true;
            input.AimPoint = new Vector3(0f, 0f, 5f);

            Step(CastTicks + 1);

            Assert.AreEqual(1, Fireballs(), "RMB casts at the cursor without a target");

            Step(80);

            Assert.AreEqual(0, Fireballs());
            Assert.AreEqual(2, _views.Created, "the fireball and its explosion");
        }

        private void OnlyOneCast()
        {
            _world.GetPool<WeaponCooldown>().Get(_staff).Value = 100f;
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<PreviousPosition>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 500f;

            return enemy;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private int Fireballs()
        {
            return _world.Filter<Fireball>().End().GetEntitiesCount();
        }

        private List<DamageEvent> Hits()
        {
            List<DamageEvent> hits = new List<DamageEvent>();
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
                hits.Add(pool.Get(entity));

            return hits;
        }

        private float Total(int target)
        {
            float total = 0f;

            foreach (DamageEvent hit in Hits())
            {
                if (hit.Target.Unpack(_world, out int entity) && entity == target)
                    total += hit.Amount;
            }

            return total;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}

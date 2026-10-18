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
    public sealed class BowTests
    {
        private const float Tick = 1f / 60f;
        private const int DrawTicks = 27;
        private const float ArrowSpeed = 30f;
        private const float ArrowRange = 20f;
        private const float Damage = 14f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private RecordingView _heroView;
        private int _hero;
        private int _bow;

        [SetUp]
        public void SetUp()
        {
            TorsoAimConfig aim = Asset<TorsoAimConfig>();
            Set(aim, "_searchRadius", 14f);
            Set(aim, "_torsoTurnSpeed", 100000f);

            BowConfig bow = Asset<BowConfig>();
            Set(bow, "_damage", Damage);
            Set(bow, "_cooldownSeconds", 0.8f);
            Set(bow, "_drawSeconds", 0.45f);
            Set(bow, "_range", 14f);
            Set(bow, "_aimToleranceDegrees", 12f);
            Set(bow, "_arrowSpeed", ArrowSpeed);
            Set(bow, "_arrowRange", ArrowRange);
            Set(bow, "_arrowRadius", 0.15f);
            Set(bow, "_muzzleDistance", 0.6f);

            WaveTimelineConfig timeline = Asset<WaveTimelineConfig>();
            LevelConfig level = Asset<LevelConfig>();
            Set(level, "_waves", timeline);

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new AimTorsoSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new TickWeaponCooldownSystem());
            _systems.Add(new ReleaseBowShotSystem());
            _systems.Add(new StartBowDrawSystem());
            _systems.Add(new AdvanceProjectilesSystem());
            _systems.Add(new AdvanceArrowsSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(_clock, level, new SpatialGrid(_world, 45f, 1f), new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f), (IViewFactory)_views);
            _systems.Init();

            _heroView = new RecordingView();
            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<BodyRadius>().Add(_hero).Value = 0.4f;
            _world.GetPool<Health>().Add(_hero).Current = 100f;
            _world.GetPool<InputState>().Add(_hero);
            _world.GetPool<View>().Add(_hero).Value = _heroView;

            ref TorsoAim torso = ref _world.GetPool<TorsoAim>().Add(_hero);
            torso.Config = aim;
            torso.Direction = Vector3.forward;
            torso.Desired = Vector3.forward;

            _bow = _world.NewEntity();
            _world.GetPool<Bow>().Add(_bow).Config = bow;
            _world.GetPool<WeaponDamage>().Add(_bow).Value = Damage;
            _world.GetPool<WeaponCooldown>().Add(_bow).Value = 0.8f;
            _world.GetPool<AttackSpeed>().Add(_bow).Value = 1f;
            _world.GetPool<WeaponReady>().Add(_bow);
            _world.GetPool<OwnerLink>().Add(_bow).Owner = _world.PackEntity(_hero);
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
        public void TheArrowLeavesWhenTheDrawEnds()
        {
            Enemy(new Vector3(0f, 0f, 10f));

            Step(1);

            Assert.IsTrue(_world.GetPool<BowDraw>().Has(_bow), "the draw starts on the first tick a target is in range");
            CollectionAssert.AreEqual(new[] { "Draw" }, _heroView.Triggers);

            Step(DrawTicks - 1);

            Assert.AreEqual(0, Arrows(), "no arrow one tick before the draw ends");

            Step(1);

            Assert.AreEqual(1, Arrows());
            Assert.AreEqual("Release", _heroView.Triggers[_heroView.Triggers.Count - 1]);
        }

        [Test]
        public void NoTargetInRangeMeansNoShot()
        {
            Enemy(new Vector3(0f, 0f, 15f));

            Step(60);

            Assert.IsFalse(_world.GetPool<BowDraw>().Has(_bow));
            Assert.AreEqual(0, _views.Created);
        }

        [Test]
        public void TheArrowHitsTheFirstEnemyOnItsLineAndIsGone()
        {
            int near = Enemy(new Vector3(0f, 0f, 6f));
            int far = Enemy(new Vector3(0f, 0f, 7.2f));

            Step(DrawTicks + 20);

            List<DamageEvent> hits = Hits();

            Assert.AreEqual(1, hits.Count, "the first hit destroys the arrow");
            Assert.IsTrue(hits[0].Target.Unpack(_world, out int target));
            Assert.AreEqual(near, target);
            Assert.AreNotEqual(far, target);
            Assert.AreEqual(Damage, hits[0].Amount);
            Assert.AreEqual(DamageKind.Weapon, hits[0].Kind);
            Assert.IsTrue(hits[0].Source.Unpack(_world, out int source) && source == _hero, "the hero is the source, so crits and damage numbers apply");
            Assert.AreEqual(0, Arrows());
            Assert.AreEqual(_views.Created, _views.Released);
        }

        [Test]
        public void TheArrowExpiresAtItsRange()
        {
            int enemy = Enemy(new Vector3(0f, 0f, 10f));

            Step(DrawTicks + 1);
            _world.GetPool<Position>().Get(enemy).Value = new Vector3(30f, 0f, 30f);
            _world.GetPool<PreviousPosition>().Get(enemy).Value = new Vector3(30f, 0f, 30f);

            int flight = Mathf.RoundToInt(ArrowRange / (ArrowSpeed * Tick));

            Step(flight - 1);

            Assert.AreEqual(1, Arrows(), "the arrow still flies on its last tick");

            Step(1);

            Assert.AreEqual(0, Arrows());
            Assert.AreEqual(0, Hits().Count);
        }

        [Test]
        public void AttackSpeedShortensTheDrawAndThePeriod()
        {
            _world.GetPool<AttackSpeed>().Get(_bow).Value = 2f;
            Enemy(new Vector3(0f, 0f, 10f));

            Step(1);

            Assert.AreEqual(Mathf.RoundToInt(0.45f / 2f / Tick), _world.GetPool<BowDraw>().Get(_bow).RemainingTicks);
            Assert.AreEqual(0.4f, _world.GetPool<WeaponReady>().Get(_bow).Remaining, 1e-5f);
        }

        [Test]
        public void RightMouseShootsAtTheCursorWithoutAnEnemy()
        {
            ref InputState input = ref _world.GetPool<InputState>().Get(_hero);
            input.StandHeld = true;
            input.HasAim = true;
            input.AimPoint = new Vector3(-5f, 0f, 0f);

            Step(DrawTicks + 1);

            Assert.AreEqual(1, Arrows());

            foreach (int arrow in _world.Filter<Arrow>().End())
            {
                Vector3 velocity = _world.GetPool<Velocity>().Get(arrow).Value;
                Assert.Less(Vector3.Angle(velocity, Vector3.left), 0.01f);
            }
        }

        [Test]
        public void AnArrowFlyingThroughTheHeroDoesNotHurtHer()
        {
            Enemy(new Vector3(0f, 0f, 10f));

            Step(DrawTicks + 1);

            foreach (int arrow in _world.Filter<Arrow>().End())
            {
                _world.GetPool<Position>().Get(arrow).Value = new Vector3(0f, 0f, -1f);
                _world.GetPool<Projectile>().Get(arrow).LastPosition = new Vector3(0f, 0f, -1f);
                _world.GetPool<Velocity>().Get(arrow).Value = Vector3.forward * ArrowSpeed;
            }

            Step(4);

            foreach (DamageEvent hit in Hits())
                Assert.IsFalse(hit.Target.Unpack(_world, out int target) && target == _hero, "an arrow must never hit its own hero");
        }

        [Test]
        public void ADeadHeroCancelsTheDraw()
        {
            Enemy(new Vector3(0f, 0f, 10f));

            Step(5);
            _world.GetPool<Dead>().Add(_hero);
            Step(1);

            Assert.IsFalse(_world.GetPool<BowDraw>().Has(_bow));
            Assert.AreEqual("BowCancel", _heroView.Triggers[_heroView.Triggers.Count - 1]);

            Step(DrawTicks);

            Assert.AreEqual(0, Arrows());
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<PreviousPosition>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 50f;

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

        private int Arrows()
        {
            return _world.Filter<Arrow>().End().GetEntitiesCount();
        }

        private List<DamageEvent> Hits()
        {
            List<DamageEvent> hits = new List<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
                hits.Add(_world.GetPool<DamageEvent>().Get(entity));

            return hits;
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

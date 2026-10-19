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
    public sealed class IceRingTests
    {
        private const float Tick = 1f / 60f;
        private const float Radius = 4f;
        private const float Damage = 15f;
        private const float FreezeSeconds = 2f;
        private const float CooldownSeconds = 8f;
        private const float WalkSpeed = 2.5f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<int> _walkers = new List<int>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            GameObject fx = new GameObject("Fx");
            _assets.Add(fx);

            IceRingConfig ring = Asset<IceRingConfig>();
            IceRingLevel level = new IceRingLevel();
            Set(level, "_cooldownSeconds", CooldownSeconds);
            Set(level, "_freezeSeconds", FreezeSeconds);
            Set(level, "_damage", Damage);
            Set(ring, "_levels", new[] { level });
            Set(ring, "_radius", Radius);
            Set(ring, "_ringFxPrefab", fx);
            Set(ring, "_frozenFxPrefab", fx);

            PerkConfig perk = Asset<PerkConfig>();
            Set(perk, "_id", "perk.ice-ring");
            Set(perk, "_behaviour", ring);
            Set(perk, "_levels", new[] { new PerkLevel() });

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new TickFrozenSystem());
            _systems.Add(new ApplyMoveSpeedSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new IceRingSystem());
            _systems.Inject(_clock, (IViewFactory)_views, (IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perk.Id;
            taken.Level = 1;
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
            _walkers.Clear();
        }

        [Test]
        public void TheRingHurtsAndFreezesOnlyEnemiesInsideIt()
        {
            int near = Enemy(new Vector3(3f, 0f, 0f));
            int far = Enemy(new Vector3(0f, 0f, 6f));

            Step(1);

            Assert.AreEqual(Damage, Total(near), 1e-4f);
            Assert.AreEqual(0f, Total(far));
            Assert.IsTrue(_world.GetPool<Frozen>().Has(near));
            Assert.IsFalse(_world.GetPool<Frozen>().Has(far));
            Assert.IsTrue(Hits()[0].IsMagic);
            Assert.AreEqual(2, _views.Created, "the ring and the ice on the frozen enemy");
        }

        [Test]
        public void AFrozenEnemyStandsStillForTheFreezeAndWalksAgainAfter()
        {
            int enemy = Enemy(new Vector3(2f, 0f, 0f));

            Step(1);

            Vector3 frozenAt = Position(enemy);

            Step(Mathf.RoundToInt(FreezeSeconds / Tick) - 2);

            Assert.AreEqual(0f, Vector3.Distance(frozenAt, Position(enemy)), "not a centimetre while frozen");
            Assert.IsTrue(_world.GetPool<Frozen>().Has(enemy));

            Step(2);

            Assert.IsFalse(_world.GetPool<Frozen>().Has(enemy), "thawed after the freeze");

            Vector3 thawedAt = Position(enemy);

            Step(60);

            float walked = Vector3.Distance(thawedAt, Position(enemy));
            TestContext.WriteLine($"walked {walked:F3} m in the second after the thaw");
            Assert.AreEqual(WalkSpeed, walked, 0.1f);
        }

        [Test]
        public void TheBossIsHurtButNeverFrozen()
        {
            int boss = Enemy(new Vector3(2f, 0f, 0f));
            _world.GetPool<Boss>().Add(boss);

            Step(1);

            Assert.AreEqual(Damage, Total(boss), 1e-4f);
            Assert.IsFalse(_world.GetPool<Frozen>().Has(boss));
        }

        [Test]
        public void TheRingWaitsForAnEnemyAndThenForItsCooldown()
        {
            Step(30);

            Assert.AreEqual(0, _views.Created, "no ring while nobody is near");

            int enemy = Enemy(new Vector3(2f, 0f, 0f));
            _walkers.Remove(enemy);

            Step(1);

            Assert.AreEqual(1, Count(enemy));

            Step(Mathf.RoundToInt(CooldownSeconds / Tick) - 1);

            Assert.AreEqual(1, Count(enemy), "still on cooldown");

            Step(2);

            Assert.AreEqual(2, Count(enemy));
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<MoveIntent>().Add(enemy);

            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(enemy);
            speed.Base = WalkSpeed;
            speed.Value = WalkSpeed;

            _walkers.Add(enemy);

            return enemy;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                foreach (int walker in _walkers)
                    _world.GetPool<MoveIntent>().Get(walker).Value = Vector3.right;

                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private Vector3 Position(int entity)
        {
            return _world.GetPool<Position>().Get(entity).Value;
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

        private int Count(int target)
        {
            int count = 0;

            foreach (DamageEvent hit in Hits())
            {
                if (hit.Target.Unpack(_world, out int entity) && entity == target)
                    count++;
            }

            return count;
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

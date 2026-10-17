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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class ExecutionTests
    {
        private const float Tick = 1f / 60f;
        private const int Seed = 987654321;
        private const string PerkId = "perk.execution.test";
        private const string ExecutionPath = "Assets/_Project/Configs/Perks/Execution_Berserk.asset";
        private const float BossRadius = 0.7f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<KeyValuePair<int, float>> _hits = new List<KeyValuePair<int, float>>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private ExecutionConfig _execution;
        private int _hero;
        private int _boss;
        private int _step;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
            _hits.Clear();
            _step = 0;

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void ThreeHitsLandAtTheClipMomentsWithAShareOfTheCurrentHealth()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 1000f, level: 1);

            Run(AdvanceExecutionSystem.TotalTicks(_execution, Tick) + 2);

            Assert.AreEqual(3, _hits.Count);
            Assert.AreEqual(AdvanceExecutionSystem.HitTick(_execution, 0, Tick) + 1, _hits[0].Key, "0.45 s into the clip");
            Assert.AreEqual(AdvanceExecutionSystem.HitTick(_execution, 1, Tick) + 1, _hits[1].Key, "0.60 s");
            Assert.AreEqual(AdvanceExecutionSystem.HitTick(_execution, 2, Tick) + 1, _hits[2].Key, "1.00 s");

            Assert.AreEqual(0.03f * 1000f + 20f, _hits[0].Value, 1e-3f);
            Assert.AreEqual(0.03f * 950f + 20f, _hits[1].Value, 1e-3f, "the share is of the health left after the first hit");
            Assert.AreEqual(0.03f * 901.5f + 20f, _hits[2].Value, 1e-3f);

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "the marker goes when the clip ends");
        }

        [Test]
        public void BelowThirtyPercentEveryHitIsDoubled()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 250f, level: 3);

            Run(AdvanceExecutionSystem.TotalTicks(_execution, Tick) + 2);

            Assert.AreEqual(3, _hits.Count);
            Assert.AreEqual((0.05f * 250f + 40f) * 2f, _hits[0].Value, 1e-3f);
        }

        [TestCase(0f, 3.69f, true)]
        [TestCase(0f, 3.8f, false)]
        [TestCase(40f, 2f, true)]
        [TestCase(50f, 2f, false)]
        [TestCase(180f, 2f, false)]
        public void TheBossMustBeInReachAndInFront(float angleDegrees, float distance, bool starts)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;

            Build(bossAt: new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * distance, bossHealth: 1000f, level: 1);

            Run(1);

            Assert.AreEqual(starts, _world.GetPool<SpecialAttack>().Has(_hero), "reach 3 m plus the boss's 0.7 m body, sector 45 deg");
        }

        [Test]
        public void TheHeroTurnsSmoothlyOntoTheBoss()
        {
            float radians = 40f * Mathf.Deg2Rad;
            Vector3 bossAt = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * 2f;

            Build(bossAt: bossAt, bossHealth: 1000f, level: 1);

            Run(2);

            float afterOneTurn = Vector3.Angle(_world.GetPool<Facing>().Get(_hero).Value, bossAt);
            Assert.AreEqual(40f - 540f * Tick, afterOneTurn, 0.5f, "540 deg/s: one tick turns 9 deg, not a snap");

            Run(AdvanceExecutionSystem.HitTick(_execution, 0, Tick) - 1);

            Assert.AreEqual(0f, Vector3.Angle(_world.GetPool<Facing>().Get(_hero).Value, bossAt), 0.01f, "the hero faces the boss by the first hit");
        }

        [Test]
        public void AnOrdinaryEnemyIsNeverExecuted()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 1000f, level: 1);

            _world.GetPool<Boss>().Del(_boss);
            _world.GetPool<Health>().Get(_boss).Current = 1f;
            Run(1);

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "only bosses and elites are executed");
        }

        [Test]
        public void AnEliteIsExecutedLikeABossWithAHalfCooldown()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 100000f, level: 1);

            _world.GetPool<Boss>().Del(_boss);
            _world.GetPool<Elite>().Add(_boss);

            Run(1);
            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero));

            Run(AdvanceExecutionSystem.TotalTicks(_execution, Tick) + 2);

            Assert.AreEqual(3, _hits.Count, "an elite takes the whole series, not one killing hit");

            int cooldown = Mathf.RoundToInt(20f * 0.5f / Tick);
            Assert.AreEqual(cooldown - (AdvanceExecutionSystem.TotalTicks(_execution, Tick) + 2), _world.GetPool<ExecutionCooldown>().Get(_hero).RemainingTicks);
        }

        [Test]
        public void TheBossIsPreferredOverACloserWoundedEnemy()
        {
            Build(bossAt: new Vector3(0f, 0f, 2.5f), bossHealth: 1000f, level: 1);

            int wounded = _world.NewEntity();
            _world.GetPool<Enemy>().Add(wounded);
            _world.GetPool<Elite>().Add(wounded);
            _world.GetPool<Position>().Add(wounded).Value = new Vector3(0f, 0f, 1f);
            _world.GetPool<BodyRadius>().Add(wounded).Value = 0.4f;
            _world.GetPool<Health>().Add(wounded).Current = 1f;
            _world.GetPool<MaxHealth>().Add(wounded).Value = 20f;

            Run(1);

            foreach (int entity in _world.Filter<Execution>().End())
                Assert.IsTrue(_world.GetPool<Execution>().Get(entity).Target.Unpack(_world, out int target) && target == _boss);
        }

        [Test]
        public void TheCooldownCountsFromTheStart()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 100000f, level: 1);

            int cooldown = Mathf.RoundToInt(20f / Tick);

            Run(1);
            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero));

            Run(cooldown - 1);
            Assert.AreEqual(3, _hits.Count);
            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "tick 1 + 20 s - 1 is still on cooldown");

            Run(1);
            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero), "20 s after the first start");
        }

        [Test]
        public void ADashCutsTheExecution()
        {
            Build(bossAt: new Vector3(0f, 0f, 2f), bossHealth: 1000f, level: 1);

            Run(AdvanceExecutionSystem.HitTick(_execution, 0, Tick) + 2);
            Assert.AreEqual(1, _hits.Count);

            _world.GetPool<Dashing>().Add(_hero);
            Run(AdvanceExecutionSystem.TotalTicks(_execution, Tick));

            Assert.AreEqual(1, _hits.Count, "no hit lands after the dash");
            Assert.AreEqual(0, _world.Filter<Execution>().End().GetEntitiesCount());
            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _step++;
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void Build(Vector3 bossAt, float bossHealth, int level)
        {
            _execution = AssetDatabase.LoadAssetAtPath<ExecutionConfig>(ExecutionPath);
            Assert.IsNotNull(_execution, $"{ExecutionPath} is missing.");

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] levels = new PerkLevel[_execution.LevelCount];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", PerkId);
            SetField(perk, "_levels", levels);
            SetField(perk, "_behaviour", _execution);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(levelConfig, "_waves", timeline);
            _assets.Add(levelConfig);

            _world = new EcsWorld();
            _clock = new SimulationClock();

            _systems = new EcsSystems(_world);
            _systems.Add(new ExpireSpecialAttackSystem());
            _systems.Add(new FaceExecutionTargetSystem());
            _systems.Add(new StartExecutionSystem());
            _systems.Add(new AdvanceExecutionSystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new RecordHitsSystem(this));
            _systems.Add(new CleanupEventsSystem());
            _systems.Inject(
                levelConfig,
                _clock,
                new ContentRegistry(new IContentEntry[] { perk }),
                new RunContext("level.test", "character.test", RunMode.Story, Seed, Array.Empty<StatModifierSpec>()));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            _boss = _world.NewEntity();
            _world.GetPool<Enemy>().Add(_boss);
            _world.GetPool<Boss>().Add(_boss);
            _world.GetPool<Position>().Add(_boss).Value = bossAt;
            _world.GetPool<BodyRadius>().Add(_boss).Value = BossRadius;
            _world.GetPool<Health>().Add(_boss).Current = bossHealth;

            ref MaxHealth maxHealth = ref _world.GetPool<MaxHealth>().Add(_boss);
            maxHealth.Base = 1000f;
            maxHealth.Value = 1000f;

            int taken = _world.NewEntity();

            ref TakenPerk takenPerk = ref _world.GetPool<TakenPerk>().Add(taken);
            takenPerk.Owner = _world.PackEntity(_hero);
            takenPerk.PerkId = PerkId;
            takenPerk.Level = level;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class RecordHitsSystem : IEcsRunSystem
        {
            private readonly ExecutionTests _owner;

            private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;

            private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
            private readonly EcsPoolInject<DamageApplied> _appliedPool = default;

            public RecordHitsSystem(ExecutionTests owner)
            {
                _owner = owner;
            }

            public void Run(IEcsSystems systems)
            {
                foreach (int entity in _applied.Value)
                {
                    if (_damageEvents.Value.Get(entity).Kind != DamageKind.Perk)
                        continue;

                    _owner._hits.Add(new KeyValuePair<int, float>(_owner._step, _appliedPool.Value.Get(entity).Amount));
                }
            }
        }
    }
}

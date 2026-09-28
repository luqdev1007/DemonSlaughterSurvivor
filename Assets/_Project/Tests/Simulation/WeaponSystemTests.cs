using Game.Configs;
using Game.Configs.Editor;
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
    public sealed class WeaponSystemTests
    {
        private const float Tick = 1f / 60f;
        private const float ClipLength = 0.5f;
        private const int Seed = 987654321;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private StatModifiers _modifiers;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();

            _systems = null;
            _world = null;

            for (int index = 0; index < _assets.Count; index++)
                Object.DestroyImmediate(_assets[index]);

            _assets.Clear();
        }

        [Test]
        public void NoSwingWithoutAnEnemyInTheTriggerSector()
        {
            Build(Front(), 90f, 1f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, -1.5f), 1.8f);

            Run(40);

            Assert.AreEqual(0, _world.Filter<Swing>().End().GetEntitiesCount());
            Assert.AreEqual(0, DamageTo(enemy));
        }

        [Test]
        public void EnemyInFrontIsHitExactlyOncePerSwing()
        {
            Build(Front(), 90f, 5f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            Run(Mathf.CeilToInt(ClipLength / Tick) + 2);

            Assert.AreEqual(1, CountEvents(enemy));
            Assert.AreEqual(20f, DamageTo(enemy), 1e-4f);
        }

        [Test]
        public void BladeAboveTheBodyHeightDoesNotHit()
        {
            Build(Front(), 90f, 5f);
            int wolf = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 0.5f);

            Run(Mathf.CeilToInt(ClipLength / Tick) + 2);

            Assert.Greater(_swingsStarted, 0);
            Assert.AreEqual(0, CountEvents(wolf));
        }

        [Test]
        public void HalfAngleKeepsASpinFromHittingBehind()
        {
            Build(FullCircle(), 90f, 5f);
            int front = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);
            int behind = SpawnEnemy(new Vector3(0f, 0f, -1.5f), 1.8f);

            Run(Mathf.CeilToInt(ClipLength / Tick) + 2);

            Assert.AreEqual(1, CountEvents(front));
            Assert.AreEqual(0, CountEvents(behind));
        }

        [Test]
        public void WithoutTheHalfAngleTheSameSpinHitsBehind()
        {
            Build(FullCircle(), 180f, 5f);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);
            int behind = SpawnEnemy(new Vector3(0f, 0f, -1.5f), 1.8f);

            Run(Mathf.CeilToInt(ClipLength / Tick) + 2);

            Assert.AreEqual(1, CountEvents(behind));
        }

        [Test]
        public void ApproachingEnemyStartsTheSwingBeforeItEntersTheSector()
        {
            Build(Front(), 90f, 5f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 2.7f), 1.8f);

            Run(3);

            Assert.AreEqual(0, _swingsStarted, "A standing enemy just outside the trigger sector must not start a swing.");

            _world.GetPool<MoveIntent>().Add(enemy).Value = Vector3.back;

            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(enemy);
            speed.Base = 5f;
            speed.Value = 5f;

            Run(1);

            Assert.AreEqual(1, _swingsStarted, "An enemy walking in must start the swing one windup ahead.");
        }

        [Test]
        public void DashInterruptsTheSwing()
        {
            Build(Front(), 90f, 5f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            Run(2);

            Assert.AreEqual(1, _world.Filter<Swing>().End().GetEntitiesCount());

            _world.GetPool<Dashing>().Add(_hero);

            Run(Mathf.CeilToInt(ClipLength / Tick));

            Assert.AreEqual(0, _world.Filter<Swing>().End().GetEntitiesCount());
            Assert.AreEqual(0, CountEvents(enemy));
        }

        [Test]
        public void CooldownShorterThanTheClipDoesNotOverlapSwings()
        {
            Build(Front(), 90f, 0.1f);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            int maxAlive = 0;
            List<int> startTicks = new List<int>();

            for (int tick = 0; tick < 120; tick++)
            {
                int before = _swingsStarted;

                Run(1);

                if (_swingsStarted > before)
                    startTicks.Add(tick);

                maxAlive = Mathf.Max(maxAlive, _world.Filter<Swing>().End().GetEntitiesCount());
            }

            Assert.AreEqual(1, maxAlive);
            Assert.Greater(startTicks.Count, 2);

            for (int index = 1; index < startTicks.Count; index++)
                Assert.GreaterOrEqual(startTicks[index] - startTicks[index - 1], Mathf.CeilToInt(ClipLength / Tick));
        }

        [Test]
        public void VariantChoiceRepeatsForTheSameSeedAndDiffersForAnotherWeapon()
        {
            List<int> first = VariantSequence("weapon.berserk_sword");
            List<int> second = VariantSequence("weapon.berserk_sword");
            List<int> other = VariantSequence("weapon.other");

            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreNotEqual(first, other);
            CollectionAssert.Contains(first, 0);
            CollectionAssert.Contains(first, 1);
        }

        [Test]
        public void WeaponInheritsTheOwnerDamageModifier()
        {
            Build(Front(), 90f, 5f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            _modifiers.Add(_hero, StatId.WeaponDamage, StatOp.Increased, 0.5f, "upgrade.weapon_damage#1");

            Run(Mathf.CeilToInt(ClipLength / Tick) + 2);

            Assert.AreEqual(30f, DamageTo(enemy), 1e-4f);
        }

        [TestCase(1f, 30)]
        [TestCase(1.5f, 20)]
        [TestCase(2f, 15)]
        [TestCase(1.3f, 23)]
        public void SwingLastsTheRoundedTicksOfItsAttackSpeed(float attackSpeed, int expectedTicks)
        {
            Build(Front(), 90f, 5f);
            int enemy = SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            SetAttackSpeed(attackSpeed);

            Assert.AreEqual(expectedTicks, MeasureNextSwing(out float playbackSpeed, out float firstClipTime));
            Assert.AreEqual(ClipLength / (expectedTicks * Tick), playbackSpeed, 1e-5f);
            Assert.AreEqual(playbackSpeed * Tick, firstClipTime, 1e-6f, "the clip must advance by the snapshot speed, not the config speed");
            Assert.AreEqual(1, CountEvents(enemy), "a faster swing must still sweep its whole hit window");
        }

        [Test]
        public void ModifierAddedMidSwingKeepsTheSnapshotAndTheNextSwingTakesIt()
        {
            Build(Front(), 90f, 0.1f);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            EcsFilter swings = _world.Filter<Swing>().End();
            EcsPool<Swing> swingPool = _world.GetPool<Swing>();

            for (int guard = 0; guard < 10 && swings.GetEntitiesCount() == 0; guard++)
                Run(1);

            Assert.AreEqual(1, swings.GetEntitiesCount());

            float snapshot = SingleSwing(swings, swingPool).PlaybackSpeed;
            int ticks = 1;

            _modifiers.Add(_hero, StatId.AttackSpeed, StatOp.More, 1f, "test.attack_speed");

            while (swings.GetEntitiesCount() > 0)
            {
                Assert.AreEqual(snapshot, SingleSwing(swings, swingPool).PlaybackSpeed, "the snapshot must not follow a modifier added mid-swing");

                Run(1);
                ticks++;

                Assert.Less(ticks, 100);
            }

            Assert.AreEqual(30, ticks);
            Assert.AreEqual(2f, _world.GetPool<AttackSpeed>().Get(_weapon).Value, 1e-6f);

            Assert.AreEqual(15, MeasureNextSwing(out float next, out _));
            Assert.AreEqual(2f, next, 1e-5f);
        }

        [Test]
        public void CooldownIsDividedByAttackSpeed()
        {
            Build(Front(), 90f, 1f);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            SetAttackSpeed(2f);

            List<int> startTicks = new List<int>();

            for (int tick = 0; tick < 200; tick++)
            {
                int before = _swingsStarted;

                Run(1);

                if (_swingsStarted > before)
                    startTicks.Add(tick);
            }

            Assert.Greater(startTicks.Count, 3);

            for (int index = 1; index < startTicks.Count; index++)
            {
                int interval = startTicks[index] - startTicks[index - 1];

                Assert.GreaterOrEqual(interval, 30);
                Assert.LessOrEqual(interval, 31, "a 1 s cooldown at attack speed 2 must last half a second, not a whole one");
            }
        }

        [TestCase(-1f)]
        [TestCase(-2f)]
        public void NonPositiveAttackSpeedFailsLoudly(float more)
        {
            Build(Front(), 90f, 5f);

            _modifiers.Add(_hero, StatId.AttackSpeed, StatOp.More, more, "test.broken_attack_speed");

            System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() => Run(1));

            StringAssert.Contains("AttackSpeed", exception.Message);
            StringAssert.Contains("test.broken_attack_speed", exception.Message);
        }

        [Test]
        public void SwingTraceRepeatsForTheSameSeedWithAttackSpeedChanges()
        {
            List<int> first = SpeedTrace();
            List<int> second = SpeedTrace();

            Assert.Greater(first.Count, 0);
            CollectionAssert.AreEqual(first, second);
        }

        private List<int> SpeedTrace()
        {
            Build(Front(), 90f, 0.1f, 2);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            EcsFilter swings = _world.Filter<Swing>().End();
            EcsPool<Swing> swingPool = _world.GetPool<Swing>();
            List<int> trace = new List<int>();

            for (int tick = 0; tick < 600; tick++)
            {
                if (tick == 100)
                    _modifiers.Add(_hero, StatId.AttackSpeed, StatOp.More, 0.5f, "test.attack_speed");

                if (tick == 250)
                    _modifiers.Add(_hero, StatId.AttackSpeed, StatOp.More, 1.2f, "test.attack_speed");

                if (tick == 400)
                    _modifiers.RemoveBySource(_hero, "test.attack_speed");

                Run(1);

                foreach (int entity in swings)
                {
                    ref Swing swing = ref swingPool.Get(entity);

                    trace.Add(swing.Variant);
                    trace.Add(swing.RemainingTicks);
                    trace.Add(System.BitConverter.SingleToInt32Bits(swing.PlaybackSpeed));
                    trace.Add(System.BitConverter.SingleToInt32Bits(swing.ClipTime));
                }
            }

            TearDown();

            return trace;
        }

        private void SetAttackSpeed(float attackSpeed)
        {
            if (attackSpeed == 1f)
                return;

            _modifiers.Add(_hero, StatId.AttackSpeed, StatOp.More, attackSpeed - 1f, "test.attack_speed");
        }

        private int MeasureNextSwing(out float playbackSpeed, out float firstClipTime)
        {
            EcsFilter swings = _world.Filter<Swing>().End();
            EcsPool<Swing> swingPool = _world.GetPool<Swing>();

            playbackSpeed = 0f;
            firstClipTime = 0f;

            int ticks = 0;
            bool started = false;

            for (int guard = 0; guard < 400; guard++)
            {
                int before = _swingsStarted;

                Run(1);

                if (started == false && _swingsStarted > before)
                {
                    started = true;

                    Swing swing = SingleSwing(swings, swingPool);
                    playbackSpeed = swing.PlaybackSpeed;
                    firstClipTime = swing.ClipTime;
                }

                if (started == false)
                    continue;

                ticks++;

                if (swings.GetEntitiesCount() == 0)
                    return ticks;
            }

            Assert.Fail("no swing finished within the guard");

            return -1;
        }

        private static Swing SingleSwing(EcsFilter swings, EcsPool<Swing> swingPool)
        {
            foreach (int entity in swings)
                return swingPool.Get(entity);

            Assert.Fail("expected a live swing");

            return default;
        }

        private int _swingsStarted;
        private int _weapon;
        private WeaponConfig _config;
        private readonly List<int> _startedVariants = new List<int>();

        private List<int> VariantSequence(string weaponId)
        {
            Build(Front(), 90f, 0.1f, 2, weaponId);
            SpawnEnemy(new Vector3(0f, 0f, 1.5f), 1.8f);

            Run(600);

            List<int> variants = new List<int>(_startedVariants);

            TearDown();

            return variants;
        }

        private void Build(SwingBake bake, float halfAngle, float cooldown, int variantCount = 1, string weaponId = "weapon.test")
        {
            _world = new EcsWorld();
            _clock = new SimulationClock();
            _modifiers = new StatModifiers(_world);
            _swingsStarted = 0;
            _startedVariants.Clear();

            LevelConfig level = CreateLevel();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            _config = CreateWeapon(bake, halfAngle, cooldown, variantCount, weaponId);

            _systems = new EcsSystems(_world);
            _systems.Add(new RecomputeStatsSystem());
            _systems.Add(new TickWeaponCooldownSystem());
            _systems.Add(new InterruptSwingSystem());
            _systems.Add(new StartSwingSystem());
            _systems.Add(new AdvanceSwingSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, grid, _clock, _modifiers, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            int weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(weapon).Config = _config;

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(weapon);
            damage.Base = 20f;
            damage.Value = 20f;

            ref WeaponCooldown weaponCooldown = ref _world.GetPool<WeaponCooldown>().Add(weapon);
            weaponCooldown.Base = cooldown;
            weaponCooldown.Value = cooldown;

            ref AttackSpeed attackSpeed = ref _world.GetPool<AttackSpeed>().Add(weapon);
            attackSpeed.Base = 1f;
            attackSpeed.Value = 1f;

            _weapon = weapon;

            _world.GetPool<WeaponReady>().Add(weapon);
            _world.GetPool<SwingRandom>().Add(weapon).State = SimulationRandom.StreamState(Seed, weaponId);
            _world.GetPool<OwnerLink>().Add(weapon).Owner = _world.PackEntity(_hero);

            _modifiers.MarkDirty(weapon);
        }

        private void Run(int ticks)
        {
            EcsFilter started = _world.Filter<SwingStarted>().End();

            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();

                _swingsStarted += started.GetEntitiesCount();

                List<int> clear = new List<int>();

                foreach (int entity in started)
                {
                    clear.Add(entity);
                    _startedVariants.Add(_world.GetPool<Swing>().Get(entity).Variant);
                }

                for (int index = 0; index < clear.Count; index++)
                    _world.GetPool<SwingStarted>().Del(clear[index]);
            }
        }

        private int SpawnEnemy(Vector3 position, float height)
        {
            int entity = _world.NewEntity();

            _world.GetPool<Enemy>().Add(entity);
            _world.GetPool<Position>().Add(entity).Value = position;
            _world.GetPool<BodyRadius>().Add(entity).Value = 0.4f;
            _world.GetPool<BodyHeight>().Add(entity).Value = height;

            return entity;
        }

        private int CountEvents(int target)
        {
            int count = 0;
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (events.Get(entity).Target.Unpack(_world, out int unpacked) && unpacked == target)
                    count++;
            }

            return count;
        }

        private float DamageTo(int target)
        {
            float total = 0f;
            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (events.Get(entity).Target.Unpack(_world, out int unpacked) && unpacked == target)
                    total += events.Get(entity).Amount;
            }

            return total;
        }

        private SwingBake Front()
        {
            return SweepBake(-60f, 60f);
        }

        private SwingBake FullCircle()
        {
            return SweepBake(-180f, 180f);
        }

        private SwingBake SweepBake(float fromDegrees, float toDegrees)
        {
            const float rate = 240f;
            int count = Mathf.FloorToInt(ClipLength * rate + 1e-3f) + 1;

            Vector3[] hands = new Vector3[count];
            Vector3[] tips = new Vector3[count];

            for (int index = 0; index < count; index++)
            {
                float time = index / rate;
                float progress = Mathf.Clamp01((time - 0.1f) / 0.2f);
                float angle = Mathf.Lerp(fromDegrees, toDegrees, progress) * Mathf.Deg2Rad;

                hands[index] = new Vector3(0f, 0.8f, 0f);
                tips[index] = new Vector3(Mathf.Sin(angle) * 2f, 0.8f, Mathf.Cos(angle) * 2f);
            }

            SwingBake bake = ScriptableObject.CreateInstance<SwingBake>();
            bake.Overwrite(null, rate, ClipLength, 0u, hands, tips);
            _assets.Add(bake);

            return bake;
        }

        private WeaponConfig CreateWeapon(SwingBake bake, float halfAngle, float cooldown, int variantCount, string weaponId)
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            _assets.Add(config);

            SwingVariant[] variants = new SwingVariant[variantCount];

            for (int index = 0; index < variantCount; index++)
            {
                SwingVariant variant = new SwingVariant();
                SetField(variant, "_animatorTrigger", "Test" + index);
                SetField(variant, "_playbackSpeed", 1f);
                SetField(variant, "_halfAngleDegrees", halfAngle);
                SetField(variant, "_bake", bake);

                SwingCoverage coverage = SwingBaker.ComputeCoverage(bake, 1f, 0.1f, 0.3f, halfAngle, 1.77f, 0.4f);
                variant.OverwriteBakeResults(0.1f, 0.3f, SwingBaker.ResolveMaxReach(bake, 0.1f, 0.3f), coverage);

                variants[index] = variant;
            }

            SetField(config, "_id", weaponId);
            SetField(config, "_damage", 20f);
            SetField(config, "_cooldownSeconds", cooldown);
            SetField(config, "_variants", variants);

            SwingCoverage trigger = SwingBaker.ComputeCoverage(bake, 1f, 0.1f, 0.3f, Mathf.Min(halfAngle, 90f), 1.77f, 0.4f);
            config.OverwriteTriggerCoverage(trigger);

            return config;
        }

        private LevelConfig CreateLevel()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            return level;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}

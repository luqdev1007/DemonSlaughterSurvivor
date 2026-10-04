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

namespace Game.Simulation.Tests
{
    public sealed class RageTests
    {
        private const float TickSeconds = 1f / 60f;
        private const string CharacterId = "character.test";
        private const float EnemyHealth = 1000000f;
        private const float Tolerance = 1e-5f;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private int _hero;
        private int[] _enemies;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;

            foreach (ScriptableObject asset in _assets)
                UnityEngine.Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void DealtHitAddsFlatRageRegardlessOfDamage()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Hit(_enemies[0], 5f));

            Assert.AreEqual(0.2f, Charge(), Tolerance);

            Step(() => Hit(_enemies[0], 500f));

            Assert.AreEqual(0.4f, Charge(), Tolerance);

            Step(() =>
            {
                Hit(_enemies[0], 1f);
                Hit(_enemies[1], 1f);
                Hit(_enemies[2], 1f);
            });

            Assert.AreEqual(1.0f, Charge(), Tolerance);
        }

        [TestCase(100f, 20f, 20f)]
        [TestCase(200f, 20f, 10f)]
        [TestCase(50f, 5f, 10f)]
        public void ReceivedDamageAddsFractionOfMaxHealth(float maxHealth, float damage, float expected)
        {
            Build(heroMaxHealth: maxHealth, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], damage));

            Assert.AreEqual(expected, Charge(), Tolerance);
        }

        [Test]
        public void DamageInsideInvulnerabilityGivesNoRage()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0.5f);

            Step(() => Receive(_enemies[0], 10f));

            Assert.AreEqual(10f, Charge(), Tolerance);
            Assert.IsTrue(_world.GetPool<Invulnerable>().Has(_hero));

            Step(() => Receive(_enemies[1], 30f));

            Assert.AreEqual(10f, Charge(), Tolerance);
            Assert.AreEqual(2, State().TicksSinceCombat, "an event swallowed by i-frames must not count as combat");
        }

        [Test]
        public void DecayWaitsForTheDelayThenFallsLinearly()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 50f));

            Assert.AreEqual(50f, Charge(), Tolerance);

            int delayTicks = Mathf.RoundToInt(3f / TickSeconds);

            for (int tick = 1; tick < delayTicks; tick++)
            {
                Step(null);

                Assert.AreEqual(50f, Charge(), Tolerance, $"rage decayed {tick} ticks after combat, before the {delayTicks}-tick delay");
            }

            Step(null);

            Assert.AreEqual(50f - 4f * TickSeconds, Charge(), Tolerance);

            for (int tick = 0; tick < 59; tick++)
                Step(null);

            Assert.AreEqual(46f, Charge(), 1e-3f);
        }

        [Test]
        public void CombatRestartsTheDelay()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 50f));

            for (int tick = 1; tick < 150; tick++)
                Step(null);

            Step(() => Hit(_enemies[0], 1f));

            for (int tick = 1; tick < 180; tick++)
                Step(null);

            Assert.AreEqual(50.2f, Charge(), Tolerance);

            Step(null);

            Assert.Less(Charge(), 50.2f);
        }

        [TestCase(DamageKind.Unmarked)]
        [TestCase(DamageKind.Perk)]
        public void DealtDamageOfOtherKindsGivesNoRageAndKeepsTheDelay(DamageKind kind)
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 50f));

            for (int tick = 1; tick < 150; tick++)
                Step(null);

            int before = State().TicksSinceCombat;

            Step(() => Hit(_enemies[0], 1f, kind));

            Assert.AreEqual(50f, Charge(), Tolerance);
            Assert.AreEqual(before + 1, State().TicksSinceCombat, $"{kind} damage must not restart the decay delay");
        }

        [TestCase(DamageKind.Unmarked)]
        [TestCase(DamageKind.Perk)]
        public void ReceivedDamageOfOtherKindsGivesNoRage(DamageKind kind)
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 20f, kind));

            Assert.AreEqual(0f, Charge(), Tolerance);
            Assert.AreEqual(1, State().TicksSinceCombat);
        }

        [Test]
        public void UnmarkedIsTheDefaultKind()
        {
            Assert.AreEqual(DamageKind.Unmarked, default(DamageKind));
            Assert.AreEqual(DamageKind.Unmarked, default(DamageEvent).Kind);
        }

        [Test]
        public void FullRageDoesNotDecay()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 150f));

            Assert.AreEqual(100f, Charge());

            for (int tick = 0; tick < 600; tick++)
                Step(null);

            Assert.AreEqual(100f, Charge());
        }

        [Test]
        public void ChargeIsClampedAtBothEnds()
        {
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0f);

            Step(() => Receive(_enemies[0], 99.9f));
            Step(() => Receive(_enemies[0], 99.9f));

            Assert.AreEqual(100f, Charge());

            ref UltimateCharge charge = ref _world.GetPool<UltimateCharge>().Get(_hero);
            charge.Value = 0.01f;

            for (int tick = 0; tick < 400; tick++)
                Step(null);

            Assert.AreEqual(0f, Charge());
        }

        [Test]
        public void SameSeedGivesByteIdenticalRage()
        {
            byte[] first = RecordSeededRun(987654321);
            byte[] second = RecordSeededRun(987654321);
            byte[] other = RecordSeededRun(123456789);

            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreNotEqual(first, other);
        }

        private byte[] RecordSeededRun(int seed)
        {
            TearDown();
            Build(heroMaxHealth: 100f, hitInvulnerabilitySeconds: 0.5f);

            SimulationRandom random = new SimulationRandom(seed);
            List<byte> trace = new List<byte>();

            for (int tick = 0; tick < 3600; tick++)
            {
                float roll = random.NextUnit();
                int enemy = _enemies[(int)(random.NextUnit() * _enemies.Length) % _enemies.Length];
                float amount = 1f + random.NextUnit() * 30f;

                Step(() =>
                {
                    if (roll < 0.08f)
                        Hit(enemy, amount);
                    else if (roll < 0.1f)
                        Receive(enemy, amount);
                });

                trace.AddRange(BitConverter.GetBytes(Charge()));
                trace.AddRange(BitConverter.GetBytes(State().TicksSinceCombat));

                ref Health health = ref _world.GetPool<Health>().Get(_hero);
                health.Current = 100f;
            }

            return trace.ToArray();
        }

        private void Build(float heroMaxHealth, float hitInvulnerabilitySeconds)
        {
            RageConfig rage = ScriptableObject.CreateInstance<RageConfig>();
            SetField(rage, "_id", "resource.rage.test");
            _assets.Add(rage);

            CharacterConfig character = ScriptableObject.CreateInstance<CharacterConfig>();
            SetField(character, "_id", CharacterId);
            SetField(character, "_rage", rage);
            _assets.Add(character);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { character });
            RunContext context = new RunContext("level.test", CharacterId, RunMode.Story, 1, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _systems = new EcsSystems(_world);

            _systems.Add(new TickInvulnerabilitySystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new AccumulateRageSystem());
            _systems.Add(new DecayRageSystem());
            _systems.Add(new CleanupEventsSystem());
            _systems.Inject(context, registry, level, _clock);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);

            ref MaxHealth maxHealth = ref _world.GetPool<MaxHealth>().Add(_hero);
            maxHealth.Base = heroMaxHealth;
            maxHealth.Value = heroMaxHealth;

            ref Health health = ref _world.GetPool<Health>().Add(_hero);
            health.Current = heroMaxHealth * 1000f;

            ref HitInvulnerability hitInvulnerability = ref _world.GetPool<HitInvulnerability>().Add(_hero);
            hitInvulnerability.Seconds = hitInvulnerabilitySeconds;

            ref UltimateCharge charge = ref _world.GetPool<UltimateCharge>().Add(_hero);
            charge.Max = rage.Max;

            _world.GetPool<RageState>().Add(_hero);

            _enemies = new int[3];

            for (int index = 0; index < _enemies.Length; index++)
            {
                int enemy = _world.NewEntity();
                _world.GetPool<Enemy>().Add(enemy);

                ref Health enemyHealth = ref _world.GetPool<Health>().Add(enemy);
                enemyHealth.Current = EnemyHealth;

                _enemies[index] = enemy;
            }
        }

        private void Step(Action emit)
        {
            _clock.Advance(TickSeconds);
            emit?.Invoke();
            _systems.Run();
        }

        private void Hit(int enemy, float amount, DamageKind kind = DamageKind.Weapon)
        {
            AddEvent(_hero, enemy, amount, kind);
        }

        private void Receive(int enemy, float amount, DamageKind kind = DamageKind.Contact)
        {
            AddEvent(enemy, _hero, amount, kind);
        }

        private void AddEvent(int source, int target, float amount, DamageKind kind)
        {
            int entity = _world.NewEntity();

            ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Add(entity);
            damageEvent.Source = _world.PackEntity(source);
            damageEvent.Target = _world.PackEntity(target);
            damageEvent.Amount = amount;
            damageEvent.Kind = kind;
        }

        private float Charge()
        {
            return _world.GetPool<UltimateCharge>().Get(_hero).Value;
        }

        private RageState State()
        {
            return _world.GetPool<RageState>().Get(_hero);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}

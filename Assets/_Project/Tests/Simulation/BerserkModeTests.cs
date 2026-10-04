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
    public sealed class BerserkModeTests
    {
        private const float TickSeconds = 1f / 60f;
        private const string CharacterId = "character.test";
        private const float BaseMoveSpeed = 5f;
        private const float BaseWeaponDamage = 20f;
        private const float HeroMaxHealth = 100f;
        private const int ModeTicks = 480;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private StatModifiers _modifiers;
        private FakeInput _input;
        private FakeView _view;
        private int _hero;
        private int _weapon;
        private int _enemy;

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
        public void ActivatesOnlyAtFullRage()
        {
            Build();

            SetCharge(99.9f);
            Press();
            Step();

            Assert.IsFalse(InMode(), "the ultimate must not start below the maximum");

            Step(8);
            SetCharge(100f);
            Press();
            Step();

            Assert.IsTrue(InMode());
            Assert.AreEqual(ModeTicks, Mode().TotalTicks);
        }

        [Test]
        public void PressWithinTheBufferStartsWhenRageFills()
        {
            Build();

            SetCharge(99f);
            Press();
            Step(6);

            Assert.IsFalse(InMode());

            SetCharge(100f);
            Step();

            Assert.IsTrue(InMode(), "a press 100 ms before the rage fills is still inside the 120 ms buffer");
        }

        [Test]
        public void PressOlderThanTheBufferIsForgotten()
        {
            Build();

            SetCharge(99f);
            Press();
            Step(8);

            SetCharge(100f);
            Step();

            Assert.IsFalse(InMode(), "a press 133 ms old must have expired");
            Assert.IsFalse(_world.GetPool<UltimateRequest>().Has(_hero));
        }

        [Test]
        public void RageStaysFullDuringTheMode()
        {
            Build();
            StartMode();

            for (int tick = 0; tick < ModeTicks - 2; tick++)
            {
                if (tick % 30 == 0)
                    Hit(1f);

                if (tick % 45 == 0)
                    Receive(5f);

                Step();

                Assert.AreEqual(100f, Charge(), $"rage moved on mode tick {tick}");
            }
        }

        [Test]
        public void ModeAppliesItsModifiersAndRemovesThemAtTheEnd()
        {
            Build();
            StartMode();

            Assert.AreEqual(BaseMoveSpeed * 1.3f, _world.GetPool<MoveSpeed>().Get(_hero).Value, 1e-5f);
            Assert.AreEqual(1.5f, _world.GetPool<AttackSpeed>().Get(_weapon).Value, 1e-6f);
            Assert.AreEqual(BaseWeaponDamage * 1.5f, _world.GetPool<WeaponDamage>().Get(_weapon).Value, 1e-5f);
            Assert.AreEqual(1.5f, _world.GetPool<DamageTaken>().Get(_hero).Value, 1e-6f);

            int modeTicks = 1;

            while (InMode())
            {
                Step();
                modeTicks++;

                Assert.Less(modeTicks, ModeTicks + 10);
            }

            Assert.AreEqual(ModeTicks + 1, modeTicks, "the mode lasts exactly its tick count; the extra tick is the one that ends it");
            Assert.AreEqual(BaseMoveSpeed, _world.GetPool<MoveSpeed>().Get(_hero).Value);
            Assert.AreEqual(1f, _world.GetPool<AttackSpeed>().Get(_weapon).Value);
            Assert.AreEqual(BaseWeaponDamage, _world.GetPool<WeaponDamage>().Get(_weapon).Value);
            Assert.AreEqual(1f, _world.GetPool<DamageTaken>().Get(_hero).Value);
            Assert.AreEqual(0f, Charge());
            Assert.AreEqual(1, _world.GetPool<RageState>().Get(_hero).TicksSinceCombat, "reset at the end, then counted once by the decay later in the same tick, as after a hit");
            Assert.AreEqual(0, CountModifiers(StartBerserkSystem.SourceId));
        }

        [Test]
        public void DamageTakenMultipliesTheAppliedAmount()
        {
            Build();
            StartMode();

            float before = _world.GetPool<Health>().Get(_hero).Current;

            Receive(10f);
            Step();

            Assert.AreEqual(15f, AppliedTo(_hero), 1e-5f);
            Assert.AreEqual(before - 15f, _world.GetPool<Health>().Get(_hero).Current, 1e-4f);
            Assert.AreEqual(10f, RawTo(_hero), 1e-6f, "the event itself stays raw");
        }

        [Test]
        public void StrongestHitIsChosenBeforeTheMultiplier()
        {
            Build();
            StartMode();

            Receive(10f);
            Receive(12f);
            Step();

            Assert.AreEqual(18f, AppliedTo(_hero), 1e-5f);
        }

        [Test]
        public void MaxHealthIsNotTouched()
        {
            Build();

            float healthBefore = _world.GetPool<Health>().Get(_hero).Current;

            StartMode();

            Assert.AreEqual(HeroMaxHealth, _world.GetPool<MaxHealth>().Get(_hero).Value);
            Assert.AreEqual(healthBefore, _world.GetPool<Health>().Get(_hero).Current, "starting the mode must not heal or hurt");

            while (InMode())
                Step();

            Assert.AreEqual(HeroMaxHealth, _world.GetPool<MaxHealth>().Get(_hero).Value);
            Assert.AreEqual(healthBefore, _world.GetPool<Health>().Get(_hero).Current);
        }

        [Test]
        public void SecondPressDuringTheModeDoesNothing()
        {
            Build();
            StartMode();

            Step(100);

            int remaining = Mode().RemainingTicks;

            Press();
            Step();

            Assert.AreEqual(remaining - 1, Mode().RemainingTicks, "a second press must not restart or extend the mode");
            Assert.AreEqual(ModeTicks, Mode().TotalTicks);
            Assert.AreEqual(4, CountModifiers(StartBerserkSystem.SourceId));

            Step(10);

            Assert.IsFalse(_world.GetPool<UltimateRequest>().Has(_hero), "the press expires instead of waiting for the mode to end");
        }

        [Test]
        public void DeadHeroDoesNotStartTheMode()
        {
            Build();

            SetCharge(100f);
            _world.GetPool<Dead>().Add(_hero);

            Press();
            Step(3);

            Assert.IsFalse(InMode());
        }

        [Test]
        public void ViewReceivesModePresenceEveryTick()
        {
            Build();

            Step(5);
            SetCharge(100f);
            Press();

            int total = 5 + ModeTicks + 10;

            for (int tick = 5; tick < total; tick++)
            {
                Step();

                Assert.AreEqual(tick + 1, _view.Berserk.Count, $"tick {tick}: the view must be told the state every tick");
                Assert.AreEqual(InMode(), _view.Berserk[tick], $"tick {tick}: the view state must equal BerserkMode presence");
            }

            Assert.AreEqual(ModeTicks, CountTrue(_view.Berserk), "the view must see the mode for exactly its duration");
        }

        [Test]
        public void HeroDeathDoesNotCutTheModeForTheView()
        {
            Build();
            StartMode();

            Step(60);
            _world.GetPool<Dead>().Add(_hero);

            int ticksAfterDeath = 0;

            while (InMode())
            {
                Step();
                ticksAfterDeath++;

                Assert.AreEqual(InMode(), _view.Berserk[_view.Berserk.Count - 1]);
            }

            Assert.AreEqual(ModeTicks - 60, ticksAfterDeath, "the mode must run to its end on a dead hero");
            Assert.AreEqual(ModeTicks, CountTrue(_view.Berserk), "the view must see the mode until its end, death included");
            Assert.IsFalse(_view.Berserk[_view.Berserk.Count - 1], "the view must be told when the mode ends");
        }

        [Test]
        public void SameSeedGivesTheSameTrace()
        {
            byte[] first = RecordSeededRun(987654321);
            byte[] second = RecordSeededRun(987654321);

            CollectionAssert.AreEqual(first, second);
        }

        private byte[] RecordSeededRun(int seed)
        {
            TearDown();
            Build();

            SimulationRandom random = new SimulationRandom(seed);
            List<byte> trace = new List<byte>();

            for (int tick = 0; tick < 1800; tick++)
            {
                float roll = random.NextUnit();

                if (roll < 0.1f)
                    Hit(1f + random.NextUnit() * 20f);
                else if (roll < 0.14f)
                    Receive(1f + random.NextUnit() * 20f);
                else if (roll < 0.16f)
                    Press();

                Step();

                ref Health health = ref _world.GetPool<Health>().Get(_hero);
                health.Current = HeroMaxHealth;

                trace.AddRange(BitConverter.GetBytes(Charge()));
                trace.AddRange(BitConverter.GetBytes(InMode() ? Mode().RemainingTicks : -1));
                trace.AddRange(BitConverter.GetBytes(_world.GetPool<MoveSpeed>().Get(_hero).Value));
                trace.AddRange(BitConverter.GetBytes(_world.GetPool<DamageTaken>().Get(_hero).Value));
            }

            return trace.ToArray();
        }

        private void Build()
        {
            RageConfig rage = ScriptableObject.CreateInstance<RageConfig>();
            SetField(rage, "_id", "resource.rage.test");
            _assets.Add(rage);

            BerserkAbilityConfig ultimate = ScriptableObject.CreateInstance<BerserkAbilityConfig>();
            SetField(ultimate, "_id", "ability.ult.test");
            SetField(ultimate, "_durationSeconds", 8f);
            SetField(ultimate, "_modifiers", new[]
            {
                Entry(StatId.MoveSpeed, 0.3f),
                Entry(StatId.AttackSpeed, 0.5f),
                Entry(StatId.WeaponDamage, 0.5f),
                Entry(StatId.DamageTaken, 0.5f),
            });
            _assets.Add(ultimate);

            CharacterConfig character = ScriptableObject.CreateInstance<CharacterConfig>();
            SetField(character, "_id", CharacterId);
            SetField(character, "_rage", rage);
            SetField(character, "_ultimate", ultimate);
            _assets.Add(character);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            InputConfig inputConfig = ScriptableObject.CreateInstance<InputConfig>();
            _assets.Add(inputConfig);

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { character });
            RunContext context = new RunContext("level.test", CharacterId, RunMode.Story, 1, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _modifiers = new StatModifiers(_world);
            _input = new FakeInput();
            _systems = new EcsSystems(_world);

            _systems.Add(new ReadUltimateInputSystem());
            _systems.Add(new TickBerserkModeSystem());
            _systems.Add(new StartBerserkSystem());
            _systems.Add(new ExpireUltimateRequestSystem());
            _systems.Add(new RecomputeStatsSystem());
            _systems.Add(new TickInvulnerabilitySystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new AccumulateRageSystem());
            _systems.Add(new DecayRageSystem());
            _systems.Add(new SyncBerserkViewSystem());
            _systems.Inject(context, registry, level, _clock, _modifiers, _input, inputConfig, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);

            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(_hero);
            speed.Base = BaseMoveSpeed;
            speed.Value = BaseMoveSpeed;

            ref MaxHealth maxHealth = ref _world.GetPool<MaxHealth>().Add(_hero);
            maxHealth.Base = HeroMaxHealth;
            maxHealth.Value = HeroMaxHealth;

            _world.GetPool<Health>().Add(_hero).Current = HeroMaxHealth;

            ref DamageTaken damageTaken = ref _world.GetPool<DamageTaken>().Add(_hero);
            damageTaken.Base = 1f;
            damageTaken.Value = 1f;

            _world.GetPool<HitInvulnerability>().Add(_hero).Seconds = 0f;
            _world.GetPool<UltimateCharge>().Add(_hero).Max = 100f;
            _world.GetPool<RageState>().Add(_hero);

            _view = new FakeView();
            _world.GetPool<View>().Add(_hero).Value = _view;

            _weapon = _world.NewEntity();

            ref WeaponDamage weaponDamage = ref _world.GetPool<WeaponDamage>().Add(_weapon);
            weaponDamage.Base = BaseWeaponDamage;
            weaponDamage.Value = BaseWeaponDamage;

            ref AttackSpeed attackSpeed = ref _world.GetPool<AttackSpeed>().Add(_weapon);
            attackSpeed.Base = 1f;
            attackSpeed.Value = 1f;

            _world.GetPool<OwnerLink>().Add(_weapon).Owner = _world.PackEntity(_hero);

            _enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(_enemy);
            _world.GetPool<Health>().Add(_enemy).Current = 1000000f;

            _modifiers.MarkDirty(_hero);
            _modifiers.MarkDirty(_weapon);
        }

        private void StartMode()
        {
            SetCharge(100f);
            Press();
            Step();

            Assert.IsTrue(InMode(), "fixture: the mode did not start");
        }

        private void Step(int ticks = 1)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(TickSeconds);
                _systems.Run();
                ClearEvents();
            }
        }

        private void ClearEvents()
        {
            _lastApplied.Clear();
            _lastRaw.Clear();

            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();
            EcsPool<DamageApplied> applied = _world.GetPool<DamageApplied>();
            List<int> doomed = new List<int>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (applied.Has(entity) && events.Get(entity).Target.Unpack(_world, out int target))
                {
                    _lastApplied[target] = applied.Get(entity).Amount;
                    _lastRaw[target] = events.Get(entity).Amount;
                }

                doomed.Add(entity);
            }

            foreach (int entity in doomed)
                _world.DelEntity(entity);
        }

        private readonly Dictionary<int, float> _lastApplied = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _lastRaw = new Dictionary<int, float>();

        private float AppliedTo(int target)
        {
            Assert.IsTrue(_lastApplied.ContainsKey(target), "no damage was applied to the target in the last tick");

            return _lastApplied[target];
        }

        private float RawTo(int target)
        {
            return _lastRaw[target];
        }

        private void Press()
        {
            _input.UltimatePressed = true;
        }

        private void Hit(float amount)
        {
            AddEvent(_hero, _enemy, amount, DamageKind.Weapon);
        }

        private void Receive(float amount)
        {
            AddEvent(_enemy, _hero, amount, DamageKind.Contact);
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

        private void SetCharge(float value)
        {
            _world.GetPool<UltimateCharge>().Get(_hero).Value = value;
        }

        private float Charge()
        {
            return _world.GetPool<UltimateCharge>().Get(_hero).Value;
        }

        private bool InMode()
        {
            return _world.GetPool<BerserkMode>().Has(_hero);
        }

        private BerserkMode Mode()
        {
            return _world.GetPool<BerserkMode>().Get(_hero);
        }

        private int CountModifiers(string sourceId)
        {
            int count = 0;
            EcsPool<StatModifier> pool = _world.GetPool<StatModifier>();

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                if (pool.Get(entity).SourceId == sourceId)
                    count++;
            }

            return count;
        }

        private static StatModifierEntry Entry(StatId stat, float value)
        {
            StatModifierEntry entry = new StatModifierEntry();
            SetField(entry, "_stat", stat);
            SetField(entry, "_op", StatOp.More);
            SetField(entry, "_value", value);

            return entry;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private static int CountTrue(List<bool> values)
        {
            int count = 0;

            for (int index = 0; index < values.Count; index++)
            {
                if (values[index])
                    count++;
            }

            return count;
        }

        private sealed class FakeInput : IInputService
        {
            public bool UltimatePressed;

            public Vector2 MoveAxis => Vector2.zero;

            public bool ConsumeDashPressed()
            {
                return false;
            }

            public bool ConsumeUltimatePressed()
            {
                if (UltimatePressed == false)
                    return false;

                UltimatePressed = false;

                return true;
            }

            public void ResetLatches()
            {
                UltimatePressed = false;
            }
        }

        private sealed class FakeView : IView
        {
            public readonly List<bool> Berserk = new List<bool>();

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
                Berserk.Add(value);
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

using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class DashChargeTests
    {
        private const float TickSeconds = 1f / 60f;
        private const float BaseDistance = 4f;
        private const float BaseDuration = 0.2f;
        private const float TapSeconds = 0.18f;
        private const float MaxChargeSeconds = 1f;
        private const float DistanceAtFull = 1.8f;
        private const float DamageAtFull = 2f;
        private const float ChargeSlow = -0.4f;

        private EcsWorld _world;
        private EcsSystems _systems;
        private FakeInput _input;
        private SimulationClock _clockRef;
        private int _hero;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
        }

        [Test]
        public void DashKeyDashesOnThePressTick()
        {
            Build();

            _input.PressDash();
            Step();

            Assert.IsTrue(_world.GetPool<Dashing>().Has(_hero), "the plain dash must not wait for a release");
            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));

            DashEndedEvent ended = RunUntilDashEnds();

            Assert.AreEqual(0f, ended.Strength);
            Assert.AreEqual(1f, ended.DamageMultiplier, 1e-5f);
            Assert.AreEqual(BaseDistance, Vector3.Distance(ended.Start, ended.End), 0.05f);
        }

        [Test]
        public void DisabledChargeIgnoresTheChargeKey()
        {
            Build();

            _world.GetPool<DashStats>().Get(_hero).ChargeEnabled = false;

            _input.HoldCharge();
            Step(30);

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(0, SlowCount());

            _input.ReleaseCharge();
            Step();

            Assert.IsFalse(_world.GetPool<Dashing>().Has(_hero));
            Assert.IsFalse(_world.GetPool<DashRequest>().Has(_hero));
        }

        [Test]
        public void ShortChargeTapGivesAnUnchargedDash()
        {
            Build();

            _input.HoldCharge();
            Step();
            _input.ReleaseCharge();
            Step();

            DashEndedEvent ended = RunUntilDashEnds();

            Assert.AreEqual(0f, ended.Strength);
            Assert.AreEqual(BaseDistance, Vector3.Distance(ended.Start, ended.End), 0.05f);
        }

        [Test]
        public void DashKeyDuringAChargeReleasesIt()
        {
            Build();

            _input.HoldCharge();
            Step(Mathf.CeilToInt(MaxChargeSeconds / TickSeconds) + 5);
            _input.PressDash();
            Step();

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));

            DashEndedEvent ended = RunUntilDashEnds();

            Assert.AreEqual(1f, ended.Strength, 1e-5f);
        }

        [Test]
        public void ChargeHeldThroughTheCooldownStartsWhenItEnds()
        {
            Build();

            _world.GetPool<DashCooldown>().Add(_hero).Remaining = 10f;

            _input.HoldCharge();
            Step(3);

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(0, SlowCount());

            _world.GetPool<DashCooldown>().Del(_hero);
            Step();

            Assert.IsTrue(_world.GetPool<DashCharge>().Has(_hero));
        }

        [Test]
        public void FullHoldScalesDistanceAndDamage()
        {
            Build();

            _input.HoldCharge();
            Step(Mathf.CeilToInt(MaxChargeSeconds / TickSeconds) + 5);
            _input.ReleaseCharge();
            Step();

            DashEndedEvent ended = RunUntilDashEnds();

            Assert.AreEqual(1f, ended.Strength, 1e-5f);
            Assert.AreEqual(DamageAtFull, ended.DamageMultiplier, 1e-5f);
            Assert.AreEqual(BaseDistance * DistanceAtFull, Vector3.Distance(ended.Start, ended.End), 0.05f);
        }

        [Test]
        public void HalfHoldGivesAPartialCharge()
        {
            Build();

            _input.HoldCharge();
            Step(Mathf.RoundToInt((TapSeconds + (MaxChargeSeconds - TapSeconds) * 0.5f) / TickSeconds));
            _input.ReleaseCharge();
            Step();

            DashEndedEvent ended = RunUntilDashEnds();

            Assert.Greater(ended.Strength, 0.4f);
            Assert.Less(ended.Strength, 0.6f);
        }

        [Test]
        public void ChargingSlowsTheHeroUntilRelease()
        {
            Build();

            _input.HoldCharge();
            Step(10);

            Assert.IsTrue(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(ChargeSlow, SlowValue(), 1e-5f);

            _input.ReleaseCharge();
            Step();

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(0, SlowCount());
        }

        [Test]
        public void DashKeyDuringCooldownBuffersAPlainDash()
        {
            Build();

            _world.GetPool<DashCooldown>().Add(_hero).Remaining = 10f;

            _input.PressDash();
            Step();

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(0, SlowCount());
            Assert.IsTrue(_world.GetPool<DashRequest>().Has(_hero));
            Assert.AreEqual(0f, _world.GetPool<DashRequest>().Get(_hero).Strength);
        }

        [Test]
        public void DeathDropsTheChargeAndItsSlow()
        {
            Build();

            _input.HoldCharge();
            Step(5);

            _world.GetPool<Dead>().Add(_hero);
            Step();

            Assert.IsFalse(_world.GetPool<DashCharge>().Has(_hero));
            Assert.AreEqual(0, SlowCount());
        }

        private void Build()
        {
            _world = new EcsWorld();
            SimulationClock clock = new SimulationClock();
            StatModifiers modifiers = new StatModifiers(_world);
            _input = new FakeInput();
            _systems = new EcsSystems(_world);

            _systems.Add(new ReadDashInputSystem());
            _systems.Add(new AdvanceDashSystem());
            _systems.Add(new StartDashSystem());
            _systems.Add(new ApplyDashVelocitySystem());
            _systems.Add(new MoveSystem());
            _systems.Inject(clock, modifiers, (IInputService)_input, (IDashInput)_input);
            _systems.Init();

            _clockRef = clock;

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<Velocity>().Add(_hero);

            ref DashStats stats = ref _world.GetPool<DashStats>().Add(_hero);
            stats.Distance = BaseDistance;
            stats.Duration = BaseDuration;
            stats.Cooldown = 10f;
            stats.Direction = DashDirection.Forward;
            stats.AccelerationPower = 1f;
            stats.InvulnerabilitySeconds = BaseDuration;
            stats.ChargeEnabled = true;
            stats.TapSeconds = TapSeconds;
            stats.MaxChargeSeconds = MaxChargeSeconds;
            stats.DistanceAtFullCharge = DistanceAtFull;
            stats.SpeedAtFullCharge = 1.4f;
            stats.DamageAtFullCharge = DamageAtFull;
            stats.ChargeMoveSpeedMore = ChargeSlow;
        }

        private void Step(int ticks = 1)
        {
            for (int index = 0; index < ticks; index++)
            {
                _clockRef.Advance(TickSeconds);
                _world.GetPool<Velocity>().Get(_hero).Value = Vector3.zero;
                _world.GetPool<DashEndedEvent>().Del(_hero);
                _systems.Run();
            }
        }

        private DashEndedEvent RunUntilDashEnds()
        {
            EcsPool<DashEndedEvent> ended = _world.GetPool<DashEndedEvent>();

            for (int tick = 0; tick < 240; tick++)
            {
                if (ended.Has(_hero))
                    return ended.Get(_hero);

                Step();

                if (ended.Has(_hero))
                    return ended.Get(_hero);
            }

            Assert.Fail("The dash never ended.");

            return default;
        }

        private int SlowCount()
        {
            int count = 0;
            EcsPool<StatModifier> pool = _world.GetPool<StatModifier>();

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                if (pool.Get(entity).SourceId == DashChargeRules.SlowSourceId)
                    count++;
            }

            return count;
        }

        private float SlowValue()
        {
            EcsPool<StatModifier> pool = _world.GetPool<StatModifier>();

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                ref StatModifier modifier = ref pool.Get(entity);

                if (modifier.SourceId == DashChargeRules.SlowSourceId)
                {
                    Assert.AreEqual(StatId.MoveSpeed, modifier.Stat);
                    Assert.AreEqual(StatOp.More, modifier.Op);

                    return modifier.Value;
                }
            }

            Assert.Fail("No charge slow modifier.");

            return 0f;
        }

        private sealed class FakeInput : IInputService, IDashInput
        {
            private bool _dashPressed;
            private bool _chargeReleased;
            private bool _chargeHeld;

            public Vector2 MoveAxis => Vector2.zero;

            public bool IsChargeHeld => _chargeHeld;

            public bool IsDashHeld => false;

            public void PressDash()
            {
                _dashPressed = true;
            }

            public void HoldCharge()
            {
                _chargeHeld = true;
            }

            public void ReleaseCharge()
            {
                _chargeReleased = true;
                _chargeHeld = false;
            }

            public bool ConsumeDashPressed()
            {
                bool value = _dashPressed;
                _dashPressed = false;

                return value;
            }

            public bool ConsumeChargeReleased()
            {
                bool value = _chargeReleased;
                _chargeReleased = false;

                return value;
            }

            public void ResetLatches()
            {
            }
        }
    }
}

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
    public sealed class BossKnightTests
    {
        private const float Tick = 1f / 60f;
        private const int Seed = 987654321;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<float> _abilityHits = new List<float>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private BossConfig _boss;
        private ChargeAbilityConfig _charge;
        private SpinAbilityConfig _spin;
        private HeavyStrikeAbilityConfig _heavyStrike;
        private EnemyConfig _enemy;
        private int _hero;
        private int _knight;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
            _abilityHits.Clear();
            CountAbilityEvents = 0;
            _heavyStrike = null;

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void TheChargeWalksWindupLockRushRecoverInWholeTicks()
        {
            BuildConfigs();
            SetField(_charge, "_homingTurnSpeedDegrees", 0f);
            BuildWorld(heroAt: new Vector3(0f, 0f, 17f), seed: Seed);

            Run(1);
            Assert.AreEqual(EnemyActionPhase.ChargeWindup, Phase(), "10-18 m away the charge is the only ready ability");

            AssertPhaseFrom(tick: 61, EnemyActionPhase.ChargeLock, previousFor: 60);
            AssertPhaseFrom(tick: 82, EnemyActionPhase.ChargeRush, previousFor: 21);
            Assert.IsTrue(_world.GetPool<Rushing>().Has(_knight));
            AssertPhaseFrom(tick: 112, EnemyActionPhase.ChargeRecover, previousFor: 30);
            Assert.IsFalse(_world.GetPool<Rushing>().Has(_knight));
            AssertPhaseFrom(tick: 172, EnemyActionPhase.None, previousFor: 60);

            Assert.AreEqual(10f, Position(_knight).z, 1e-3f, "30 rush ticks at 20 m/s cover TelegraphLength = 10 m");
            Assert.AreEqual(_charge.TelegraphLength, Position(_knight).z, 1e-3f);
        }

        [Test]
        public void TheGlobalCooldownCountsFromTheEndAndTheAbilityCooldownFromTheStart()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 17f), seed: Seed);

            Run(172);
            Assert.AreEqual(EnemyActionPhase.None, Phase());
            Assert.AreEqual(179, _world.GetPool<EnemyAction>().Get(_knight).GlobalCooldownTicks, "the 3 s global cooldown starts with the action's end");

            for (int tick = 173; tick < 481; tick++)
            {
                Run(1);
                Assert.AreEqual(EnemyActionPhase.None, Phase(), $"tick {tick}: the 8 s charge cooldown runs from the charge start");
            }

            Run(1);
            Assert.AreEqual(EnemyActionPhase.ChargeWindup, Phase(), "tick 481 = 1 + 480");
        }

        [Test]
        public void TheWindupTurnIsLimitedAndTheDirectionLocksAtItsEnd()
        {
            BuildConfigs();
            SetField(_charge, "_homingTurnSpeedDegrees", 0f);
            SetField(_charge, "_windupTurnSpeedDegrees", 30f);
            BuildWorld(heroAt: new Vector3(10f, 0f, 0f), seed: Seed);

            Run(61);

            Assert.AreEqual(EnemyActionPhase.ChargeLock, Phase());

            Vector3 locked = _world.GetPool<EnemyCharge>().Get(_knight).Direction;

            Assert.AreEqual(30f, Vector3.Angle(Vector3.forward, locked), 0.6f, "60 windup ticks at 30 deg/s");

            Position(_hero) = new Vector3(-10f, 0f, 0f);
            Run(21 + 30);

            Vector3 travelled = Position(_knight);
            Assert.AreEqual(30f, Vector3.Angle(Vector3.forward, travelled), 0.6f, "the hero moving after the lock does not bend the rush");
        }

        [Test]
        public void TheRushHitsOnceThroughTheHero()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 8f), seed: Seed);

            Run(172);

            Assert.AreEqual(1, _abilityHits.Count, "one hit per rush");
            Assert.AreEqual(60f, _abilityHits[0]);
            Assert.AreEqual(1000f - 60f, _world.GetPool<Health>().Get(_hero).Current);
            Assert.Greater(Position(_knight).z, Position(_hero).z, "the rush carries the boss through and past the hero");
        }

        [Test]
        public void AnInvulnerableHeroSpendsTheHitWithoutDamage()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 8f), seed: Seed);

            _world.GetPool<Invulnerable>().Add(_hero).RemainingTicks = 100000;

            Run(172);

            Assert.AreEqual(1, CountAbilityEvents, "the event is created and dropped by ApplyDamageSystem, like a bolt");
            Assert.AreEqual(0, _abilityHits.Count);
            Assert.AreEqual(1000f, _world.GetPool<Health>().Get(_hero).Current);
        }

        [Test]
        public void AFastRushDoesNotTunnelThroughTheHero()
        {
            BuildConfigs();
            SetField(_charge, "_rushSpeed", 600f);
            SetField(_charge, "_rushSeconds", 0.05f);
            BuildWorld(heroAt: new Vector3(0f, 0f, 15f), seed: Seed, heroRadius: 0.1f);

            Run(81 + 3 + 1);

            Assert.AreEqual(1, _abilityHits.Count, "10 m per tick; samples land at 10 m and 20 m, the hero stands at 15 m");
        }

        [Test]
        public void ARushBesideTheHeroMisses()
        {
            BuildConfigs();
            SetField(_charge, "_rushSpeed", 600f);
            SetField(_charge, "_rushSeconds", 0.05f);
            BuildWorld(heroAt: new Vector3(0f, 0f, 15f), seed: Seed, heroRadius: 0.1f);

            Run(81);
            Position(_hero) = new Vector3(1.2f, 0f, 15f);
            Run(4);

            Assert.AreEqual(0, CountAbilityEvents, "1.2 m to the side is beyond 0.7 + 0.3 + 0.1");
        }

        [Test]
        public void TheSpinPulsesEveryHalfSecondWithDamageFallingWithDistance()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 2f), seed: Seed);

            Run(1);
            Assert.AreEqual(EnemyActionPhase.SpinRaise, Phase(), "2 m away the spin is the only ready ability");

            Run(48);
            Assert.AreEqual(EnemyActionPhase.Spinning, Phase());
            Assert.AreEqual(1, _abilityHits.Count, "the first pulse lands on the first spinning tick");
            Assert.AreEqual(Mathf.Lerp(15f, 4f, 0.5f), _abilityHits[0], 1e-4f);

            Run(239);
            Assert.AreEqual(EnemyActionPhase.Spinning, Phase());

            Run(1);
            Assert.AreEqual(EnemyActionPhase.SpinRecover, Phase());
            Assert.AreEqual(8, _abilityHits.Count, "4 s / 0.5 s");
        }

        [Test]
        public void TheSpinEdgeIsTheBodyRimAndClampsToTheEdgeDamage()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 4.3f), seed: Seed);

            Run(49);
            Assert.AreEqual(1, _abilityHits.Count, "4.3 m is inside radius 4 plus the hero's 0.4");
            Assert.AreEqual(4f, _abilityHits[0], 1e-4f);

            Position(_hero) = new Vector3(0f, 0f, 4.5f);
            Run(30);
            Assert.AreEqual(1, _abilityHits.Count, "4.5 m is outside");

            Assert.AreEqual(15f, _spin.DamageAt(0f), 1e-5f);
        }

        [Test]
        public void TheSpinSlowsTheBossThroughAModifierAndRemovesIt()
        {
            BuildConfigs();
            BuildWorld(heroAt: new Vector3(0f, 0f, 2f), seed: Seed);

            Run(49);
            Assert.AreEqual(1, CountModifiers(_spin.Id), "MoveSpeed More (0.6 - 1) while spinning");

            Run(240);
            Assert.AreEqual(EnemyActionPhase.SpinRecover, Phase());
            Assert.AreEqual(0, CountModifiers(_spin.Id));
        }

        [Test]
        public void TheHeavyStrikeWalksWindupLockRecoverAndHitsOnceAtTheImpact()
        {
            BuildConfigs();
            AddHeavyStrike();
            BuildWorld(heroAt: new Vector3(0f, 0f, 3f), seed: Seed);

            Run(1);
            Assert.AreEqual(EnemyActionPhase.StrikeWindup, Phase());

            AssertPhaseFrom(tick: 109, EnemyActionPhase.StrikeLock, previousFor: 108);
            Assert.AreEqual(0, _abilityHits.Count, "nothing lands while the marker grows");

            AssertPhaseFrom(tick: 139, EnemyActionPhase.StrikeRecover, previousFor: 30);
            Assert.AreEqual(1, _abilityHits.Count, "the impact lands on the tick the lock ends");
            Assert.AreEqual(45f, _abilityHits[0]);

            AssertPhaseFrom(tick: 259, EnemyActionPhase.None, previousFor: 120);
            Assert.AreEqual(1, _abilityHits.Count, "one impact per strike");
        }

        [Test]
        public void TheWindupTurnsToTheHeroAndTheImpactLandsWhereItLocked()
        {
            BuildConfigs();
            AddHeavyStrike();
            BuildWorld(heroAt: new Vector3(3f, 0f, 0f), seed: Seed);

            Run(109);
            Assert.AreEqual(EnemyActionPhase.StrikeLock, Phase());

            Vector3 locked = _world.GetPool<EnemyHeavyStrike>().Get(_knight).Direction;
            Assert.AreEqual(0f, Vector3.Angle(Vector3.right, locked), 0.5f, "90 deg/s for 1.8 s turns the boss fully onto the hero at 90 deg");

            Position(_hero) = new Vector3(0f, 0f, 3f);
            Run(30);

            Assert.AreEqual(EnemyActionPhase.StrikeRecover, Phase());
            Assert.AreEqual(0, CountAbilityEvents, "stepping out of the locked marker dodges the impact");
        }

        [TestCase(3f, 90f)]
        [TestCase(-3f, -90f)]
        public void TheWindupReportsItsSignedTurnRateAndZeroOnceItFacesTheHero(float heroX, float expectedRate)
        {
            BuildConfigs();
            AddHeavyStrike();
            BuildWorld(heroAt: new Vector3(heroX, 0f, 0f), seed: Seed);

            Run(1);
            Assert.AreEqual(expectedRate, _world.GetPool<EnemyHeavyStrike>().Get(_knight).TurnDegreesPerSecond, 0.5f, "positive turns right, negative turns left");

            Run(70);
            Assert.AreEqual(EnemyActionPhase.StrikeWindup, Phase());
            Assert.AreEqual(0f, _world.GetPool<EnemyHeavyStrike>().Get(_knight).TurnDegreesPerSecond, 1e-3f, "90 deg take 60 ticks; after that the boss faces the hero and stands");

            Run(40);
            Assert.AreEqual(EnemyActionPhase.StrikeLock, Phase());
            Assert.AreEqual(0f, _world.GetPool<EnemyHeavyStrike>().Get(_knight).TurnDegreesPerSecond, "no turning while locked");
        }

        [TestCase(5.3f, 1)]
        [TestCase(5.5f, 0)]
        public void TheImpactReachesTheRadiusPlusTheHeroRim(float heroZ, int expectedHits)
        {
            BuildConfigs();
            AddHeavyStrike();
            BuildWorld(heroAt: new Vector3(0f, 0f, 3f), seed: Seed);

            Run(109);
            Position(_hero) = new Vector3(0f, 0f, heroZ);
            Run(30);

            Assert.AreEqual(expectedHits, CountAbilityEvents, "impact at 3 m, radius 2 plus the hero's 0.4: 5.3 m is in, 5.5 m is out");
        }

        [Test]
        public void AZeroWeightHeavyStrikeIsNeverChosen()
        {
            for (int seed = 1; seed <= 16; seed++)
            {
                TearDown();
                BuildConfigs();
                AddHeavyStrike();
                SetField(_boss, "_spinWeight", 1f);
                SetField(_boss, "_heavyStrikeWeight", 0f);
                BuildWorld(heroAt: new Vector3(0f, 0f, 3f), seed: seed);

                Run(1);

                Assert.AreEqual(EnemyActionPhase.SpinRaise, Phase(), $"seed {seed}");
            }
        }

        [Test]
        public void TheChoiceFollowsTheWeightsAndRepeatsForTheSameSeed()
        {
            int charges = 0;
            int spins = 0;

            for (int seed = 1; seed <= 64; seed++)
            {
                EnemyActionPhase first = FirstPick(seed, chargeWeight: 1f, spinWeight: 1f);
                EnemyActionPhase again = FirstPick(seed, chargeWeight: 1f, spinWeight: 1f);

                Assert.AreEqual(first, again, $"seed {seed} must pick the same ability twice");

                if (first == EnemyActionPhase.ChargeWindup)
                    charges++;
                else
                    spins++;
            }

            Assert.Greater(charges, 16, "both abilities show up at weights 1 : 1");
            Assert.Greater(spins, 16);

            for (int seed = 1; seed <= 16; seed++)
                Assert.AreEqual(EnemyActionPhase.SpinRaise, FirstPick(seed, chargeWeight: 0f, spinWeight: 1f));
        }

        [Test]
        public void TheBossStreamDoesNotTouchTheSpawnStream()
        {
            uint spawn = SimulationRandom.SeedState(Seed);
            uint boss = SimulationRandom.StreamState(Seed, BossDecideSystem.RandomStreamId);

            Assert.AreNotEqual(spawn, boss);
        }

        [Test]
        public void BossDeathWinsTheRunAfterItsDeathSeconds()
        {
            RunOutcome outcome = RunBossDeath(heroDead: false, ticks: 269);
            Assert.IsFalse(outcome.IsFinished, "4.5 s = 270 ticks");

            outcome = RunBossDeath(heroDead: false, ticks: 270);
            Assert.IsTrue(outcome.IsFinished);
            Assert.AreEqual(RunEndReason.Victory, outcome.Reason);
        }

        [Test]
        public void ADeadHeroIsNotDeclaredTheWinner()
        {
            RunOutcome outcome = RunBossDeath(heroDead: true, ticks: 400);

            Assert.IsFalse(outcome.IsFinished, "the hero's own death path ends the run");
        }

        [Test]
        public void TheFirstFinishWins()
        {
            RunOutcome outcome = new RunOutcome();

            outcome.Finish(RunEndReason.Death);
            outcome.Finish(RunEndReason.Victory);

            Assert.AreEqual(RunEndReason.Death, outcome.Reason);
        }

        [Test]
        public void MotionBoundsTakeTheRushIntoTheCandidateSlack()
        {
            BuildConfigs();
            SetField(_charge, "_rushSpeed", 600f);

            CharacterConfig character = Create<CharacterConfig>();
            SetField(character, "_bodyRadius", 0.4f);

            EnemyMotionBounds bounds = EnemyMotionBounds.From(character, TimelineWith(_enemy), Array.Empty<PerkConfig>());

            Assert.AreEqual(600f, bounds.MaxEnemyRushSpeed);
            Assert.AreEqual(10f, bounds.CandidateSlack(Tick), 1e-4f);
        }

        [Test]
        public void TheIndexDoesNotLoseARushingBossWithTheRushSlack()
        {
            _world = new EcsWorld();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            int boss = _world.NewEntity();
            _world.GetPool<Position>().Add(boss).Value = Vector3.zero;

            grid.Clear();
            grid.Add(boss, Vector3.zero);

            _world.GetPool<Position>().Get(boss).Value = new Vector3(0f, 0f, 10f);

            List<int> found = new List<int>();
            EnemyMotionBounds walking = new EnemyMotionBounds(3f, 1.1f, 5f, 0.7f);
            EnemyMotionBounds rushing = new EnemyMotionBounds(3f, 1.1f, 5f, 0.7f, 600f);

            grid.Query(new Vector3(0f, 0f, 10f), 1.1f, walking.CandidateSlack(Tick), found);
            Assert.AreEqual(0, found.Count, "without the rush the slack is 1.15 m and the boss, 10 m away from its cell, is lost");

            grid.Query(new Vector3(0f, 0f, 10f), 1.1f, rushing.CandidateSlack(Tick), found);
            CollectionAssert.Contains(found, boss);
        }

        [Test]
        public void ASpinFasterThanBaseRaisesTheTimelineSpeedCeiling()
        {
            BuildConfigs();
            SetField(_spin, "_moveSpeedMultiplier", 1.5f);

            Assert.AreEqual(3.75f, WaveTimelineValidator.ResolveMaxMoveSpeed(TimelineWith(_enemy)), 1e-5f);
            Assert.DoesNotThrow(() => RecomputeEnemySpeed(maxEnemyMoveSpeed: 3.75f, more: 0.5f));
            Assert.Throws<InvalidOperationException>(() => RecomputeEnemySpeed(maxEnemyMoveSpeed: 2.5f, more: 0.5f));

            SetField(_spin, "_moveSpeedMultiplier", 0.6f);
            Assert.AreEqual(2.5f, WaveTimelineValidator.ResolveMaxMoveSpeed(TimelineWith(_enemy)), 1e-5f, "a slowing spin keeps the base");
        }

        [Test]
        public void ASpawnTooCloseToTheHeroIsMirroredInwards()
        {
            RingSpawnPlacement placement = new RingSpawnPlacement();
            Vector3 hero = new Vector3(44f, 0f, 0f);

            Vector3 clamped = placement.Resolve(hero, 30f, 45f, 0f, 0f);
            Assert.AreEqual(1f, Vector3.Distance(hero, clamped), 1e-3f, "the known edge case: the ring is pressed to the arena 1 m from the hero");

            Vector3 boss = placement.Resolve(hero, 30f, 45f, 0f, 12f);
            Assert.GreaterOrEqual(Vector3.Distance(hero, boss), 12f);
            Assert.AreEqual(14f, boss.x, 1e-3f);

            Assert.AreEqual(placement.Resolve(hero, 30f, 45f, 0.3f), placement.Resolve(hero, 30f, 45f, 0.3f, 0f), "zero distance changes nothing");
        }

        private int CountAbilityEvents { get; set; }

        private EnemyActionPhase FirstPick(int seed, float chargeWeight, float spinWeight)
        {
            TearDown();
            BuildConfigs();
            SetField(_spin, "_triggerRange", 10f);
            SetField(_boss, "_chargeWeight", chargeWeight);
            SetField(_boss, "_spinWeight", spinWeight);
            BuildWorld(heroAt: new Vector3(0f, 0f, 8f), seed: seed);

            Run(1);

            return Phase();
        }

        private RunOutcome RunBossDeath(bool heroDead, int ticks)
        {
            TearDown();
            BuildConfigs();

            _world = new EcsWorld();
            _clock = new SimulationClock();
            RunOutcome outcome = new RunOutcome();

            int hero = _world.NewEntity();
            _world.GetPool<Player>().Add(hero);

            if (heroDead)
                _world.GetPool<Dead>().Add(hero);

            int boss = _world.NewEntity();
            _world.GetPool<Boss>().Add(boss).Config = _boss;
            _world.GetPool<DiedEvent>().Add(boss);

            _systems = new EcsSystems(_world);
            _systems.Add(new FinishRunOnBossDeathSystem());
            _systems.Inject(outcome, _clock, LevelWithFeedback());
            _systems.Init();

            Run(1);
            _world.DelEntity(boss);
            Run(ticks);

            return outcome;
        }

        private void RecomputeEnemySpeed(float maxEnemyMoveSpeed, float more)
        {
            EcsWorld world = new EcsWorld();

            try
            {
                StatModifiers modifiers = new StatModifiers(world);

                int enemy = world.NewEntity();
                world.GetPool<Enemy>().Add(enemy);
                ref MoveSpeed speed = ref world.GetPool<MoveSpeed>().Add(enemy);
                speed.Base = 2.5f;
                speed.Value = 2.5f;

                modifiers.Add(enemy, StatId.MoveSpeed, StatOp.More, more, "test.spin");

                EcsSystems systems = new EcsSystems(world);
                systems.Add(new RecomputeStatsSystem());
                systems.Inject(Create<LevelConfig>(), new EnemyMotionBounds(maxEnemyMoveSpeed, 1.1f, 5f, 0.7f));
                systems.Init();

                try
                {
                    systems.Run();
                }
                finally
                {
                    systems.Destroy();
                }
            }
            finally
            {
                world.Destroy();
            }
        }

        private void AssertPhaseFrom(int tick, EnemyActionPhase phase, int previousFor)
        {
            EnemyActionPhase before = Phase();

            Run(previousFor - 1);
            Assert.AreEqual(before, Phase(), $"tick {tick - 1} is still {before}");

            Run(1);
            Assert.AreEqual(phase, Phase(), $"tick {tick}");
        }

        private EnemyActionPhase Phase()
        {
            return _world.GetPool<EnemyAction>().Get(_knight).Phase;
        }

        private ref Vector3 Position(int entity)
        {
            return ref _world.GetPool<Position>().Get(entity).Value;
        }

        private int CountModifiers(string source)
        {
            int count = 0;

            foreach (int entity in _world.Filter<StatModifier>().End())
            {
                if (_world.GetPool<StatModifier>().Get(entity).SourceId == source)
                    count++;
            }

            return count;
        }

        private void Run(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private void BuildConfigs()
        {
            _boss = Create<BossConfig>();
            SetField(_boss, "_id", "boss.test");

            _charge = Create<ChargeAbilityConfig>();
            SetField(_charge, "_id", "ability.charge.test");

            _spin = Create<SpinAbilityConfig>();
            SetField(_spin, "_id", "ability.spin.test");

            _enemy = Create<EnemyConfig>();
            SetField(_enemy, "_id", "enemy.boss.test");
            SetField(_enemy, "_moveSpeed", 2.5f);
            SetField(_enemy, "_bodyRadius", 0.7f);
            SetField(_enemy, "_boss", _boss);
            SetField(_enemy, "_charge", _charge);
            SetField(_enemy, "_spin", _spin);
        }

        private void AddHeavyStrike()
        {
            _heavyStrike = Create<HeavyStrikeAbilityConfig>();
            SetField(_heavyStrike, "_id", "ability.heavy_strike.test");
            SetField(_enemy, "_heavyStrike", _heavyStrike);
            SetField(_boss, "_chargeWeight", 0f);
            SetField(_boss, "_spinWeight", 0f);
            SetField(_boss, "_heavyStrikeWeight", 1f);
        }

        private void BuildWorld(Vector3 heroAt, int seed, float heroRadius = 0.4f)
        {
            _world = new EcsWorld();
            _clock = new SimulationClock();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = heroAt;
            _world.GetPool<PreviousPosition>().Add(_hero).Value = heroAt;
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<BodyRadius>().Add(_hero).Value = heroRadius;
            _world.GetPool<Health>().Add(_hero).Current = 1000f;

            _knight = _world.NewEntity();
            _world.GetPool<Enemy>().Add(_knight);
            _world.GetPool<Boss>().Add(_knight).Config = _boss;
            _world.GetPool<NotPushable>().Add(_knight);
            _world.GetPool<EnemyAction>().Add(_knight);
            _world.GetPool<EnemyCharge>().Add(_knight).Config = _charge;
            _world.GetPool<EnemySpin>().Add(_knight).Config = _spin;

            if (_heavyStrike != null)
                _world.GetPool<EnemyHeavyStrike>().Add(_knight).Config = _heavyStrike;

            _world.GetPool<Position>().Add(_knight).Value = Vector3.zero;
            _world.GetPool<PreviousPosition>().Add(_knight).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_knight).Value = Vector3.forward;
            _world.GetPool<BodyRadius>().Add(_knight).Value = 0.7f;
            _world.GetPool<MoveIntent>().Add(_knight);
            _world.GetPool<Velocity>().Add(_knight);
            ref MoveSpeed speed = ref _world.GetPool<MoveSpeed>().Add(_knight);
            speed.Base = 0f;
            speed.Value = 0f;
            _world.GetPool<ChaseTarget>().Add(_knight).Value = _world.PackEntity(_hero);
            _world.GetPool<Health>().Add(_knight).Current = 1500f;

            _systems = new EcsSystems(_world);
            _systems.Add(new ChaseTargetSystem());
            _systems.Add(new AdvanceEnemyChargeSystem());
            _systems.Add(new AdvanceEnemySpinSystem());
            _systems.Add(new AdvanceEnemyHeavyStrikeSystem());
            _systems.Add(new BossDecideSystem());
            _systems.Add(new HoldForEnemyActionSystem());
            _systems.Add(new StorePreviousPositionSystem());
            _systems.Add(new ApplyEnemyChargeVelocitySystem());
            _systems.Add(new ApplyMoveSpeedSystem());
            _systems.Add(new FaceEnemyChargeTargetSystem());
            _systems.Add(new FaceEnemyHeavyStrikeTargetSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new PulseEnemySpinSystem());
            _systems.Add(new ImpactEnemyHeavyStrikeSystem());
            _systems.Add(new SweepEnemyChargeSystem());
            _systems.Add(new TickInvulnerabilitySystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new RecordAbilityHitsSystem(this));
            _systems.Add(new CleanupEventsSystem());
            _systems.Inject(
                new ContentRegistry(new IContentEntry[] { _enemy, _boss, _charge, _spin }),
                new RunContext("level.test", "hero.test", RunMode.Story, seed, Array.Empty<StatModifierSpec>()),
                _clock,
                new StatModifiers(_world),
                Create<LevelConfig>());
            _systems.Init();
        }

        private LevelConfig LevelWithFeedback()
        {
            FeedbackConfig feedback = Create<FeedbackConfig>();
            LevelConfig level = Create<LevelConfig>();
            SetField(level, "_feedback", feedback);

            return level;
        }

        private WaveTimelineConfig TimelineWith(EnemyConfig enemy)
        {
            GuaranteedSpawn spawn = new GuaranteedSpawn();
            SetField(spawn, "_enemy", enemy);

            Wave wave = new Wave();
            SetField(wave, "_guaranteed", new[] { spawn });

            WaveTimelineConfig timeline = Create<WaveTimelineConfig>();
            SetField(timeline, "_waves", new[] { wave });

            return timeline;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class RecordAbilityHitsSystem : IEcsRunSystem
        {
            private readonly BossKnightTests _owner;

            private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;

            private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
            private readonly EcsPoolInject<DamageApplied> _applied = default;

            public RecordAbilityHitsSystem(BossKnightTests owner)
            {
                _owner = owner;
            }

            public void Run(IEcsSystems systems)
            {
                foreach (int entity in _events.Value)
                {
                    if (_damageEvents.Value.Get(entity).Kind != DamageKind.EnemyAbility)
                        continue;

                    _owner.CountAbilityEvents++;

                    if (_applied.Value.Has(entity))
                        _owner._abilityHits.Add(_applied.Value.Get(entity).Amount);
                }
            }
        }
    }
}

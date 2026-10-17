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
    public sealed class SlicingDashTests
    {
        private const float Tick = 1f / 60f;
        private const string PerkId = "perk.slicing-dash.test";
        private const string ConfigPath = "Assets/_Project/Configs/Perks/SlicingDash_Berserk.asset";
        private const float SwordDamage = 20f;
        private const float EnemyRadius = 0.4f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private SlicingDashConfig _config;
        private int _hero;

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
        public void CorridorCutsTheSwordSideOnceAndPushesSideways()
        {
            Build(chance: 1f);
            TakePerk();

            int swordSide = SpawnEnemy(new Vector3(0.9f, 0f, 3f));
            int offSide = SpawnEnemy(new Vector3(-1.5f, 0f, 3f));

            Dash(0f);
            StepUntilDashEnds();

            Assert.AreEqual(new[] { SwordDamage * 0.5f }, Damage(swordSide).ToArray());
            Assert.AreEqual(0, Damage(offSide).Count);

            ref Pushed pushed = ref _world.GetPool<Pushed>().Get(swordSide);

            Assert.Greater(pushed.Velocity.x, 4.99f);
            Assert.AreEqual(0f, pushed.Velocity.z, 1e-4f);
        }

        [Test]
        public void FinalSlashLandsOnTheImpactTickAndHoldsTheHero()
        {
            Build(chance: 1f);
            TakePerk();

            Dash(0f);
            int endTick = StepUntilDashEnds();

            int target = SpawnEnemy(_world.GetPool<Position>().Get(_hero).Value + new Vector3(0f, 0f, 1.8f));
            int impactTicks = Mathf.RoundToInt(_config.FinalImpactClipSeconds / _config.FinalPlaybackSpeed / Tick);
            int finishTicks = Mathf.RoundToInt(_config.FinalClipSeconds / _config.FinalPlaybackSpeed / Tick);

            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero), "the final slash is a special attack");

            int landed = -1;

            for (int tick = 1; tick <= finishTicks + 2; tick++)
            {
                Step();

                if (landed < 0 && Damage(target).Count > 0)
                    landed = tick;
            }

            Assert.AreEqual(impactTicks, landed, $"dash ended on tick {endTick}");
            Assert.AreEqual(new[] { SwordDamage * 1.5f }, Damage(target).ToArray());
            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "the hold ends with the clip");
        }

        [Test]
        public void FullChargeDoublesBothStrikes()
        {
            Build(chance: 1f);
            TakePerk();

            int swordSide = SpawnEnemy(new Vector3(0.9f, 0f, 3f));

            Dash(1f);
            StepUntilDashEnds();

            int target = SpawnEnemy(_world.GetPool<Position>().Get(_hero).Value + new Vector3(0f, 0f, 1.8f));

            Step(80);

            Assert.AreEqual(new[] { SwordDamage * 0.5f * 2f }, Damage(swordSide).ToArray());
            Assert.AreEqual(new[] { SwordDamage * 1.5f * 2f }, Damage(target).ToArray());
        }

        [Test]
        public void WithoutThePerkTheDashNeitherCutsNorFinishes()
        {
            Build(chance: 1f);

            int swordSide = SpawnEnemy(new Vector3(0.9f, 0f, 3f));

            Dash(0f);
            StepUntilDashEnds();
            Step(80);

            Assert.AreEqual(0, Damage(swordSide).Count);
            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));
            Assert.AreEqual(0, _world.Filter<DashFinisher>().End().GetEntitiesCount());
        }

        [Test]
        public void AFailedRollSkipsTheCorridorAndTheFinalSlash()
        {
            Build(chance: 1e-6f);
            TakePerk();

            int swordSide = SpawnEnemy(new Vector3(0.9f, 0f, 3f));

            Dash(0f);
            StepUntilDashEnds();
            Step(80);

            Assert.AreEqual(0, Damage(swordSide).Count);
            Assert.AreEqual(0, _world.Filter<DashFinisher>().End().GetEntitiesCount());
        }

        [Test]
        public void ANewDashCancelsTheFinalSlashBeforeImpact()
        {
            Build(chance: 1f);
            TakePerk();

            Dash(0f);
            StepUntilDashEnds();

            int target = SpawnEnemy(_world.GetPool<Position>().Get(_hero).Value + new Vector3(0f, 0f, 1.8f));

            Step(5);

            _world.GetPool<DashCooldown>().Del(_hero);

            ref Dashing dashing = ref _world.GetPool<Dashing>().Add(_hero);
            dashing.Direction = Vector3.back;
            dashing.RemainingTicks = 3;
            dashing.TotalTicks = 3;

            Step(80);

            Assert.AreEqual(0, CountAmount(target, SwordDamage * 1.5f));
        }

        private void Build(float chance)
        {
            _config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SlicingDashConfig>(ConfigPath));
            Assert.IsNotNull(_config, $"{ConfigPath} is missing.");
            _assets.Add(_config);

            SlicingDashLevel level = _config.Level(1);
            SetField(level, "_chance", chance);
            SetField(level, "_corridorDamageShare", 0.5f);
            SetField(level, "_finalDamageShare", 1.5f);

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] levels = new PerkLevel[_config.LevelCount];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", PerkId);
            SetField(perk, "_levels", levels);
            SetField(perk, "_behaviour", _config);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(levelConfig, "_waves", timeline);
            _assets.Add(levelConfig);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);
            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, 987654321, Array.Empty<StatModifierSpec>());

            _systems = new EcsSystems(_world);
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Add(new AdvanceDashSystem());
            _systems.Add(new StartDashSystem());
            _systems.Add(new StorePreviousPositionSystem());
            _systems.Add(new ApplyDashVelocitySystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new ExpireSpecialAttackSystem());
            _systems.Add(new StartDashFinisherSystem());
            _systems.Add(new AdvanceDashFinisherSystem());
            _systems.Add(new ResolveAreaStrikeSystem());
            _systems.Add(new SweepSlicingDashSystem());
            _systems.Inject(new ContentRegistry(new IContentEntry[] { perk }), levelConfig, grid, _clock, context, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<PreviousPosition>().Add(_hero);
            _world.GetPool<Velocity>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<BodyRadius>().Add(_hero).Value = 0.4f;
            _world.GetPool<NotIndexed>().Add(_hero);

            ref DashStats stats = ref _world.GetPool<DashStats>().Add(_hero);
            stats.Distance = 8f;
            stats.Duration = 0.33f;
            stats.Cooldown = 1f;
            stats.Direction = DashDirection.Forward;
            stats.AccelerationPower = 1f;
            stats.InvulnerabilitySeconds = 0.33f;
            stats.DistanceAtFullCharge = 1.8f;
            stats.SpeedAtFullCharge = 1.4f;
            stats.DamageAtFullCharge = 2f;

            int weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(weapon);
            _world.GetPool<OwnerLink>().Add(weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(weapon);
            damage.Base = SwordDamage;
            damage.Value = SwordDamage;
        }

        private void TakePerk()
        {
            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = PerkId;
            taken.Level = 1;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = EnemyRadius;

            return enemy;
        }

        private void Dash(float strength)
        {
            _world.GetPool<DashRequest>().Add(_hero).Strength = strength;
        }

        private int StepUntilDashEnds()
        {
            for (int tick = 1; tick < 240; tick++)
            {
                Step();

                if (_world.GetPool<DashEndedEvent>().Has(_hero))
                    return tick;
            }

            Assert.Fail("The dash never ended.");

            return -1;
        }

        private void Step(int ticks = 1)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _world.GetPool<DashEndedEvent>().Del(_hero);
                _world.GetPool<Velocity>().Get(_hero).Value = Vector3.zero;
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private List<float> Damage(int target)
        {
            List<float> amounts = new List<float>();
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                ref DamageEvent damageEvent = ref pool.Get(entity);

                if (damageEvent.Target.Unpack(_world, out int unpacked) && unpacked == target)
                    amounts.Add(damageEvent.Amount);
            }

            return amounts;
        }

        private int CountAmount(int target, float amount)
        {
            int count = 0;

            foreach (float value in Damage(target))
            {
                if (Mathf.Abs(value - amount) < 1e-4f)
                    count++;
            }

            return count;
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

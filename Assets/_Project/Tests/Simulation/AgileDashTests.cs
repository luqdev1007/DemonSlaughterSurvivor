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
    public sealed class AgileDashTests
    {
        private const float Tick = 1f / 60f;
        private const string AgileId = "perk.agile-dash.test";
        private const string SlicingId = "perk.slicing-dash.test";
        private const string AgilePath = "Assets/_Project/Configs/Perks/AgileDash_Berserk.asset";
        private const string SlicingPath = "Assets/_Project/Configs/Perks/SlicingDash_Berserk.asset";
        private const float SwordDamage = 20f;
        private const float AgileShare = 2f;
        private const float SlicingFinalShare = 1.5f;
        private const float SlicingCorridorShare = 0.5f;
        private const float CooldownSeconds = 8f;

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private AgileDashConfig _agile;
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
        public void EnemyNearTheEndGivesTheDodgeWindowAndALateRoundStrike()
        {
            Build();
            Take(AgileId);

            int near = SpawnEnemy(new Vector3(1.5f, 0f, 8f));

            Dash();
            StepUntilDashEnds();

            int window = Mathf.RoundToInt(_agile.WindowSeconds / Tick);
            int impact = Mathf.RoundToInt(_agile.ImpactClipSeconds / _agile.PlaybackSpeed / Tick);

            Assert.IsTrue(_world.GetPool<SpecialAttack>().Has(_hero));
            Assert.IsTrue(_world.GetPool<Invulnerable>().Has(_hero));
            Assert.GreaterOrEqual(_world.GetPool<Invulnerable>().Get(_hero).RemainingTicks, window);
            Assert.AreEqual(window, _world.GetPool<ProjectilePassThrough>().Get(_hero).RemainingTicks);

            int landed = -1;

            for (int tick = 1; tick <= impact + 2; tick++)
            {
                Step();

                if (landed < 0 && Damage(near).Count > 0)
                    landed = tick;
            }

            Assert.AreEqual(impact, landed);
            Assert.AreEqual(new[] { SwordDamage * AgileShare }, Damage(near).ToArray());
            Assert.IsTrue(_world.GetPool<Pushed>().Has(near));
        }

        [Test]
        public void NoEnemyNearTheEndMeansNoDodge()
        {
            Build();
            Take(AgileId);

            SpawnEnemy(new Vector3(0f, 0f, 8f + _agile.TriggerRadius + 1f));

            Dash();
            StepUntilDashEnds();

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));
            Assert.IsFalse(_world.GetPool<ProjectilePassThrough>().Has(_hero));
        }

        [Test]
        public void CooldownBlocksTheNextDodge()
        {
            Build();
            Take(AgileId);

            SpawnEnemy(new Vector3(1.5f, 0f, 8f));

            Dash();
            StepUntilDashEnds();
            Step(Mathf.RoundToInt(_agile.ClipSeconds / _agile.PlaybackSpeed / Tick) + 5);

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));

            _world.GetPool<Position>().Get(_hero).Value = new Vector3(1.5f, 0f, 0f);
            _world.GetPool<DashCooldown>().Del(_hero);
            Dash();
            StepUntilDashEnds();

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "the second dash lands near the enemy inside the cooldown");
        }

        [Test]
        public void WithBothPerksTheDodgeReplacesTheFinalSlashButTheCorridorStays()
        {
            Build();
            Take(AgileId);
            Take(SlicingId);

            int corridor = SpawnEnemy(new Vector3(0.9f, 0f, 3f));
            int near = SpawnEnemy(new Vector3(-1.5f, 0f, 8.5f));

            Dash();
            StepUntilDashEnds();
            Step(Mathf.RoundToInt(_agile.ClipSeconds / _agile.PlaybackSpeed / Tick) + 5);

            Assert.AreEqual(new[] { SwordDamage * SlicingCorridorShare }, Damage(corridor).ToArray());
            Assert.AreEqual(new[] { SwordDamage * AgileShare }, Damage(near).ToArray());
            Assert.AreEqual(0, CountAmount(SwordDamage * SlicingFinalShare));
        }

        [Test]
        public void ProjectilesPassThroughTheHeroDuringTheWindow()
        {
            _world = new EcsWorld();
            EcsSystems systems = new EcsSystems(_world);

            try
            {
                systems.Add(new AdvanceProjectilesSystem());
                systems.Inject((IViewFactory)new NullViewFactory());
                systems.Init();

                int hero = _world.NewEntity();
                _world.GetPool<Player>().Add(hero);
                _world.GetPool<Position>().Add(hero);
                _world.GetPool<PreviousPosition>().Add(hero);
                _world.GetPool<BodyRadius>().Add(hero).Value = 0.4f;
                _world.GetPool<Health>().Add(hero).Current = 100f;
                _world.GetPool<ProjectilePassThrough>().Add(hero).RemainingTicks = 10;

                int bolt = _world.NewEntity();
                _world.GetPool<Position>().Add(bolt).Value = new Vector3(0f, 0f, 0.5f);

                ref Projectile projectile = ref _world.GetPool<Projectile>().Add(bolt);
                projectile.LastPosition = new Vector3(0f, 0f, -0.5f);
                projectile.Damage = 10f;
                projectile.Radius = 0.1f;
                projectile.RemainingTicks = 30;

                systems.Run();

                Assert.AreEqual(0, _world.Filter<DamageEvent>().End().GetEntitiesCount());
                Assert.IsTrue(_world.GetPool<Projectile>().Has(bolt), "the bolt keeps flying");

                _world.GetPool<ProjectilePassThrough>().Del(hero);
                _world.GetPool<Projectile>().Get(bolt).LastPosition = new Vector3(0f, 0f, -0.5f);

                systems.Run();

                Assert.AreEqual(1, _world.Filter<DamageEvent>().End().GetEntitiesCount(), "without the window the same bolt hits");
            }
            finally
            {
                systems.Destroy();
            }
        }

        private void Build()
        {
            _agile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AgileDashConfig>(AgilePath));
            Assert.IsNotNull(_agile, $"{AgilePath} is missing.");
            _assets.Add(_agile);

            SetField(_agile.Level(1), "_damageShare", AgileShare);
            SetField(_agile.Level(1), "_cooldownSeconds", CooldownSeconds);

            SlicingDashConfig slicing = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SlicingDashConfig>(SlicingPath));
            _assets.Add(slicing);

            SetField(slicing.Level(1), "_chance", 1f);
            SetField(slicing.Level(1), "_corridorDamageShare", SlicingCorridorShare);
            SetField(slicing.Level(1), "_finalDamageShare", SlicingFinalShare);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(levelConfig, "_waves", timeline);
            _assets.Add(levelConfig);

            _world = new EcsWorld();
            _clock = new SimulationClock();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);
            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, 987654321, Array.Empty<StatModifierSpec>());
            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { Perk(AgileId, _agile), Perk(SlicingId, slicing) });

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
            _systems.Add(new TickInvulnerabilitySystem());
            _systems.Add(new TickProjectilePassThroughSystem());
            _systems.Inject(registry, levelConfig, grid, _clock, context, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
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

        private PerkConfig Perk(string id, PerkBehaviourConfig behaviour)
        {
            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] levels = new PerkLevel[behaviour.LevelCount];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", id);
            SetField(perk, "_levels", levels);
            SetField(perk, "_behaviour", behaviour);

            return perk;
        }

        private void Take(string id)
        {
            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = id;
            taken.Level = 1;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;

            return enemy;
        }

        private void Dash()
        {
            _world.GetPool<DashRequest>().Add(_hero).Strength = 0f;
        }

        private void StepUntilDashEnds()
        {
            for (int tick = 1; tick < 240; tick++)
            {
                Step();

                if (_world.GetPool<DashEndedEvent>().Has(_hero))
                    return;
            }

            Assert.Fail("The dash never ended.");
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

        private int CountAmount(float amount)
        {
            int count = 0;
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                if (Mathf.Abs(pool.Get(entity).Amount - amount) < 1e-4f)
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

        private sealed class NullViewFactory : IViewFactory
        {
            public IView Create(GameObject prefab, Vector3 position)
            {
                return null;
            }

            public void Release(IView view)
            {
            }

            public void Retire(IView view, float seconds, DeathBlow blow)
            {
            }
        }
    }
}

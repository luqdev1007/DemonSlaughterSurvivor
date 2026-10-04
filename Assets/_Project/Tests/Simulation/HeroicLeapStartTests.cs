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
    public sealed class HeroicLeapStartTests
    {
        private const float Tick = 1f / 60f;
        private const string PerkId = "perk.heroic-leap.test";
        private const string LeapPath = "Assets/_Project/Configs/Perks/HeroicLeap_Berserk.asset";

        private readonly List<Object> _assets = new List<Object>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private HeroicLeapConfig _config;
        private int _hero;
        private int _starts;
        private readonly List<int> _startTicks = new List<int>();
        private int _tick;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
            _startTicks.Clear();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [TestCase(3, false)]
        [TestCase(4, true)]
        public void LeapsOnlyWhenEnoughEnemiesAreClose(int around, bool expected)
        {
            Build();
            TakePerk(1);
            Surround(around, 1.2f);
            SpawnEnemy(new Vector3(0f, 0f, 1.6f));

            Run(3);

            Assert.AreEqual(expected, _starts > 0, $"{around} enemies within 1.5 m");
        }

        [TestCase(1, 8f)]
        [TestCase(5, 4f)]
        public void CooldownFollowsTheLevelFromTheStart(int level, float seconds)
        {
            Build();
            TakePerk(level);
            Surround(5, 1.2f);

            Run(Mathf.RoundToInt(seconds / Tick) * 2 + 5);

            Assert.GreaterOrEqual(_startTicks.Count, 2);
            Assert.AreEqual(Mathf.RoundToInt(seconds / Tick), _startTicks[1] - _startTicks[0], $"{seconds} s between leaps at level {level}");
        }

        [Test]
        public void StartBuildsTheLeapFromTheLevel()
        {
            Build();
            TakePerk(3);
            Surround(4, 1.2f);

            Run(1);

            EcsFilter leaps = _world.Filter<HeroicLeap>().End();

            Assert.AreEqual(1, leaps.GetEntitiesCount());

            foreach (int entity in leaps)
            {
                ref HeroicLeap leap = ref _world.GetPool<HeroicLeap>().Get(entity);

                Assert.AreSame(_config, leap.Config);
                Assert.AreEqual(_config.Level(3).DamageShare, leap.DamageScale);
                Assert.AreEqual(1, _starts, "the leap is announced once with LeapStarted");

                ref SpecialAttack special = ref _world.GetPool<SpecialAttack>().Get(_hero);

                Assert.IsTrue(special.Attack.Unpack(_world, out int attack) && attack == entity);
                Assert.IsTrue(special.LocksMovement);
            }
        }

        [Test]
        public void LandingComesTenTicksAfterTheStartTick()
        {
            Build();
            TakePerk(1);
            int ahead = SpawnEnemy(new Vector3(0f, 0f, 1.2f));
            Surround(3, 1.3f);

            Run(1);

            Assert.AreEqual(1, _starts);

            Run(_config.LandingTick - 1);

            Assert.AreEqual(0, CountEvents(ahead));

            Run(1);

            Assert.AreEqual(1, CountEvents(ahead), "the strike lands on start tick + 10");
        }

        [TestCase("busy")]
        [TestCase("dead")]
        [TestCase("dashing")]
        public void BusyHeroDoesNotLeapNorSpendTheCooldown(string state)
        {
            Build();
            TakePerk(1);
            Surround(5, 1.2f);

            if (state == "busy")
            {
                int other = _world.NewEntity();
                _world.GetPool<SpecialSwing>().Add(other);
                _world.GetPool<SpecialAttack>().Add(_hero).Attack = _world.PackEntity(other);
            }
            else if (state == "dead")
            {
                _world.GetPool<Dead>().Add(_hero);
            }
            else
            {
                _world.GetPool<Dashing>().Add(_hero);
            }

            Run(30);

            Assert.AreEqual(0, _starts);
            Assert.IsFalse(_world.GetPool<HeroicLeapCooldown>().Has(_hero), "a busy hero never reaches the cooldown");
        }

        [Test]
        public void WithoutThePerkNothingIsAddedToTheHero()
        {
            Build();
            Surround(6, 1.2f);

            Run(30);

            Assert.AreEqual(0, _starts);
            Assert.IsFalse(_world.GetPool<HeroicLeapCooldown>().Has(_hero));
        }

        private void Build()
        {
            _config = AssetDatabase.LoadAssetAtPath<HeroicLeapConfig>(LeapPath);
            Assert.IsNotNull(_config, $"{LeapPath} is missing.");

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

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { perk });

            _world = new EcsWorld();
            _clock = new SimulationClock();
            _starts = 0;
            _tick = 0;

            _systems = new EcsSystems(_world);
            _systems.Add(new StartHeroicLeapSystem());
            _systems.Add(new ExpireSpecialAttackSystem());
            _systems.Add(new AdvanceHeroicLeapSystem());
            _systems.Add(new RebuildSpatialGridSystem());
            _systems.Inject(level, new SpatialGrid(_world, 45f, 1f), _clock, registry, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            int weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(weapon);
            _world.GetPool<OwnerLink>().Add(weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(weapon);
            damage.Base = 20f;
            damage.Value = 20f;
        }

        private void TakePerk(int level)
        {
            int entity = _world.NewEntity();

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(entity);
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = PerkId;
            taken.Level = level;
        }

        private void Surround(int count, float distance)
        {
            for (int index = 0; index < count; index++)
            {
                float radians = (90f + index * 360f / count) * Mathf.Deg2Rad;

                SpawnEnemy(new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * distance);
            }

            Prime();
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 1000f;

            return enemy;
        }

        private void Prime()
        {
            new RebuildPrimer(_world).Run(_systems);
        }

        private void Run(int ticks)
        {
            EcsFilter started = _world.Filter<LeapStarted>().End();
            EcsPool<LeapStarted> pool = _world.GetPool<LeapStarted>();
            List<int> clear = new List<int>();

            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _tick++;
                _systems.Run();

                clear.Clear();

                foreach (int entity in started)
                {
                    _starts++;
                    _startTicks.Add(_tick);
                    clear.Add(entity);
                }

                foreach (int entity in clear)
                    pool.Del(entity);
            }
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

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class RebuildPrimer
        {
            private readonly EcsWorld _world;

            public RebuildPrimer(EcsWorld world)
            {
                _world = world;
            }

            public void Run(IEcsSystems systems)
            {
                foreach (IEcsSystem system in systems.GetAllSystems())
                {
                    if (system is RebuildSpatialGridSystem rebuild)
                        rebuild.Run(systems);
                }
            }
        }
    }
}

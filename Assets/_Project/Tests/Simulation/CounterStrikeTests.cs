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
    public sealed class CounterStrikeTests
    {
        private const float Tick = 1f / 60f;
        private const int Seed = 987654321;
        private const string PerkId = "perk.counter-strike.test";
        private const string CounterPath = "Assets/_Project/Configs/Perks/CounterStrike_Berserk.asset";
        private const float WeaponDamage = 20f;
        private const int SpinTicks = 42;

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<int> _hits = new List<int>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private SimulationClock _clock;
        private CounterStrikeConfig _counter;
        private int _hero;
        private int _weapon;

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world?.Destroy();
            _systems = null;
            _world = null;
            _hits.Clear();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [TestCase(1, 0.15f)]
        [TestCase(5, 0.35f)]
        public void ChanceOver10000HitsMatchesTheLevel(int level, float chance)
        {
            const int Draws = 10000;

            Build(triggerOnly: true);
            TakePerk(level);

            int strikes = 0;

            for (int draw = 0; draw < Draws; draw++)
            {
                HitHero();
                Step();

                if (_world.GetPool<SpecialAttack>().Has(_hero))
                {
                    strikes++;
                    DropSpin();
                }
            }

            float share = strikes / (float)Draws;
            float tolerance = 4f * Mathf.Sqrt(chance * (1f - chance) / Draws);

            Assert.AreEqual(chance, share, tolerance, $"{strikes} strikes in {Draws} hits at level {level}");
        }

        [Test]
        public void WithoutThePerkAHitDoesNothing()
        {
            Build(triggerOnly: true);

            HitHero();
            Step();

            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero));
            Assert.IsFalse(_world.GetPool<CounterStrikeRandom>().Has(_hero), "no perk, no stream");
        }

        [Test]
        public void WithoutAHitThereIsNoRoll()
        {
            Build(triggerOnly: true);
            TakePerk(5);

            Step();

            Assert.IsFalse(_world.GetPool<CounterStrikeRandom>().Has(_hero));
        }

        [Test]
        public void HitDuringASpecialAttackIsLostWithoutARoll()
        {
            Build(triggerOnly: true);
            TakePerk(5);

            int busy = _world.NewEntity();
            _world.GetPool<SpecialSwing>().Add(busy);
            _world.GetPool<SpecialAttack>().Add(_hero).Attack = _world.PackEntity(busy);

            HitHero();
            Step();

            Assert.IsFalse(_world.GetPool<CounterStrikeRandom>().Has(_hero), "the chance must be lost, not rolled");
            Assert.AreEqual(0, _world.Filter<Swing>().End().GetEntitiesCount());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DeadOrDashingHeroDoesNotStrike(bool dead)
        {
            Build(triggerOnly: true);
            TakePerk(5);

            if (dead)
                _world.GetPool<Dead>().Add(_hero);
            else
                _world.GetPool<Dashing>().Add(_hero);

            for (int tick = 0; tick < 50; tick++)
            {
                HitHero();
                Step();
            }

            Assert.AreEqual(0, _world.Filter<Swing>().End().GetEntitiesCount());
            Assert.IsFalse(_world.GetPool<CounterStrikeRandom>().Has(_hero));
        }

        [Test]
        public void StrikeBuildsASpecialSwingFromTheLevel()
        {
            Build(triggerOnly: true);
            TakePerk(3);

            int spin = StrikeOnce();
            ref Swing swing = ref _world.GetPool<Swing>().Get(spin);

            Assert.AreSame(_counter.Swing, swing.Variant);
            Assert.AreEqual(_counter.Level(3).DamageShare, swing.DamageScale);
            Assert.AreEqual(DamageKind.Perk, swing.Kind);
            Assert.AreEqual(SpinTicks, swing.RemainingTicks);
            Assert.IsTrue(_world.GetPool<SpecialSwing>().Has(spin));
            Assert.IsTrue(_world.GetPool<SwingStarted>().Has(spin));

            ref SpecialAttack special = ref _world.GetPool<SpecialAttack>().Get(_hero);

            Assert.IsTrue(special.Attack.Unpack(_world, out int attack) && attack == spin);
            Assert.AreEqual(_counter.LocksMovement, special.LocksMovement);
        }

        [Test]
        public void AttackSpeedDoesNotChangeTheSpin()
        {
            Build(triggerOnly: true);
            TakePerk(1);

            ref AttackSpeed attackSpeed = ref _world.GetPool<AttackSpeed>().Get(_weapon);
            attackSpeed.Value = 1.5f;

            int spin = StrikeOnce();

            Assert.AreEqual(SpinTicks, _world.GetPool<Swing>().Get(spin).RemainingTicks);
        }

        [Test]
        public void SpinHitsEveryEnemyAroundOnceWithAShareOfTheWeapon()
        {
            Build(triggerOnly: false);
            TakePerk(5);

            List<int> ring = new List<int>();

            for (int index = 0; index < 8; index++)
            {
                float radians = index * 45f * Mathf.Deg2Rad;

                ring.Add(SpawnEnemy(new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * 1.2f));
            }

            Step();
            StrikeOnce();

            for (int tick = 0; tick < SpinTicks + 2; tick++)
                Step();

            float expected = WeaponDamage * _counter.Level(5).DamageShare;

            foreach (int enemy in ring)
            {
                Assert.AreEqual(1, CountPerkEvents(enemy, out float amount), $"enemy {enemy} must be hit exactly once");
                Assert.AreEqual(expected, amount, 1e-4f);
            }

            Assert.AreEqual(0, _world.Filter<Swing>().End().GetEntitiesCount());
            Assert.IsFalse(_world.GetPool<SpecialAttack>().Has(_hero), "the marker goes with the Spin");
        }

        private int StrikeOnce()
        {
            for (int attempt = 0; attempt < 500; attempt++)
            {
                HitHero();
                Step();

                if (_world.GetPool<SpecialAttack>().Has(_hero) == false)
                    continue;

                _world.GetPool<SpecialAttack>().Get(_hero).Attack.Unpack(_world, out int spin);

                return spin;
            }

            Assert.Fail("no counter strike in 500 hits");

            return -1;
        }

        private void DropSpin()
        {
            _world.GetPool<SpecialAttack>().Get(_hero).Attack.Unpack(_world, out int spin);
            _world.DelEntity(spin);
            _world.GetPool<SpecialAttack>().Del(_hero);
        }

        private void HitHero()
        {
            int entity = _world.NewEntity();

            ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Add(entity);
            damageEvent.Target = _world.PackEntity(_hero);
            damageEvent.Amount = 10f;
            damageEvent.Kind = DamageKind.Contact;

            _world.GetPool<DamageApplied>().Add(entity).Amount = 10f;

            _hits.Add(entity);
        }

        private void Step()
        {
            _clock.Advance(Tick);
            _systems.Run();

            foreach (int entity in _hits)
                _world.DelEntity(entity);

            _hits.Clear();
        }

        private int CountPerkEvents(int target, out float amount)
        {
            int count = 0;
            amount = 0f;

            EcsPool<DamageEvent> events = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                ref DamageEvent damageEvent = ref events.Get(entity);

                if (damageEvent.Kind != DamageKind.Perk || damageEvent.Target.Unpack(_world, out int unpacked) == false || unpacked != target)
                    continue;

                count++;
                amount = damageEvent.Amount;
            }

            return count;
        }

        private void Build(bool triggerOnly)
        {
            _counter = AssetDatabase.LoadAssetAtPath<CounterStrikeConfig>(CounterPath);
            Assert.IsNotNull(_counter, $"{CounterPath} is missing.");

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);

            PerkLevel[] levels = new PerkLevel[_counter.LevelCount];

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", Array.Empty<StatModifierEntry>());
            }

            SetField(perk, "_id", PerkId);
            SetField(perk, "_levels", levels);
            SetField(perk, "_counterStrike", _counter);

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { perk });
            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, Seed, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _clock = new SimulationClock();
            SpatialGrid grid = new SpatialGrid(_world, 45f, 1f);

            _systems = new EcsSystems(_world);

            if (triggerOnly == false)
            {
                _systems.Add(new InterruptSwingSystem());
                _systems.Add(new ExpireSpecialAttackSystem());
                _systems.Add(new AdvanceSwingSystem());
            }

            _systems.Add(new TriggerCounterStrikeSystem());

            if (triggerOnly == false)
                _systems.Add(new RebuildSpatialGridSystem());

            _systems.Inject(level, grid, _clock, registry, context, new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f));
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;

            _weapon = _world.NewEntity();
            _world.GetPool<Weapon>().Add(_weapon);
            _world.GetPool<OwnerLink>().Add(_weapon).Owner = _world.PackEntity(_hero);

            ref WeaponDamage damage = ref _world.GetPool<WeaponDamage>().Add(_weapon);
            damage.Base = WeaponDamage;
            damage.Value = WeaponDamage;

            ref AttackSpeed attackSpeed = ref _world.GetPool<AttackSpeed>().Add(_weapon);
            attackSpeed.Base = 1f;
            attackSpeed.Value = 1f;
        }

        private void TakePerk(int level)
        {
            int entity = _world.NewEntity();

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(entity);
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = PerkId;
            taken.Level = level;
        }

        private int SpawnEnemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<BodyHeight>().Add(enemy).Value = 1.77f;
            _world.GetPool<Health>().Add(enemy).Current = 1000f;

            return enemy;
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

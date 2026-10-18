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
    public sealed class HitOutcomeTests
    {
        private const float TickSeconds = 1f / 60f;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;

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
        public void AttackPowerScalesPhysicalAndSpellPowerScalesMagic()
        {
            Build();

            int hero = SpawnHero(attackPower: 50f, spellPower: 100f);
            int enemy = SpawnEnemy(1000f);
            int physical = AddEvent(hero, enemy, 20f, false);
            int magic = AddEvent(hero, enemy, 20f, true);

            _systems.Run();

            Assert.AreEqual(30f, Applied(physical), 0.0001f);
            Assert.AreEqual(40f, Applied(magic), 0.0001f);
        }

        [Test]
        public void ACertainCritDoublesTheHitAndMarksIt()
        {
            Build();

            int hero = SpawnHero(crit: 1f);
            int enemy = SpawnEnemy(1000f);
            int hit = AddEvent(hero, enemy, 20f, true);

            _systems.Run();

            Assert.AreEqual(40f, Applied(hit), 0.0001f);
            Assert.IsTrue(_world.GetPool<DamageEvent>().Get(hit).IsCrit);
            Assert.IsTrue(_world.GetPool<DamageEvent>().Get(hit).IsMagic);
        }

        [Test]
        public void CritRateFollowsTheChance()
        {
            Build();

            int hero = SpawnHero(crit: 0.25f);
            int enemy = SpawnEnemy(1e9f);
            const int Hits = 4000;
            int crits = 0;

            for (int index = 0; index < Hits; index++)
            {
                int hit = AddEvent(hero, enemy, 10f, false);

                _systems.Run();

                if (_world.GetPool<DamageEvent>().Get(hit).IsCrit)
                    crits++;

                _world.DelEntity(hit);
            }

            Assert.AreEqual(0.25f, crits / (float)Hits, 0.03f);
        }

        [Test]
        public void ArmorCutsPhysicalAndMagicResistCutsMagic()
        {
            Build();

            int defender = SpawnHero(armor: 100f, magicResist: 300f);
            int attacker = SpawnEnemy(100f);
            int physical = AddEvent(attacker, defender, 40f, false);

            _systems.Run();

            Assert.AreEqual(20f, Applied(physical), 0.0001f);

            _world.DelEntity(physical);

            int magic = AddEvent(attacker, defender, 40f, true);

            _systems.Run();

            Assert.AreEqual(10f, Applied(magic), 0.0001f);
        }

        [Test]
        public void EvasionIsCappedAndEvadedHitsAreNotApplied()
        {
            Build();

            int defender = SpawnHero(evasion: 1f);
            int attacker = SpawnEnemy(100f);
            const int Hits = 3000;
            int evaded = 0;

            for (int index = 0; index < Hits; index++)
            {
                int hit = AddEvent(attacker, defender, 1f, false);

                _systems.Run();

                if (_world.GetPool<DamageEvaded>().Has(hit))
                {
                    evaded++;
                    Assert.IsFalse(_world.GetPool<DamageApplied>().Has(hit));
                }

                _world.GetPool<Health>().Get(defender).Current = 100f;
                _world.DelEntity(hit);
            }

            Assert.AreEqual(0.6f, evaded / (float)Hits, 0.03f);
        }

        [Test]
        public void TheKillingBlowCarriesItsAnchorForTheDamageNumber()
        {
            Build();

            int hero = SpawnHero();
            int enemy = SpawnEnemy(5f);
            _world.GetPool<Position>().Add(enemy).Value = new Vector3(3f, 0f, 4f);
            int hit = AddEvent(hero, enemy, 20f, false);

            _systems.Run();

            ref DamageApplied applied = ref _world.GetPool<DamageApplied>().Get(hit);

            Assert.IsTrue(applied.OnEnemy);
            Assert.AreEqual(enemy, applied.TargetKey);
            Assert.AreEqual(3f, applied.Anchor.x, 0.0001f);
            Assert.Greater(applied.Anchor.y, 0f);
        }

        private void Build()
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            _assets.Add(level);

            SimulationClock clock = new SimulationClock();
            clock.Advance(TickSeconds);

            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, 987654321, Array.Empty<StatModifierSpec>());

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);

            _systems.Add(new ResolveHitOutcomeSystem());
            _systems.Add(new ApplyDamageSystem());
            _systems.Inject(level, clock, context);
            _systems.Init();
        }

        private int SpawnHero(float crit = 0f, float attackPower = 0f, float spellPower = 0f, float armor = 0f, float magicResist = 0f, float evasion = 0f)
        {
            int hero = _world.NewEntity();
            _world.GetPool<Player>().Add(hero);
            _world.GetPool<Health>().Add(hero).Current = 100f;

            ref CombatStats stats = ref _world.GetPool<CombatStats>().Add(hero);
            stats.CritChance = crit;
            stats.CritMultiplier = 2f;
            stats.AttackPower = attackPower;
            stats.SpellPower = spellPower;
            stats.Armor = armor;
            stats.MagicResist = magicResist;
            stats.DefenseScale = 100f;
            stats.Evasion = evasion;
            stats.MaxEvasion = 0.6f;

            return hero;
        }

        private int SpawnEnemy(float health)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Health>().Add(enemy).Current = health;

            return enemy;
        }

        private int AddEvent(int source, int target, float amount, bool magic)
        {
            int entity = _world.NewEntity();

            ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Add(entity);
            damageEvent.Source = _world.PackEntity(source);
            damageEvent.Target = _world.PackEntity(target);
            damageEvent.Amount = amount;
            damageEvent.IsMagic = magic;
            damageEvent.Kind = DamageKind.Weapon;

            return entity;
        }

        private float Applied(int eventEntity)
        {
            Assert.IsTrue(_world.GetPool<DamageApplied>().Has(eventEntity), "the event was not applied");

            return _world.GetPool<DamageApplied>().Get(eventEntity).Amount;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            field.SetValue(target, value);
        }
    }
}

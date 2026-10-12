using Game.Configs;
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
    public sealed class LifeStealTests
    {
        private const float TickSeconds = 1f / 60f;
        private const float Tolerance = 1e-4f;
        private const float MaxHealth = 100f;
        private const float StartHealth = 50f;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();

        private EcsWorld _world;
        private EcsSystems _systems;
        private int _hero;

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
        public void KillHealsTheKillerByAFractionOfMaxHealth()
        {
            Build(onKill: 0.02f, onHit: 0f);

            AddEvent(_hero, SpawnEnemy(10f), 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(StartHealth + 2f, HeroHealth(), Tolerance, "0.02 x 100");
        }

        [Test]
        public void EveryKillOfTheTickCounts()
        {
            Build(onKill: 0.02f, onHit: 0f);

            AddEvent(_hero, SpawnEnemy(10f), 20f, DamageKind.Weapon);
            AddEvent(_hero, SpawnEnemy(10f), 20f, DamageKind.Perk);
            _systems.Run();

            Assert.AreEqual(StartHealth + 4f, HeroHealth(), Tolerance, "a kill by a perk attack is still the hero's kill");
        }

        [Test]
        public void OverkillOnOneEnemyIsOneKill()
        {
            Build(onKill: 0.02f, onHit: 0f);

            int enemy = SpawnEnemy(10f);
            AddEvent(_hero, enemy, 20f, DamageKind.Weapon);
            AddEvent(_hero, enemy, 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(StartHealth + 2f, HeroHealth(), Tolerance);
        }

        [Test]
        public void KillBySomeoneElseDoesNotHeal()
        {
            Build(onKill: 0.02f, onHit: 0f);

            int archer = SpawnEnemy(100f);
            AddEvent(archer, SpawnEnemy(10f), 20f, DamageKind.Projectile);
            _systems.Run();

            Assert.AreEqual(StartHealth, HeroHealth(), Tolerance);
        }

        [Test]
        public void HitsHealOnlyWhileLifeOnHitIsAboveZero()
        {
            Build(onKill: 0.02f, onHit: 0f);

            AddEvent(_hero, SpawnEnemy(100f), 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(StartHealth, HeroHealth(), Tolerance, "a hit that does not kill heals nothing outside the ultimate");

            ref LifeSteal lifeSteal = ref _world.GetPool<LifeSteal>().Get(_hero);
            lifeSteal.OnHit = 0.01f;

            AddEvent(_hero, SpawnEnemy(100f), 20f, DamageKind.Weapon);
            AddEvent(_hero, SpawnEnemy(100f), 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(StartHealth + 2f, HeroHealth(), Tolerance, "two weapon hits x 0.01 x 100");
        }

        [Test]
        public void KillingHitInTheUltimateHealsForTheHitAndTheKill()
        {
            Build(onKill: 0.02f, onHit: 0.01f);

            AddEvent(_hero, SpawnEnemy(10f), 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(StartHealth + 3f, HeroHealth(), Tolerance);
        }

        [Test]
        public void OnlyWeaponHitsOnEnemiesHeal()
        {
            Build(onKill: 0f, onHit: 0.01f);

            AddEvent(_hero, SpawnEnemy(100f), 20f, DamageKind.Perk);

            int breakable = SpawnEnemy(100f);
            _world.GetPool<Breakable>().Add(breakable);
            AddEvent(_hero, breakable, 20f, DamageKind.Weapon);

            _systems.Run();

            Assert.AreEqual(StartHealth, HeroHealth(), Tolerance);
        }

        [Test]
        public void DeadHeroIsNotHealed()
        {
            Build(onKill: 0.02f, onHit: 0.01f);

            _world.GetPool<Health>().Get(_hero).Current = 0f;

            AddEvent(_hero, SpawnEnemy(10f), 20f, DamageKind.Weapon);
            _systems.Run();

            Assert.AreEqual(0f, HeroHealth(), Tolerance);
            Assert.IsTrue(_world.GetPool<Dead>().Has(_hero));
        }

        private void Build(float onKill, float onHit)
        {
            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            _assets.Add(timeline);

            FeedbackConfig feedback = ScriptableObject.CreateInstance<FeedbackConfig>();
            _assets.Add(feedback);

            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            SetField(level, "_waves", timeline);
            SetField(level, "_feedback", feedback);
            _assets.Add(level);

            SimulationClock clock = new SimulationClock();
            clock.Advance(TickSeconds);

            _world = new EcsWorld();
            _systems = new EcsSystems(_world);

            _systems.Add(new ApplyDamageSystem());
            _systems.Add(new MarkDeadSystem());
            _systems.Add(new GrantLifeStealSystem());
            _systems.Add(new ApplyHealSystem());
            _systems.Add(new CleanupEventsSystem());
            _systems.Inject(level, clock);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Health>().Add(_hero).Current = StartHealth;

            ref MaxHealth maxHealth = ref _world.GetPool<MaxHealth>().Add(_hero);
            maxHealth.Base = MaxHealth;
            maxHealth.Value = MaxHealth;

            ref LifeSteal lifeSteal = ref _world.GetPool<LifeSteal>().Add(_hero);
            lifeSteal.OnKillBase = onKill;
            lifeSteal.OnKill = onKill;
            lifeSteal.OnHitBase = onHit;
            lifeSteal.OnHit = onHit;
        }

        private int SpawnEnemy(float health)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Health>().Add(enemy).Current = health;

            return enemy;
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

        private float HeroHealth()
        {
            return _world.GetPool<Health>().Get(_hero).Current;
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

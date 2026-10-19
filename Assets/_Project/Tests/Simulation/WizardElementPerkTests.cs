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
    public sealed class WizardElementPerkTests
    {
        private const float Tick = 1f / 60f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private GameObject _fx;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            _fx = new GameObject("Fx");
            _assets.Add(_fx);
            _world = new EcsWorld();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
        }

        [TearDown]
        public void TearDown()
        {
            _systems?.Destroy();
            _world.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void AFireballHitSetsTheEnemyOnFireAndItBurnsEveryHalfSecond()
        {
            SearingFireballConfig config = Asset<SearingFireballConfig>();
            SearingFireballLevel level = new SearingFireballLevel();
            Set(level, "_damagePerSecond", 10f);
            Set(level, "_seconds", 2f);
            Set(level, "_critChanceBonus", 0.2f);
            Set(config, "_levels", new[] { level });
            Set(config, "_burningFxPrefab", _fx);

            Build(config, new BurningSystem());

            int enemy = Enemy(new Vector3(0f, 0f, 3f));
            Damage(enemy, 20f, DamageKind.Weapon, DamageElement.Fire);

            Step(1);
            ClearDamage();

            Assert.IsTrue(_world.GetPool<Burning>().Has(enemy));
            Assert.AreEqual(0.2f, _world.GetPool<Burning>().Get(enemy).CritChanceBonus, 1e-6f);

            float burned = 0f;

            for (int tick = 0; tick < 150; tick++)
                burned += StepAndCollect(enemy);

            Assert.AreEqual(20f, burned, 1e-3f, "10 per second for 2 seconds, in 5-point ticks");
            Assert.IsFalse(_world.GetPool<Burning>().Has(enemy), "the fire goes out");
        }

        [Test]
        public void IceDamageDoesNotIgnite()
        {
            SearingFireballConfig config = Asset<SearingFireballConfig>();
            Set(config, "_levels", new[] { new SearingFireballLevel() });
            Set(config, "_burningFxPrefab", _fx);

            Build(config, new BurningSystem());

            int enemy = Enemy(new Vector3(0f, 0f, 3f));
            Damage(enemy, 20f, DamageKind.Weapon, DamageElement.Ice);

            Step(1);

            Assert.IsFalse(_world.GetPool<Burning>().Has(enemy));
        }

        [Test]
        public void TheArcaneWaveHitsEachEnemyOnceAndHarderWhenItSuffersAnElement()
        {
            ArcaneWaveConfig config = Asset<ArcaneWaveConfig>();
            ArcaneWaveLevel level = new ArcaneWaveLevel();
            Set(level, "_cooldownSeconds", 6f);
            Set(level, "_damage", 20f);
            Set(level, "_radius", 5f);
            Set(level, "_affectedBonus", 0.5f);
            Set(config, "_levels", new[] { level });
            Set(config, "_waveFxPrefab", _fx);

            Build(config, new ArcaneWaveSystem());

            int plain = Enemy(new Vector3(0f, 0f, 2f));
            int frozen = Enemy(new Vector3(0f, 0f, -4f));
            int far = Enemy(new Vector3(9f, 0f, 0f));
            _world.GetPool<Frozen>().Add(frozen).RemainingTicks = 600;

            Dictionary<int, float> totals = new Dictionary<int, float>();

            for (int tick = 0; tick < 60; tick++)
            {
                Step(1);
                Collect(totals);
                ClearDamage();
            }

            Assert.AreEqual(20f, Get(totals, plain), 1e-3f, "one hit, not one per tick");
            Assert.AreEqual(30f, Get(totals, frozen), 1e-3f, "+50% on a frozen enemy");
            Assert.AreEqual(0f, Get(totals, far));
            Assert.IsTrue(_world.GetPool<ArcaneWaveCooldown>().Has(_hero));
            Assert.AreEqual(0, _world.Filter<ArcaneWave>().End().GetEntitiesCount(), "the wave ends at its radius");
        }

        [Test]
        public void TheLightningShieldStrikesOnlyTheNearestFewWithinItsRadius()
        {
            LightningShieldConfig config = Asset<LightningShieldConfig>();
            LightningShieldLevel level = new LightningShieldLevel();
            Set(level, "_intervalSeconds", 0.5f);
            Set(level, "_maxTargets", 2);
            Set(level, "_damage", 8f);
            Set(config, "_levels", new[] { level });
            Set(config, "_radius", 2.6f);
            Set(config, "_auraFxPrefab", _fx);
            Set(config, "_boltPrefab", _fx);
            Set(config, "_impactFxPrefab", _fx);

            Build(config, new LightningShieldSystem());

            int nearest = Enemy(new Vector3(1f, 0f, 0f));
            int second = Enemy(new Vector3(0f, 0f, -1.5f));
            int third = Enemy(new Vector3(-2f, 0f, 0f));
            int outside = Enemy(new Vector3(0f, 0f, 5f));

            Step(1);

            Dictionary<int, float> totals = new Dictionary<int, float>();
            Collect(totals);

            Assert.AreEqual(8f, Get(totals, nearest), 1e-3f);
            Assert.AreEqual(8f, Get(totals, second), 1e-3f);
            Assert.AreEqual(0f, Get(totals, third), "only two at a time");
            Assert.AreEqual(0f, Get(totals, outside));

            foreach (int entity in _world.Filter<DamageEvent>().End())
                Assert.AreEqual(DamageElement.Nature, _world.GetPool<DamageEvent>().Get(entity).Element);
        }

        [Test]
        public void ATornadoDamagesAndSlowsWhatItTouches()
        {
            TornadoConfig config = Asset<TornadoConfig>();
            TornadoLevel level = new TornadoLevel();
            Set(level, "_cooldownSeconds", 7f);
            Set(level, "_count", 1);
            Set(level, "_damage", 6f);
            Set(config, "_levels", new[] { level });
            Set(config, "_radius", 1.5f);
            Set(config, "_tornadoPrefab", _fx);

            Build(config, new TornadoSystem());

            int enemy = Enemy(new Vector3(0.5f, 0f, 0.5f));

            Step(2);

            Dictionary<int, float> totals = new Dictionary<int, float>();
            Collect(totals);

            Assert.AreEqual(6f, Get(totals, enemy), 1e-3f);
            Assert.IsTrue(_world.GetPool<Slowed>().Has(enemy));
            Assert.AreEqual(0.4f, _world.GetPool<Slowed>().Get(enemy).Factor, 1e-6f);
            Assert.AreEqual(1, _world.Filter<Tornado>().End().GetEntitiesCount());
        }

        [Test]
        public void AMarkedEnemyKilledByItsElementExplodesAndOtherKillsDoNot()
        {
            ElementalWeaknessConfig config = Asset<ElementalWeaknessConfig>();
            Set(config, "_fireExplosionPrefab", _fx);
            Set(config, "_iceExplosionPrefab", _fx);
            Set(config, "_arcaneExplosionPrefab", _fx);

            Build(config, new ExplodeMarkedKillsSystem());

            int matched = Enemy(new Vector3(3f, 0f, 0f));
            int mismatched = Enemy(new Vector3(-3f, 0f, 0f));

            Kill(matched, config, DamageElement.Fire, DamageElement.Fire);
            Kill(mismatched, config, DamageElement.Ice, DamageElement.Fire);

            Step(1);

            EcsFilter strikes = _world.Filter<AreaStrike>().End();
            Assert.AreEqual(1, strikes.GetEntitiesCount());

            foreach (int strike in strikes)
            {
                AreaStrike data = _world.GetPool<AreaStrike>().Get(strike);
                Assert.AreEqual(DamageElement.Fire, data.Element);
                Assert.AreEqual(new Vector3(3f, 0f, 0f), data.Origin);
                Assert.AreEqual(30f, data.Damage, 1e-3f);
            }
        }

        [Test]
        public void TheMarkAmplifiesOnlyItsOwnElement()
        {
            _systems = new EcsSystems(_world);
            _systems.Add(new ResolveHitOutcomeSystem());
            _systems.Inject(_clock, new RunContext("level.test", "character.test", RunMode.Story, 1, Array.Empty<StatModifierSpec>()));
            _systems.Init();

            int enemy = Enemy(Vector3.zero);
            _world.GetPool<Health>().Get(enemy).Current = 100f;
            ref ElementMark mark = ref _world.GetPool<ElementMark>().Add(enemy);
            mark.Element = DamageElement.Ice;
            mark.DamageBonus = 0.5f;

            Damage(enemy, 10f, DamageKind.Perk, DamageElement.Ice);
            Damage(enemy, 10f, DamageKind.Perk, DamageElement.Fire);

            Step(1);

            float ice = 0f;
            float fire = 0f;

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                DamageEvent damage = _world.GetPool<DamageEvent>().Get(entity);

                if (damage.Element == DamageElement.Ice)
                    ice = damage.Amount;
                else
                    fire = damage.Amount;
            }

            Assert.AreEqual(15f, ice, 1e-3f);
            Assert.AreEqual(10f, fire, 1e-3f);
        }

        private void Kill(int enemy, ElementalWeaknessConfig config, DamageElement markElement, DamageElement blowElement)
        {
            ref ElementMark mark = ref _world.GetPool<ElementMark>().Add(enemy);
            mark.Config = config;
            mark.Source = _world.PackEntity(_hero);
            mark.Element = markElement;
            mark.ExplosionDamage = 30f;

            _world.GetPool<DiedEvent>().Add(enemy);
            _world.GetPool<KillingBlow>().Add(enemy).Element = blowElement;
        }

        private void Build(PerkBehaviourConfig behaviour, params IEcsSystem[] list)
        {
            PerkConfig perk = Asset<PerkConfig>();
            Set(perk, "_id", "perk.test");
            Set(perk, "_behaviour", behaviour);
            Set(perk, "_levels", new[] { new PerkLevel() });

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perk.Id;
            taken.Level = 1;

            WaveTimelineConfig timeline = Asset<WaveTimelineConfig>();
            LevelConfig level = Asset<LevelConfig>();
            Set(level, "_waves", timeline);
            Set(level, "_arenaRadius", 45f);

            _systems = new EcsSystems(_world);

            foreach (IEcsSystem system in list)
                _systems.Add(system);

            _systems.Inject(
                _clock,
                level,
                (IViewFactory)new RecordingViewFactory(),
                (IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }),
                new RunContext("level.test", "character.test", RunMode.Story, 1, Array.Empty<StatModifierSpec>()));
            _systems.Init();
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;
            _world.GetPool<Health>().Add(enemy).Current = 1000f;
            _world.GetPool<Velocity>().Add(enemy);
            _world.GetPool<MoveIntent>().Add(enemy);

            return enemy;
        }

        private void Damage(int target, float amount, DamageKind kind, DamageElement element)
        {
            ref DamageEvent damage = ref _world.GetPool<DamageEvent>().Add(_world.NewEntity());
            damage.Target = _world.PackEntity(target);
            damage.Source = _world.PackEntity(_hero);
            damage.Amount = amount;
            damage.Kind = kind;
            damage.Element = element;
            damage.IsMagic = true;
            damage.IsCrit = true;
        }

        private float StepAndCollect(int target)
        {
            Step(1);

            Dictionary<int, float> totals = new Dictionary<int, float>();
            Collect(totals);
            ClearDamage();

            return Get(totals, target);
        }

        private void Collect(Dictionary<int, float> totals)
        {
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                DamageEvent damage = pool.Get(entity);

                if (damage.Target.Unpack(_world, out int target) == false)
                    continue;

                totals[target] = Get(totals, target) + damage.Amount;
            }
        }

        private void ClearDamage()
        {
            EcsFilter events = _world.Filter<DamageEvent>().End();
            int[] copy = new int[events.GetEntitiesCount()];
            int index = 0;

            foreach (int entity in events)
                copy[index++] = entity;

            foreach (int entity in copy)
                _world.DelEntity(entity);
        }

        private static float Get(Dictionary<int, float> totals, int key)
        {
            return totals.TryGetValue(key, out float value) ? value : 0f;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}

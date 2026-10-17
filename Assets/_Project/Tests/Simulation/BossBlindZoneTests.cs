using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class BossBlindZoneTests
    {
        private EcsWorld _world;
        private EcsSystems _systems;
        private BossConfig _config;
        private int _boss;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BossConfig>();

            _world = new EcsWorld();

            _boss = _world.NewEntity();
            _world.GetPool<Enemy>().Add(_boss);
            _world.GetPool<Boss>().Add(_boss).Config = _config;
            _world.GetPool<EnemyAction>().Add(_boss).Phase = EnemyActionPhase.Spinning;
            _world.GetPool<Position>().Add(_boss).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(_boss).Value = Vector3.forward;

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);

            _systems = new EcsSystems(_world);
            _systems.Add(new ApplyBossBlindZoneSystem());
            _systems.Inject();
            _systems.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();
            Object.DestroyImmediate(_config);
        }

        [TestCase(0f, 10f)]
        [TestCase(45f, 10f)]
        [TestCase(90f, 2.5f)]
        [TestCase(-90f, 2.5f)]
        [TestCase(150f, 0f)]
        [TestCase(180f, 0f)]
        public void DamageWhileCastingDependsOnWhereTheHeroStands(float angleDegrees, float expected)
        {
            PlaceHero(angleDegrees);

            Assert.AreEqual(expected, Run(_boss), 1e-4f);
        }

        [Test]
        public void OutsideACastTheBossHitsFullFromAnySide()
        {
            _world.GetPool<EnemyAction>().Get(_boss).Phase = EnemyActionPhase.None;
            PlaceHero(180f);

            Assert.AreEqual(10f, Run(_boss), 1e-4f);
        }

        [Test]
        public void AnOrdinaryEnemyIsNotAffected()
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = Vector3.zero;
            _world.GetPool<Facing>().Add(enemy).Value = Vector3.forward;
            PlaceHero(180f);

            Assert.AreEqual(10f, Run(enemy), 1e-4f);
        }

        private void PlaceHero(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            _world.GetPool<Position>().Get(_hero).Value = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * 2f;
        }

        private float Run(int source)
        {
            int entity = _world.NewEntity();

            ref DamageEvent damageEvent = ref _world.GetPool<DamageEvent>().Add(entity);
            damageEvent.Target = _world.PackEntity(_hero);
            damageEvent.Source = _world.PackEntity(source);
            damageEvent.Amount = 10f;
            damageEvent.Kind = DamageKind.EnemyAbility;

            _systems.Run();

            return _world.GetPool<DamageEvent>().Get(entity).Amount;
        }
    }
}

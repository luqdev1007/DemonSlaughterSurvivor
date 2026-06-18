using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Configs;
using DemonSlaughter.Core.Services;
using DemonSlaughter.Gameplay.Combat;
using DemonSlaughter.Gameplay.ECS;
using DemonSlaughter.Gameplay.ECS.Components;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Characters
{
    public sealed class HeroFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly EcsPipeline _pipeline;

        public HeroFactory(IAssetProvider assetProvider, EcsPipeline pipeline)
        {
            _assetProvider = assetProvider;
            _pipeline = pipeline;
        }

        public async UniTask<int> CreatePlayerAsync(HeroConfig config, Vector3 spawnPoint)
        {
            var prefab = await _assetProvider.LoadAsync<GameObject>(config.AddressableAddress);
            GameObject instance = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);

            return SetupEntity(config, instance);
        }

        private int SetupEntity(HeroConfig config, GameObject instance)
        {
            var world = _pipeline.World;
            var entity = world.NewEntity();

            // base
            world.GetPool<PlayerTagComponent>().Add(entity);
            world.GetPool<CameraTargetTag>().Add(entity);
            world.GetPool<MoveInputComponent>().Add(entity);

            ref var movement = ref world.GetPool<MovementComponent>().Add(entity);
            movement.Speed = config.MoveSpeed;

            ref var transformRef = ref world.GetPool<TransformRefComponent>().Add(entity);
            transformRef.Value = instance.transform;

            ref var animatorRef = ref world.GetPool<AnimatorRefComponent>().Add(entity);
            animatorRef.Value = instance.GetComponentInChildren<Animator>();

            // Combat
            ref var attackState = ref world.GetPool<AttackStateComponent>().Add(entity);
            attackState.Cooldown = 0f;
            attackState.ComboIndex = 0;
            attackState.IsAttacking = false;

            ref var detector = ref world.GetPool<AttackDetectorComponent>().Add(entity);
            detector.DetectionRadius = config.DetectionRadius;
            detector.DetectionAngle = config.DetectionAngle;

            // Weapon setup
            var swordBone = instance.transform.FindDeepChild(config.SwordBoneName);
            var hitboxAdapter = swordBone.gameObject.AddComponent<SwordHitboxEcsAdapter>();
            hitboxAdapter.Initialize(world, entity, config.AttackDamage);

            var swordHitbox = swordBone.GetComponent<SwordHitbox>();

            var animatorGO = instance.GetComponentInChildren<Animator>().gameObject;

            var eventReceiver = animatorGO.GetComponent<AttackAnimationEventReceiver>();

            eventReceiver.Initialize(world, entity, config.AttackCooldown, swordHitbox);

            ref var weapon = ref world.GetPool<WeaponComponent>().Add(entity);
            weapon.SwordBone = swordBone;
            weapon.HitboxRadius = config.HitboxRadius;
            weapon.Damage = config.AttackDamage;

            return entity;
        }
    }
}
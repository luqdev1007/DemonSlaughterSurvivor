using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartBowDrawSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Bow, WeaponReady, WeaponCooldown, AttackSpeed, OwnerLink>, Exc<BowDraw>> _bows = default;
        private readonly EcsFilterInject<Inc<PowerShotCooldown>> _powerCooldowns = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Bow> _bowPool = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AttackSpeed> _attackSpeeds = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<BowDraw> _draws = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<PowerShotCooldown> _powerCooldownPool = default;
        private readonly EcsPoolInject<Duality> _dualities = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _powerCooldowns.Value)
            {
                ref PowerShotCooldown cooldown = ref _powerCooldownPool.Value.Get(hero);

                cooldown.Progress += ResolveCooldownRate(world, hero);

                while (cooldown.Progress >= 1f && cooldown.RemainingTicks > 0)
                {
                    cooldown.Progress -= 1f;
                    cooldown.RemainingTicks--;
                }

                if (cooldown.RemainingTicks <= 0)
                    _powerCooldownPool.Value.Del(hero);
            }

            foreach (int bow in _bows.Value)
            {
                ref WeaponReady ready = ref _readies.Value.Get(bow);

                if (ready.Remaining > 0f)
                    continue;

                if (_ownerLinks.Value.Get(bow).Owner.Unpack(world, out int owner) == false || BowShot.IsBlocked(owner, _dead.Value, _backleaps.Value, ShootsInAir(world, owner)))
                    continue;

                if (_aims.Value.Has(owner) == false)
                    continue;

                BowConfig config = _bowPool.Value.Get(bow).Config;
                ref TorsoAim aim = ref _aims.Value.Get(owner);

                if (HasShot(world, in aim, _positions.Value.Get(owner).Value, config) == false)
                    continue;

                if (Vector3.Angle(aim.Direction, aim.Desired) > config.AimToleranceDegrees)
                    continue;

                bool empowered = IsPowerShotReady(world, owner, out PowerShotConfig power);
                float drawSeconds = config.DrawSeconds * (empowered ? power.DrawMultiplier : 1f);
                float attackSpeed = _attackSpeeds.Value.Get(bow).Value;
                int ticks = Mathf.Max(1, Mathf.RoundToInt(drawSeconds / attackSpeed / delta));

                ref BowDraw draw = ref _draws.Value.Add(bow);
                draw.RemainingTicks = ticks;
                draw.Empowered = empowered;
                draw.PlaybackSpeed = config.DrawClipSeconds / (ticks * delta);

                ready.Remaining = _cooldowns.Value.Get(bow).Value / attackSpeed;

                if (_views.Value.Has(owner) && _views.Value.Get(owner).Value != null)
                    _views.Value.Get(owner).Value.PlayAttack(config.DrawTrigger, draw.PlaybackSpeed);
            }
        }

        private bool ShootsInAir(EcsWorld world, int owner)
        {
            return _backleaps.Value.Has(owner) && TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out DemonicAgilityConfig _, out _);
        }

        private float ResolveCooldownRate(EcsWorld world, int hero)
        {
            if (DualityForm.IsHuman(_dualities.Value, hero) == false)
                return 1f;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out PowerShotConfig power, out _) == false)
                return 1f;

            return 1f / power.HumanCooldownScale;
        }

        private bool IsPowerShotReady(EcsWorld world, int owner, out PowerShotConfig power)
        {
            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out power, out _) == false)
                return false;

            return _powerCooldownPool.Value.Has(owner) == false;
        }

        private bool HasShot(EcsWorld world, in TorsoAim aim, Vector3 position, BowConfig config)
        {
            if (aim.Manual)
                return true;

            if (aim.HasTarget == false)
                return false;

            if (RangedTarget.TryFindLiving(world, aim.Target, _positions.Value, _dead.Value, out Vector3 target) == false)
                return false;

            return RangedTarget.IsWithin(position, target, config.Range);
        }
    }
}

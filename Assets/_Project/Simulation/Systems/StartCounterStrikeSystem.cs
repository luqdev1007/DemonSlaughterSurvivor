using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartCounterStrikeSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, CounterStrikeArmed, Position, Facing>> _armedHeroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<CounterStrikeArmed> _armed = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AttackSpeed> _attackSpeeds = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;
        private readonly EcsPoolInject<PoweringUp> _poweringUps = default;
        private readonly EcsPoolInject<Swing> _swings = default;
        private readonly EcsPoolInject<SpecialSwing> _specialSwings = default;
        private readonly EcsPoolInject<SwingStarted> _started = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _armedHeroes.Value)
            {
                ref CounterStrikeArmed armed = ref _armed.Value.Get(hero);

                armed.RemainingTicks--;

                if (_dead.Value.Has(hero) || armed.RemainingTicks <= 0)
                {
                    _armed.Value.Del(hero);

                    continue;
                }

                if (_dashing.Value.Has(hero) || _specialAttacks.Value.Has(hero) || _poweringUps.Value.Has(hero))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out CounterStrikeConfig counter, out int _) == false)
                {
                    _armed.Value.Del(hero);

                    continue;
                }

                if (TryFindFreeWeapon(world, hero, out int weapon) == false)
                    continue;

                Strike(world, hero, weapon, counter, counter.Level(armed.Level));

                _armed.Value.Del(hero);
            }
        }

        private bool TryFindFreeWeapon(EcsWorld world, int hero, out int weapon)
        {
            foreach (int entity in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(entity).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                weapon = entity;

                if (_swinging.Value.Has(entity))
                    return false;

                return _readies.Value.Has(entity) == false || _readies.Value.Get(entity).Remaining <= 0f;
            }

            weapon = -1;

            return false;
        }

        private void Strike(EcsWorld world, int hero, int weapon, CounterStrikeConfig counter, CounterStrikeLevel entry)
        {
            SwingVariant variant = counter.Swing;
            SwingBake bake = variant.Bake;

            float delta = _clock.Value.Delta;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(bake.ClipLength / variant.PlaybackSpeed / delta));

            SwingPose pose = new SwingPose(_positions.Value.Get(hero).Value, _facings.Value.Get(hero).Value);

            int entity = world.NewEntity();

            ref Swing swing = ref _swings.Value.Add(entity);
            swing.Weapon = world.PackEntity(weapon);
            swing.Owner = world.PackEntity(hero);
            swing.Variant = variant;
            swing.DamageScale = entry.DamageShare;
            swing.Kind = DamageKind.Perk;
            swing.SourcePerk = counter;
            swing.ClipTime = 0f;
            swing.PlaybackSpeed = bake.ClipLength / (ticks * delta);
            swing.RemainingTicks = ticks;

            bake.SampleAt(0f, out Vector3 hand, out Vector3 tip);

            swing.PreviousHand = pose.ToWorld(hand);
            swing.PreviousTip = pose.ToWorld(tip);

            _specialSwings.Value.Add(entity);
            _started.Value.Add(entity);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(entity);
            special.LocksMovement = counter.LocksMovement;

            if (_readies.Value.Has(weapon) && _cooldowns.Value.Has(weapon))
            {
                float attackSpeed = _attackSpeeds.Value.Has(weapon) ? _attackSpeeds.Value.Get(weapon).Value : 1f;

                _readies.Value.Get(weapon).Remaining = _cooldowns.Value.Get(weapon).Value / attackSpeed;
            }
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class TriggerCounterStrikeSystem : IEcsRunSystem
    {
        public const string RandomStreamId = "stream.counter-strike";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, Facing>, Exc<Dead, Dashing, SpecialAttack>> _heroes = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink>> _weapons = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<CounterStrikeRandom> _randoms = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Swing> _swings = default;
        private readonly EcsPoolInject<SpecialSwing> _specialSwings = default;
        private readonly EcsPoolInject<SwingStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (WasHit(world, hero) == false)
                    continue;

                if (TryFindCounterStrike(world, hero, out CounterStrikeConfig counter, out int level) == false)
                    continue;

                if (TryFindWeapon(world, hero, out int weapon) == false)
                    continue;

                CounterStrikeLevel entry = counter.Level(level);

                if (SimulationRandom.NextUnit(ref ResolveRandom(hero).State) >= entry.Chance)
                    continue;

                Strike(world, hero, weapon, counter, entry);
            }
        }

        private bool WasHit(EcsWorld world, int hero)
        {
            foreach (int entity in _applied.Value)
            {
                if (_damageEvents.Value.Get(entity).Target.Unpack(world, out int target) && target == hero)
                    return true;
            }

            return false;
        }

        private bool TryFindCounterStrike(EcsWorld world, int hero, out CounterStrikeConfig counter, out int level)
        {
            foreach (int entity in _taken.Value)
            {
                ref TakenPerk taken = ref _takenPool.Value.Get(entity);

                if (taken.Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                PerkConfig perk = _content.Value.Get<PerkConfig>(taken.PerkId);

                if (perk.CounterStrike == null)
                    continue;

                counter = perk.CounterStrike;
                level = taken.Level;

                return true;
            }

            counter = null;
            level = 0;

            return false;
        }

        private bool TryFindWeapon(EcsWorld world, int hero, out int weapon)
        {
            foreach (int entity in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(entity).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                weapon = entity;

                return true;
            }

            weapon = -1;

            return false;
        }

        private ref CounterStrikeRandom ResolveRandom(int hero)
        {
            if (_randoms.Value.Has(hero))
                return ref _randoms.Value.Get(hero);

            ref CounterStrikeRandom random = ref _randoms.Value.Add(hero);
            random.State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

            return ref random;
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
        }
    }
}

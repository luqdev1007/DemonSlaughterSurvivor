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

        private readonly EcsFilterInject<Inc<Player>, Exc<Dead, Dashing, SpecialAttack, CounterStrikeArmed>> _heroes = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<CounterStrikeRandom> _randoms = default;
        private readonly EcsPoolInject<CounterStrikeArmed> _armed = default;

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

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out CounterStrikeConfig counter, out int level) == false)
                    continue;

                CounterStrikeLevel entry = counter.Level(level);

                if (SimulationRandom.NextUnit(ref ResolveRandom(hero).State) >= entry.Chance)
                    continue;

                ref CounterStrikeArmed armed = ref _armed.Value.Add(hero);
                armed.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(counter.ArmedSeconds / _clock.Value.Delta));
                armed.Level = level;
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

        private ref CounterStrikeRandom ResolveRandom(int hero)
        {
            if (_randoms.Value.Has(hero))
                return ref _randoms.Value.Get(hero);

            ref CounterStrikeRandom random = ref _randoms.Value.Add(hero);
            random.State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

            return ref random;
        }
    }
}

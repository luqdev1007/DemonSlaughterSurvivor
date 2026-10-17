using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ApplyExecutionThirstSystem : IEcsRunSystem
    {
        public const string ModifierSource = "perk.execution-thirst";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>> _heroes = default;
        private readonly EcsFilterInject<Inc<Execution>> _executions = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Execution> _executionPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ExecutionThirstActive> _active = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                bool applied = _active.Value.Has(hero);
                bool executing = _dead.Value.Has(hero) == false && IsExecuting(world, hero);

                if (executing == applied)
                    continue;

                if (executing == false)
                {
                    _statModifiers.Value.RemoveBySource(hero, ModifierSource);
                    _active.Value.Del(hero);

                    continue;
                }

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ExecutionThirstConfig thirst, out int level) == false)
                    continue;

                _statModifiers.Value.Add(hero, StatId.DamageTaken, StatOp.More, -thirst.Level(level).DamageReduction, ModifierSource);
                _active.Value.Add(hero);
            }
        }

        private bool IsExecuting(EcsWorld world, int hero)
        {
            foreach (int entity in _executions.Value)
            {
                if (_executionPool.Value.Get(entity).Owner.Unpack(world, out int owner) && owner == hero)
                    return true;
            }

            return false;
        }
    }
}

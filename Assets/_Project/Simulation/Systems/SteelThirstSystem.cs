using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SteelThirstSystem : IEcsRunSystem
    {
        public const string SourceId = "trait.steel-thirst";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<SwingEndedEvent>> _ended = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<SwingEndedEvent> _endedPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<SteelThirst> _thirsts = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _ended.Value)
            {
                ref SwingEndedEvent ended = ref _endedPool.Value.Get(entity);

                if (ended.Owner.Unpack(world, out int hero) == false || _players.Value.Has(hero) == false || _dead.Value.Has(hero))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out SteelThirstConfig config, out int _) == false)
                    continue;

                if (_thirsts.Value.Has(hero) == false)
                    _thirsts.Value.Add(hero);

                ref SteelThirst thirst = ref _thirsts.Value.Get(hero);

                if (thirst.RemainingSwings > 0)
                {
                    thirst.RemainingSwings--;

                    if (thirst.RemainingSwings == 0)
                        _statModifiers.Value.RemoveBySource(hero, SourceId);
                }

                if (ended.EnemyHits < config.MinTargets)
                    continue;

                thirst.RemainingSwings = config.Swings;
                _statModifiers.Value.Add(hero, StatId.AttackSpeed, StatOp.More, config.AttackSpeedBonus, SourceId);
            }
        }
    }
}

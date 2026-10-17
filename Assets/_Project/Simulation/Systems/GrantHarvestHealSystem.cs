using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class GrantHarvestHealSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<SwingEndedEvent>> _ended = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<SwingEndedEvent> _endedPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<HealEvent> _healEvents = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _ended.Value)
            {
                ref SwingEndedEvent ended = ref _endedPool.Value.Get(entity);

                if (ended.Owner.Unpack(world, out int hero) == false || _players.Value.Has(hero) == false || _dead.Value.Has(hero))
                    continue;

                if (_maxHealths.Value.Has(hero) == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out HarvestConfig harvest, out int level) == false)
                    continue;

                float share = harvest.HealShare(harvest.Level(level), ended.EnemyHits);

                if (share <= 0f)
                    continue;

                ref HealEvent heal = ref _healEvents.Value.Add(world.NewEntity());
                heal.Target = ended.Owner;
                heal.Amount = share * _maxHealths.Value.Get(hero).Value;
            }
        }
    }
}

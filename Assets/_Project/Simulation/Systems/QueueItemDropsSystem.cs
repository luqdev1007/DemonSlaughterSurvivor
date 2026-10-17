using Game.Configs;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class QueueItemDropsSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Enemy, Elite, DiedEvent>> _dying = default;
        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<PendingItemDrops> _drops = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;

        public void Run(IEcsSystems systems)
        {
            int dying = _dying.Value.GetEntitiesCount();

            if (dying == 0)
                return;

            WaveTimelineConfig timeline = _level.Value.Waves;

            if (timeline == null || timeline.ItemLoot == null)
                return;

            foreach (int hero in _heroes.Value)
            {
                if (_drops.Value.Has(hero) == false)
                    _drops.Value.Add(hero);

                _drops.Value.Get(hero).Count += dying;
            }
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class QueueItemDropsSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "stream.item-drops";

        private readonly EcsFilterInject<Inc<Enemy, Elite, DiedEvent>> _dying = default;
        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<PendingItemDrops> _drops = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private uint _random;
        private float _lastDropTime = float.NegativeInfinity;

        public void Init(IEcsSystems systems)
        {
            _random = SimulationRandom.StreamState(_context.Value == null ? 0 : _context.Value.Seed, RandomStreamId);
        }

        public void Run(IEcsSystems systems)
        {
            int dying = _dying.Value.GetEntitiesCount();

            if (dying == 0)
                return;

            WaveTimelineConfig timeline = _level.Value.Waves;

            if (timeline == null || timeline.ItemLoot == null)
                return;

            ItemLootConfig loot = timeline.ItemLoot;
            float now = _clock.Value.Elapsed;
            int earned = 0;

            for (int index = 0; index < dying; index++)
            {
                if (now - _lastDropTime < loot.MinDropIntervalSeconds)
                    break;

                if (SimulationRandom.NextUnit(ref _random) >= loot.DropChance)
                    continue;

                earned++;
                _lastDropTime = now;
            }

            if (earned == 0)
                return;

            foreach (int hero in _heroes.Value)
            {
                if (_drops.Value.Has(hero) == false)
                    _drops.Value.Add(hero);

                ref PendingItemDrops drops = ref _drops.Value.Get(hero);
                drops.Count = System.Math.Min(loot.MaxPendingDrops, drops.Count + earned);
            }
        }
    }
}

using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class UpdateAirborneSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Height>> _filter = default;

        private readonly EcsPoolInject<Height> _heights = default;
        private readonly EcsPoolInject<Airborne> _airborne = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Height height = ref _heights.Value.Get(entity);
                bool above = height.Value > height.Clearance;
                bool marked = _airborne.Value.Has(entity);

                if (above && marked == false)
                    _airborne.Value.Add(entity);
                else if (above == false && marked)
                    _airborne.Value.Del(entity);
            }
        }
    }
}

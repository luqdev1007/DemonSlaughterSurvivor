using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncLocomotionViewSystem : IEcsRunSystem
    {
        private const float MinIntentSqr = 1e-6f;

        private readonly EcsFilterInject<Inc<MoveIntent, View>> _filter = default;

        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                ref MoveIntent intent = ref _intents.Value.Get(entity);

                view.Value.SetRunning(intent.Value.sqrMagnitude > MinIntentSqr);
            }
        }
    }
}

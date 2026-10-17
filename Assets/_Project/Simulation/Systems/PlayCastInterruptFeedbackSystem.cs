using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayCastInterruptFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<CastInterruptedEvent, View>> _interrupted = default;

        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _interrupted.Value)
            {
                if (_views.Value.Get(entity).Value is not ICastInterruptView view)
                    continue;

                view.PlayCastInterrupted();
            }
        }
    }
}

using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayExecutionFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Execution, ExecutionStarted>> _started = default;

        private readonly EcsPoolInject<Execution> _executions = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref Execution execution = ref _executions.Value.Get(entity);

                if (execution.Owner.Unpack(world, out int owner) == false || _views.Value.Has(owner) == false)
                    continue;

                ref View view = ref _views.Value.Get(owner);

                if (view.Value == null)
                    continue;

                view.Value.PlayAttack(execution.Config.AnimatorTrigger, execution.Config.PlaybackSpeed);
            }
        }
    }
}

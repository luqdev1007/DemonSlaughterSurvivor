using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class FinishRunOnPlayerDeathSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Dead, PendingFinish>> _filter = default;

        private readonly EcsPoolInject<PendingFinish> _pendingFinishes = default;

        private readonly EcsCustomInject<RunOutcome> _outcome = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref PendingFinish pending = ref _pendingFinishes.Value.Get(entity);

                if (pending.RemainingTicks > 0)
                    continue;

                _outcome.Value.Finish(RunEndReason.Death);
            }
        }
    }
}

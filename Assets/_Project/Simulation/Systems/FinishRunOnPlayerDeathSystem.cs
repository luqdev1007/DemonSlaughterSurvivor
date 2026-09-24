using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class FinishRunOnPlayerDeathSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Dead>> _filter = default;

        private readonly EcsCustomInject<RunOutcome> _outcome = default;

        public void Run(IEcsSystems systems)
        {
            if (_filter.Value.GetEntitiesCount() == 0)
                return;

            _outcome.Value.Finish();
        }
    }
}

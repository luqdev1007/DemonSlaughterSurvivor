using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class DebugVictorySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player>, Exc<Dead>> _heroes = default;

        private readonly EcsCustomInject<IDebugVictoryInput> _input = default;
        private readonly EcsCustomInject<RunOutcome> _outcome = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumePressed() == false)
                return;

            if (_heroes.Value.GetEntitiesCount() == 0)
                return;

            _outcome.Value.Finish(RunEndReason.Victory);
        }
    }
}

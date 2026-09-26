using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class DebugStatModifierSystem : IEcsRunSystem
    {
        private const string SourceId = "debug";
        private const float MoveSpeedIncrease = 0.5f;

        private readonly EcsFilterInject<Inc<Player, MoveSpeed>, Exc<Dead>> _players = default;

        private readonly EcsCustomInject<IDebugStatInput> _input = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.ConsumePressed() == false)
                return;

            StatModifiers statModifiers = _statModifiers.Value;

            foreach (int player in _players.Value)
            {
                if (statModifiers.RemoveBySource(player, SourceId) > 0)
                    continue;

                statModifiers.Add(player, StatId.MoveSpeed, StatOp.Increased, MoveSpeedIncrease, SourceId);
            }
        }
    }
}

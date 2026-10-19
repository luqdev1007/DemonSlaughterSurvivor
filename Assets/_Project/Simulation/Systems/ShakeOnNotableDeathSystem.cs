using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ShakeOnNotableDeathSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Enemy, Dead, Boss>> _bosses = default;
        private readonly EcsFilterInject<Inc<Enemy, Dead, EliteAffix>, Exc<Boss>> _elites = default;

        private readonly EcsCustomInject<IScreenShake> _shake = default;

        public void Run(IEcsSystems systems)
        {
            if (_bosses.Value.GetEntitiesCount() > 0)
            {
                _shake.Value.Shake(ShakeKind.BossDeath);

                return;
            }

            if (_elites.Value.GetEntitiesCount() > 0)
                _shake.Value.Shake(ShakeKind.EliteDeath);
        }
    }
}

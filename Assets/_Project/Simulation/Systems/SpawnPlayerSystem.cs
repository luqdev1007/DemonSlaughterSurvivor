using Game.Core;
using Game.Simulation.Assets._Project.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation
{
    public sealed class SpawnPlayerSystem : IEcsInitSystem, IEcsDestroySystem
    {
        private readonly EcsWorldInject _world = default;
        private readonly EcsPoolInject<Player> _players = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _views = default;

        public void Init(IEcsSystems systems)
        {
            throw new System.NotImplementedException();
        }

        public void Destroy(IEcsSystems systems)
        {
            throw new System.NotImplementedException();
        }
    }
}

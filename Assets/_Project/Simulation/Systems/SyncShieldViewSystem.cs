using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncShieldViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Concentration, View>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _heroes.Value)
            {
                if (_views.Value.Get(hero).Value is IShieldView view)
                    view.SetShieldStrength(ConcentrationBonus.Fraction(_concentrations.Value, hero));
            }
        }
    }
}

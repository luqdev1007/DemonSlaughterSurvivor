using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ExplodeMarkedKillsSystem : IEcsRunSystem
    {
        private const float FullCircleDegrees = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, DiedEvent, KillingBlow, ElementMark, Position>> _kills = default;

        private readonly EcsPoolInject<KillingBlow> _killingBlows = default;
        private readonly EcsPoolInject<ElementMark> _marks = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int enemy in _kills.Value)
            {
                ref ElementMark mark = ref _marks.Value.Get(enemy);

                if (_killingBlows.Value.Get(enemy).Element != mark.Element)
                    continue;

                ElementalWeaknessConfig config = mark.Config;
                Vector3 at = _positions.Value.Get(enemy).Value;
                float area = ConcentrationBonus.Area(world, _concentrations.Value, mark.Source);

                ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
                strike.Owner = mark.Source;
                strike.Origin = at;
                strike.Forward = Vector3.forward;
                strike.Reach = config.ExplosionRadius * area;
                strike.HalfAngleDegrees = FullCircleDegrees;
                strike.Damage = mark.ExplosionDamage;
                strike.Kind = DamageKind.Perk;
                strike.IsMagic = true;
                strike.Element = mark.Element;
                strike.SourcePerk = config;

                int fx = world.NewEntity();
                _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ExplosionFxSeconds / _clock.Value.Delta));
                _positions.Value.Add(fx).Value = at;
                _notIndexed.Value.Add(fx);
                _views.Value.Add(fx).Value = AreaScale.Apply(_viewFactory.Value.Create(ElementalWeaknessSystem.ExplosionPrefab(config, mark.Element), at), area);

                _marks.Value.Del(enemy);
            }
        }
    }
}

using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class RestoreConcentrationSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Concentration>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Elite, DiedEvent, KillingBlow>> _eliteKills = default;

        private readonly EcsPoolInject<Concentration> _concentrations = default;
        private readonly EcsPoolInject<KillingBlow> _killingBlows = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                ref Concentration concentration = ref _concentrations.Value.Get(hero);
                float gain = concentration.Config.RegenPerSecond * delta;

                foreach (int elite in _eliteKills.Value)
                {
                    if (_killingBlows.Value.Get(elite).Source.Unpack(world, out int killer) && killer == hero)
                        gain += concentration.Config.PerEliteKill;
                }

                concentration.Value = Mathf.Min(concentration.Config.Max, concentration.Value + gain);

                if (concentration.ShieldDown && concentration.Value >= concentration.Config.ShieldRestoreAt)
                    concentration.ShieldDown = false;
            }
        }
    }
}

using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartBackleapSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<BackleapStats, BackleapRequest, Position, MoveIntent>, Exc<Backleaping, BackleapCooldown, Dead>> _filter = default;

        private readonly EcsPoolInject<BackleapStats> _stats = default;
        private readonly EcsPoolInject<BackleapRequest> _requests = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int hero in _filter.Value)
            {
                BackleapConfig config = _stats.Value.Get(hero).Config;
                Vector3 intent = _intents.Value.Get(hero).Value;
                intent.y = 0f;

                ref Backleaping leap = ref _backleaps.Value.Add(hero);
                leap.Ticks = 0;
                leap.HoverTicks = 0;

                if (intent.sqrMagnitude > Mathf.Epsilon)
                {
                    leap.Phase = BackleapPhase.Leap;
                    leap.PhaseTicks = Ticks(config.LeapSeconds, delta);
                    leap.Direction = ResolveLeapDirection(hero, intent);
                    leap.Speed = config.Distance / (leap.PhaseTicks * delta);

                    int protectedTicks = Ticks(config.InvulnerabilitySeconds, delta);

                    if (config.InvulnerabilitySeconds > 0f)
                        Invulnerability.Grant(_invulnerables.Value, hero, protectedTicks + Invulnerability.TicksSpentBeforeDamage);
                }
                else
                {
                    leap.Phase = BackleapPhase.Hop;
                    leap.PhaseTicks = Ticks(config.HopSeconds, delta);
                    leap.Direction = Vector3.zero;
                    leap.Speed = 0f;
                }

                _requests.Value.Del(hero);
            }
        }

        private Vector3 ResolveLeapDirection(int hero, Vector3 intent)
        {
            if (_aims.Value.Has(hero))
            {
                ref TorsoAim aim = ref _aims.Value.Get(hero);

                if (aim.HasTarget && RangedTarget.TryFindLiving(_world.Value, aim.Target, _positions.Value, _dead.Value, out Vector3 target))
                {
                    Vector3 away = _positions.Value.Get(hero).Value - target;
                    away.y = 0f;

                    if (away.sqrMagnitude > Mathf.Epsilon)
                        return away.normalized;
                }
            }

            return -intent.normalized;
        }

        public static int Ticks(float seconds, float delta)
        {
            return Mathf.Max(1, Mathf.RoundToInt(seconds / delta));
        }
    }
}

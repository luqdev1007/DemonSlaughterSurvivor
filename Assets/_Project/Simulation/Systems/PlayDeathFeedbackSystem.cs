using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PlayDeathFeedbackSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DissolveCountdown, View>> _countdowns = default;
        private readonly EcsFilterInject<Inc<Player, DiedEvent, View>> _died = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<DissolveCountdown> _countdownPool = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private float _delaySeconds;

        public void Init(IEcsSystems systems)
        {
            _delaySeconds = _level.Value.Feedback == null ? 0f : _level.Value.Feedback.HeroDissolveDelaySeconds;
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _countdowns.Value)
            {
                ref DissolveCountdown countdown = ref _countdownPool.Value.Get(entity);

                countdown.RemainingTicks--;

                if (countdown.RemainingTicks > 0)
                    continue;

                _countdownPool.Value.Del(entity);

                Dissolve(entity);
            }

            foreach (int entity in _died.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                view.Value.PlayDeath();

                int ticks = Mathf.RoundToInt(_delaySeconds / _clock.Value.Delta);

                if (ticks <= 0)
                {
                    view.Value.Dissolve();

                    continue;
                }

                _countdownPool.Value.Add(entity).RemainingTicks = ticks;
            }
        }

        private void Dissolve(int entity)
        {
            ref View view = ref _views.Value.Get(entity);

            if (view.Value == null)
                return;

            view.Value.Dissolve();
        }
    }
}

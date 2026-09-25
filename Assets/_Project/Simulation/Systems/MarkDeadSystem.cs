using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class MarkDeadSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Health>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Dead> _deads = default;
        private readonly EcsPoolInject<DiedEvent> _diedEvents = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<DashRequest> _dashRequests = default;
        private readonly EcsPoolInject<PendingFinish> _pendingFinishes = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private float _finishDelaySeconds;

        public void Init(IEcsSystems systems)
        {
            FeedbackConfig feedback = _level.Value.Feedback;

            if (feedback == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{_level.Value.Id}' has no {nameof(FeedbackConfig)} assigned.");

            if (feedback.PlayerDeathDelaySeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has a negative player death delay " +
                    $"({feedback.PlayerDeathDelaySeconds}). Use 0 to leave the run on the tick of death.");

            if (feedback.DissolveSeconds > feedback.PlayerDeathDelaySeconds)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' dissolves for {feedback.DissolveSeconds} s, longer than the " +
                    $"player death delay ({feedback.PlayerDeathDelaySeconds} s). The run would end with the hero half dissolved.");

            _finishDelaySeconds = feedback.PlayerDeathDelaySeconds;
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);

                if (health.Current > 0f)
                    continue;

                _deads.Value.Add(entity);
                _diedEvents.Value.Add(entity);

                if (_players.Value.Has(entity))
                    StopPlayer(entity);
            }
        }

        private void StopPlayer(int entity)
        {
            if (_intents.Value.Has(entity))
            {
                ref MoveIntent intent = ref _intents.Value.Get(entity);
                intent.Value = Vector3.zero;
            }

            if (_dashRequests.Value.Has(entity))
                _dashRequests.Value.Del(entity);

            ref PendingFinish pending = ref _pendingFinishes.Value.Add(entity);
            pending.RemainingTicks = Mathf.RoundToInt(_finishDelaySeconds / _clock.Value.Delta);
        }
    }
}

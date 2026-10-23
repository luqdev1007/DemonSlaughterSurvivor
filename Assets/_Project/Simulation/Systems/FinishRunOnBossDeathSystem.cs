using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FinishRunOnBossDeathSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Boss, DiedEvent>> _dying = default;
        private readonly EcsFilterInject<Inc<PendingVictory>> _pending = default;
        private readonly EcsFilterInject<Inc<Player, Dead>> _deadHeroes = default;

        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<PendingVictory> _victories = default;

        private readonly EcsCustomInject<RunOutcome> _outcome = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IRunStatsSink> _stats = default;

        public void Init(IEcsSystems systems)
        {
            FeedbackConfig feedback = _level.Value.Feedback;

            if (feedback == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{_level.Value.Id}' has no {nameof(FeedbackConfig)} assigned.");

            if (float.IsNaN(feedback.VictoryBannerSeconds) || feedback.VictoryBannerSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has a victory banner of {feedback.VictoryBannerSeconds} s. Use 0 to leave the run on the tick of victory.");
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _pending.Value)
            {
                ref PendingVictory victory = ref _victories.Value.Get(entity);

                victory.RemainingTicks -= 1;

                if (victory.RemainingTicks > 0)
                    continue;

                if (_deadHeroes.Value.GetEntitiesCount() > 0)
                    continue;

                _outcome.Value.Finish(RunEndReason.Victory);
            }

            foreach (int entity in _dying.Value)
            {
                BossConfig boss = _bosses.Value.Get(entity).Config;
                float seconds = boss.DeathSeconds;

                if (_stats.Value != null)
                    _stats.Value.RecordBossKilled(boss.Id);

                int victory = _world.Value.NewEntity();

                _victories.Value.Add(victory).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / _clock.Value.Delta));
            }
        }
    }
}

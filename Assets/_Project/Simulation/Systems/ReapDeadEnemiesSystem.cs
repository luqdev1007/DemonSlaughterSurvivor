using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;

namespace Game.Simulation.Systems
{
    public sealed class ReapDeadEnemiesSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, Dead>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private float _deathSeconds;

        public void Init(IEcsSystems systems)
        {
            FeedbackConfig feedback = _level.Value.Feedback;

            if (feedback == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{_level.Value.Id}' has no {nameof(FeedbackConfig)} assigned.");

            if (feedback.EnemyDeathSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has a negative enemy death duration " +
                    $"({feedback.EnemyDeathSeconds}). Use 0 to return the view to the pool on the tick of death.");

            _deathSeconds = feedback.EnemyDeathSeconds;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (_views.Value.Has(entity))
                {
                    ref View view = ref _views.Value.Get(entity);

                    _viewFactory.Value.Retire(view.Value, _deathSeconds);

                    view.Value = null;
                }

                world.DelEntity(entity);
            }
        }
    }
}

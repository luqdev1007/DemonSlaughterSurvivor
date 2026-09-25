using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReapDeadEnemiesSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const float MinDirectionSqr = 1e-8f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Enemy, Dead>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<KillingBlow> _killingBlows = default;

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

                    _viewFactory.Value.Retire(view.Value, _deathSeconds, ResolveKnockbackDirection(entity));

                    view.Value = null;
                }

                world.DelEntity(entity);
            }
        }

        private Vector3 ResolveKnockbackDirection(int entity)
        {
            if (_killingBlows.Value.Has(entity) == false || _positions.Value.Has(entity) == false)
                return Vector3.zero;

            ref KillingBlow killingBlow = ref _killingBlows.Value.Get(entity);
            ref Position position = ref _positions.Value.Get(entity);

            Vector3 away = position.Value - killingBlow.SourcePosition;
            away.y = 0f;

            if (away.sqrMagnitude > MinDirectionSqr)
                return away.normalized;

            if (_facings.Value.Has(entity) == false)
                return Vector3.zero;

            ref Facing facing = ref _facings.Value.Get(entity);

            Vector3 backwards = -facing.Value;
            backwards.y = 0f;

            return backwards.sqrMagnitude > MinDirectionSqr ? backwards.normalized : Vector3.zero;
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PlayHitFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _events = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BerserkMode> _berserkModes = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                bool hasTarget = damageEvent.Target.Unpack(world, out int target);

                if (damageEvent.IsSlash && (hasTarget == false || _breakables.Value.Has(target) == false))
                    PlayWeaponHit(world, damageEvent.Source);

                if (hasTarget == false)
                    continue;

                if (_players.Value.Has(target))
                    continue;

                if (_views.Value.Has(target) == false)
                    continue;

                ref View view = ref _views.Value.Get(target);

                if (view.Value == null)
                    continue;

                view.Value.PlayHit(ResolveDirection(target, damageEvent.SourcePosition));

                if (IsHeavy(world, ref damageEvent) && view.Value is IHitPauseView pause)
                    pause.PlayHitPause();
            }
        }

        private bool IsHeavy(EcsWorld world, ref DamageEvent damageEvent)
        {
            if (damageEvent.IsCrit || damageEvent.SourcePerk is ExecutionConfig)
                return true;

            return damageEvent.Source.Unpack(world, out int source) && _berserkModes.Value.Has(source);
        }

        private void PlayWeaponHit(EcsWorld world, EcsPackedEntity source)
        {
            if (source.Unpack(world, out int wielder) == false || _views.Value.Has(wielder) == false)
                return;

            ref View view = ref _views.Value.Get(wielder);

            if (view.Value != null)
                view.Value.PlayWeaponHit();
        }

        private Vector3 ResolveDirection(int target, Vector3 sourcePosition)
        {
            if (_positions.Value.Has(target) == false)
                return Vector3.zero;

            Vector3 away = _positions.Value.Get(target).Value - sourcePosition;
            away.y = 0f;

            return away.sqrMagnitude > 1e-8f ? away.normalized : Vector3.zero;
        }
    }
}

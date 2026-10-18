using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SyncAimViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<TorsoAim, Facing, Velocity, View>> _filter = default;

        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                if (_views.Value.Get(entity).Value is IAimView view == false)
                    continue;

                ref TorsoAim aim = ref _aims.Value.Get(entity);
                Vector3 forward = _facings.Value.Get(entity).Value;
                forward.y = 0f;
                forward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;

                Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                Vector3 velocity = _velocities.Value.Get(entity).Value;

                Vector2 local = new Vector2(Vector3.Dot(velocity, right), Vector3.Dot(velocity, forward));
                float twist = Vector3.SignedAngle(forward, aim.Direction, Vector3.up);

                view.SetAim(twist, local, aim.TurnSide);
            }
        }
    }
}

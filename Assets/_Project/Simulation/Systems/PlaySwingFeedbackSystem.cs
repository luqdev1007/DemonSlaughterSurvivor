using Game.Configs;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlaySwingFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing, SwingStarted>> _started = default;

        private readonly EcsPoolInject<Swing> _swings = default;
        private readonly EcsPoolInject<Weapon> _weapons = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref Swing swing = ref _swings.Value.Get(entity);

                if (swing.Owner.Unpack(world, out int owner) == false || swing.Weapon.Unpack(world, out int weapon) == false)
                    continue;

                if (_views.Value.Has(owner) == false)
                    continue;

                ref View view = ref _views.Value.Get(owner);

                if (view.Value == null)
                    continue;

                SwingVariant variant = _weapons.Value.Get(weapon).Config.Variant(swing.Variant);

                view.Value.PlayAttack(variant.AnimatorTrigger, swing.PlaybackSpeed);
            }
        }
    }
}

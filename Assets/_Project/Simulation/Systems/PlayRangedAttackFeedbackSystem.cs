using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayRangedAttackFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<RangedAttack, RangedWindupStarted, View>> _started = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _started.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                ref RangedAttack attack = ref _attacks.Value.Get(entity);

                view.Value.PlayAttack(attack.Config.AnimatorTrigger, attack.PlaybackSpeed);
            }
        }
    }
}

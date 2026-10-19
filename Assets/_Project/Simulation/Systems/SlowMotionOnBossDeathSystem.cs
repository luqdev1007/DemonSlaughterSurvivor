using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SlowMotionOnBossDeathSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Enemy, Dead, Boss>> _bosses = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<ISlowMotionSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            if (_bosses.Value.GetEntitiesCount() == 0)
                return;

            FeedbackConfig feedback = _level.Value.Feedback;

            _sink.Value.RequestSlowMotion(feedback.BossDeathSlowMotionScale, feedback.BossDeathSlowMotionSeconds);
        }
    }
}

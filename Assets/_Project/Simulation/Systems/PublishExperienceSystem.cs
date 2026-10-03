using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PublishExperienceSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Experience>> _heroes = default;

        private readonly EcsPoolInject<Experience> _experiences = default;

        private readonly EcsCustomInject<IExperienceSink> _sink = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;

        private ExperienceConfig _config;

        public void Init(IEcsSystems systems)
        {
            _config = ExperienceRules.Resolve(_content.Value, _context.Value);
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _heroes.Value)
            {
                ref Experience experience = ref _experiences.Value.Get(hero);

                _sink.Value.Publish(experience.Level, experience.Current, _config.Required(experience.Level));
            }
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceLevelSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Experience>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Experience> _experiences = default;
        private readonly EcsPoolInject<LevelUpEvent> _events = default;

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

                int levels = 0;

                while (experience.Current >= _config.Required(experience.Level))
                {
                    experience.Current -= _config.Required(experience.Level);
                    experience.Level++;
                    levels++;
                }

                if (levels == 0)
                    continue;

                if (_events.Value.Has(hero) == false)
                    _events.Value.Add(hero);

                _events.Value.Get(hero).Count += levels;
            }
        }
    }
}

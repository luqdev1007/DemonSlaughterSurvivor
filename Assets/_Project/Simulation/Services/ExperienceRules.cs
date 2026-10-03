using Game.Configs;
using Game.Core;
using System;

namespace Game.Simulation.Services
{
    public static class ExperienceRules
    {
        public static ExperienceConfig Resolve(IContentRegistry content, RunContext context)
        {
            CharacterConfig character = content.Get<CharacterConfig>(context.CharacterId);
            ExperienceConfig config = character.Experience;

            if (config == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no {nameof(ExperienceConfig)} assigned, " +
                    "so the hero has no level curve and gems cannot fly.");

            if (config.Required(1) <= 0 || config.StepRequired < 0)
                throw new InvalidOperationException(
                    $"{nameof(ExperienceConfig)} '{config.Id}' needs {config.BaseRequired} + {config.StepRequired} x level. " +
                    "The first level must need more than zero and the step must not be negative, or one gem would raise levels forever.");

            if (config.FlightSeconds <= 0f || config.FlightPower <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(ExperienceConfig)} '{config.Id}' has flight {config.FlightSeconds} s with power {config.FlightPower}; both must be positive.");

            return config;
        }

        public static int FlightTicks(ExperienceConfig config, float tickSeconds)
        {
            return Math.Max(1, (int)Math.Round(config.FlightSeconds / tickSeconds));
        }
    }
}

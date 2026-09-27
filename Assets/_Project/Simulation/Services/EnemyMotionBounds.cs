using Game.Configs;
using System;

namespace Game.Simulation.Services
{
    public sealed class EnemyMotionBounds
    {
        public EnemyMotionBounds(float maxEnemyMoveSpeed, float maxOverlapCorrection, float pushSpeed)
        {
            if (maxEnemyMoveSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxEnemyMoveSpeed), maxEnemyMoveSpeed, "Enemy move speed ceiling must not be negative.");

            if (maxOverlapCorrection < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxOverlapCorrection), maxOverlapCorrection, "Overlap correction must not be negative.");

            if (pushSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(pushSpeed), pushSpeed, "Push speed must not be negative.");

            MaxEnemyMoveSpeed = maxEnemyMoveSpeed;
            MaxOverlapCorrection = maxOverlapCorrection;
            PushSpeed = pushSpeed;
        }

        public float MaxEnemyMoveSpeed { get; }

        public float MaxOverlapCorrection { get; }

        public float PushSpeed { get; }

        public static EnemyMotionBounds From(CharacterConfig character, WaveTimelineConfig waves)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            float maxOverlapCorrection = character.BodyRadius + WaveTimelineValidator.ResolveMaxBodyRadius(waves);
            float maxEnemyMoveSpeed = WaveTimelineValidator.ResolveMaxMoveSpeed(waves);
            float pushSpeed = character.Dash == null ? 0f : character.Dash.PushSpeed;

            return new EnemyMotionBounds(maxEnemyMoveSpeed, maxOverlapCorrection, pushSpeed);
        }

        public float CandidateSlack(float deltaTime)
        {
            float walkedAndCorrected = MaxEnemyMoveSpeed * deltaTime + MaxOverlapCorrection;
            float pushed = PushSpeed * deltaTime;

            return Math.Max(walkedAndCorrected, pushed);
        }
    }
}

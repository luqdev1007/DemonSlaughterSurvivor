using Game.Configs;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Services
{
    public sealed class EnemyMotionBounds
    {
        public EnemyMotionBounds(float maxEnemyMoveSpeed, float maxOverlapCorrection, float pushSpeed, float maxEnemyBodyRadius)
            : this(maxEnemyMoveSpeed, maxOverlapCorrection, pushSpeed, maxEnemyBodyRadius, 0f)
        {
        }

        public EnemyMotionBounds(float maxEnemyMoveSpeed, float maxOverlapCorrection, float pushSpeed, float maxEnemyBodyRadius, float maxEnemyRushSpeed)
        {
            if (maxEnemyMoveSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxEnemyMoveSpeed), maxEnemyMoveSpeed, "Enemy move speed ceiling must not be negative.");

            if (maxOverlapCorrection < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxOverlapCorrection), maxOverlapCorrection, "Overlap correction must not be negative.");

            if (maxEnemyBodyRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxEnemyBodyRadius), maxEnemyBodyRadius, "Enemy body radius must not be negative.");

            if (pushSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(pushSpeed), pushSpeed, "Push speed must not be negative.");

            if (maxEnemyRushSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxEnemyRushSpeed), maxEnemyRushSpeed, "Enemy rush speed must not be negative.");

            MaxEnemyMoveSpeed = maxEnemyMoveSpeed;
            MaxOverlapCorrection = maxOverlapCorrection;
            PushSpeed = pushSpeed;
            MaxEnemyBodyRadius = maxEnemyBodyRadius;
            MaxEnemyRushSpeed = maxEnemyRushSpeed;
        }

        public float MaxEnemyMoveSpeed { get; }

        public float MaxOverlapCorrection { get; }

        public float PushSpeed { get; }

        public float MaxEnemyBodyRadius { get; }

        public float MaxEnemyRushSpeed { get; }

        public static EnemyMotionBounds From(CharacterConfig character, WaveTimelineConfig waves, IReadOnlyList<PerkConfig> perks)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (perks == null)
                throw new ArgumentNullException(nameof(perks));

            float maxEnemyBodyRadius = WaveTimelineValidator.ResolveMaxBodyRadius(waves);
            float maxOverlapCorrection = character.BodyRadius + maxEnemyBodyRadius;
            float maxEnemyMoveSpeed = WaveTimelineValidator.ResolveMaxMoveSpeed(waves);
            float maxEnemyRushSpeed = WaveTimelineValidator.ResolveMaxRushSpeed(waves);
            float pushSpeed = character.Dash == null ? 0f : character.Dash.PushSpeed;

            if (character.StartingWeapon != null)
                pushSpeed = Math.Max(pushSpeed, character.StartingWeapon.HitPushSpeed);

            if (character.Backleap != null)
                pushSpeed = Math.Max(pushSpeed, character.Backleap.ImpactPushSpeed);

            if (character.Staff != null)
                pushSpeed = Math.Max(pushSpeed, character.Staff.ExplosionPushSpeed);

            for (int index = 0; index < perks.Count; index++)
            {
                if (perks[index] != null && perks[index].Behaviour != null)
                    pushSpeed = Math.Max(pushSpeed, perks[index].Behaviour.MaxPushSpeed);
            }

            return new EnemyMotionBounds(maxEnemyMoveSpeed, maxOverlapCorrection, pushSpeed, maxEnemyBodyRadius, maxEnemyRushSpeed);
        }

        public float CandidateSlack(float deltaTime)
        {
            float walkedAndCorrected = MaxEnemyMoveSpeed * deltaTime + MaxOverlapCorrection;
            float pushed = PushSpeed * deltaTime;
            float rushed = MaxEnemyRushSpeed * deltaTime;

            return Math.Max(walkedAndCorrected, Math.Max(pushed, rushed));
        }
    }
}

using Game.Configs;
using Game.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.View
{
    public sealed class ScreenShake : IScreenShake
    {
        private readonly CinemachineImpulseSource _source;
        private readonly FeedbackConfig _feedback;

        private float _nextHeroHitTime;

        public ScreenShake(LevelConfig level, CameraService camera)
        {
            _feedback = level.Feedback;
            _source = camera.ImpulseSource;
        }

        public float Multiplier { get; set; } = 1f;

        public void Shake(ShakeKind kind)
        {
            float force = ForceOf(kind) * _feedback.ShakeMultiplier * Multiplier;

            if (force <= 0f)
                return;

            if (kind == ShakeKind.HeroHit)
            {
                if (Time.time < _nextHeroHitTime)
                    return;

                _nextHeroHitTime = Time.time + _feedback.ShakeHeroHitCooldownSeconds;
            }

            _source.GenerateImpulseWithForce(force);
        }

        private float ForceOf(ShakeKind kind)
        {
            switch (kind)
            {
                case ShakeKind.HeroHit:
                    return _feedback.ShakeHeroHit;
                case ShakeKind.Landing:
                    return _feedback.ShakeLanding;
                case ShakeKind.Ultimate:
                    return _feedback.ShakeUltimate;
                case ShakeKind.EliteDeath:
                    return _feedback.ShakeEliteDeath;
                case ShakeKind.BossDeath:
                    return _feedback.ShakeBossDeath;
                case ShakeKind.BossSlam:
                    return _feedback.ShakeBossSlam;
                default:
                    return 0f;
            }
        }
    }
}

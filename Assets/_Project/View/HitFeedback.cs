using UnityEngine;

namespace Game.View
{
    internal readonly struct HitFeedback
    {
        public readonly Material FlashMaterial;
        public readonly float FlashSeconds;
        public readonly float FlashCooldownSeconds;
        public readonly float RecoilDistance;
        public readonly float Squash;
        public readonly float PulseSeconds;
        public readonly float PulseCooldownSeconds;
        public readonly float PauseSeconds;

        public HitFeedback(
            Material flashMaterial,
            float flashSeconds,
            float flashCooldownSeconds,
            float recoilDistance,
            float squash,
            float pulseSeconds,
            float pulseCooldownSeconds,
            float pauseSeconds)
        {
            FlashMaterial = flashMaterial;
            FlashSeconds = flashSeconds;
            FlashCooldownSeconds = flashCooldownSeconds;
            RecoilDistance = recoilDistance;
            Squash = squash;
            PulseSeconds = pulseSeconds;
            PulseCooldownSeconds = pulseCooldownSeconds;
            PauseSeconds = pauseSeconds;
        }
    }
}

using UnityEngine;

namespace Game.View
{
    internal readonly struct HitFeedback
    {
        public readonly Material FlashMaterial;
        public readonly float FlashSeconds;
        public readonly float FlashCooldownSeconds;

        public HitFeedback(Material flashMaterial, float flashSeconds, float flashCooldownSeconds)
        {
            FlashMaterial = flashMaterial;
            FlashSeconds = flashSeconds;
            FlashCooldownSeconds = flashCooldownSeconds;
        }
    }
}

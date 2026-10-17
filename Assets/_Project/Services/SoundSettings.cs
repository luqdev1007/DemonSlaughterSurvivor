using Game.Core;
using UnityEngine;

namespace Game.Services
{
    public sealed class SoundSettings : ISoundSettings
    {
        public SoundSettings()
        {
            MasterVolume = 1f;
            Apply();
        }

        public float MasterVolume { get; private set; }

        public bool IsMuted { get; private set; }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            Apply();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            Apply();
        }

        private void Apply()
        {
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
        }
    }
}

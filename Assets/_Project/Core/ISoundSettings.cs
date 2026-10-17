namespace Game.Core
{
    public interface ISoundSettings
    {
        float MasterVolume { get; }

        bool IsMuted { get; }

        void SetMasterVolume(float volume);

        void SetMuted(bool muted);
    }
}

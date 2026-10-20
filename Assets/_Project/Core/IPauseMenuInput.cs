namespace Game.Core
{
    public interface IPauseMenuInput
    {
        bool ConsumeResumeRequested();

        bool ConsumeExitRequested();
    }
}

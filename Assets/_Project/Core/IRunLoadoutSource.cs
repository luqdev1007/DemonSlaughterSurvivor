namespace Game.Core
{
    public interface IRunLoadoutSource
    {
        RunLoadout Build(string characterId);
    }
}

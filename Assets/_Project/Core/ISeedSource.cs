namespace Game.Core
{
    /// <summary>
    /// Source of run seeds. Lives on the project scope: a run receives its seed, it does not invent one.
    /// </summary>
    public interface ISeedSource
    {
        int Next();
    }
}

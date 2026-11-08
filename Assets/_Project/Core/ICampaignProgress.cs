using System.Collections.Generic;

namespace Game.Core
{
    public interface ICampaignProgress
    {
        IReadOnlyList<string> CompletedLevelIds { get; }
        bool IsCompleted(string levelId);
        bool IsAvailable(string levelId);
    }
}

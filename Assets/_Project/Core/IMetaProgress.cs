using System;

namespace Game.Core
{
    public interface IMetaProgress
    {
        event Action Changed;

        int MemoryShards { get; }
        int MagicShards { get; }
        int RelicCount { get; }

        int GlobalRank(string passiveId);
        int TotalGlobalRanks { get; }
        bool IsParagonUnlocked { get; }
        int ParagonRank(string passiveId);
        MetaOffer GlobalRankOffer(string passiveId);
        MetaStatus BuyGlobalRank(string passiveId);
        MetaOffer ParagonRankOffer(string passiveId);
        MetaStatus BuyParagonRank(string passiveId);

        int NodeRank(string characterId, string nodeId);
        MetaOffer NodeOffer(string characterId, string nodeId);
        MetaStatus BuyNode(string characterId, string nodeId);
        int TreeSpent(string characterId);
        int ResetTree(string characterId);

        string EquippedWeapon(string characterId);
        bool IsForged(string weaponId);
        int WeaponLevel(string weaponId);
        MetaOffer ForgeOffer(string weaponId);
        MetaStatus ForgeOrEquip(string characterId, string weaponId);
        MetaOffer WeaponUpgradeOffer(string weaponId);
        MetaStatus UpgradeWeapon(string weaponId);

        string SocketContent(string weaponId, string socketId);
        int GemCount(string gemId);
        MetaStatus InsertGem(string weaponId, string socketId, string gemId);
        MetaStatus ClearSocket(string weaponId, string socketId);

        bool OwnsRelic(string relicId);
        int RelicLevel(string relicId);
        bool TryGetRelicPlacement(string relicId, out string weaponId, out string socketId);
        MetaStatus InsertRelic(string weaponId, string socketId, string relicId);
        MetaOffer RelicUpgradeOffer(string relicId);
        MetaStatus UpgradeRelic(string relicId);

        bool IsBossDefeated(string bossId);

        void DebugAddCurrency(int memoryShards, int magicShards);
        void DebugGrantGems();
        void DebugGrantRelics();
        void DebugReset();
    }
}

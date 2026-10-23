using System;
using System.Collections.Generic;

namespace Game.Meta
{
    [Serializable]
    public sealed class MetaSaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int MemoryShards;
        public int MagicShards;
        public List<RankRecord> Global = new List<RankRecord>();
        public List<RankRecord> Paragon = new List<RankRecord>();
        public List<TreeRecord> Trees = new List<TreeRecord>();
        public List<WeaponRecord> Weapons = new List<WeaponRecord>();
        public List<EquipRecord> Equipped = new List<EquipRecord>();
        public List<RankRecord> Gems = new List<RankRecord>();
        public List<RelicRecord> Relics = new List<RelicRecord>();
        public List<string> DefeatedBosses = new List<string>();
    }

    [Serializable]
    public sealed class RankRecord
    {
        public string Id;
        public int Rank;
    }

    [Serializable]
    public sealed class TreeRecord
    {
        public string CharacterId;
        public List<RankRecord> Nodes = new List<RankRecord>();
    }

    [Serializable]
    public sealed class WeaponRecord
    {
        public string Id;
        public int Level = 1;
        public List<SocketRecord> Sockets = new List<SocketRecord>();
    }

    [Serializable]
    public sealed class SocketRecord
    {
        public string SocketId;
        public string GemId;
    }

    [Serializable]
    public sealed class EquipRecord
    {
        public string CharacterId;
        public string WeaponId;
    }

    [Serializable]
    public sealed class RelicRecord
    {
        public string Id;
        public int Level = 1;
        public string WeaponId;
        public string SocketId;
    }
}

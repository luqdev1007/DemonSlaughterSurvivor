using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Meta
{
    public sealed class MetaProgress : IMetaProgress
    {
        public const string FileName = "meta.json";
        public const string CorruptFileName = "meta.corrupt.json";

        private const int DebugGemCount = 1;

        private readonly IContentRegistry _content;
        private readonly ISaveStorage _storage;
        private readonly GlobalPowerConfig _global;

        private MetaSaveData _data;

        public MetaProgress(IContentRegistry content, ISaveStorage storage)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            MetaContentValidator.Validate(content);

            IReadOnlyList<GlobalPowerConfig> globals = content.All<GlobalPowerConfig>();
            _global = globals.Count > 0 ? globals[0] : null;

            _data = Load();
        }

        public event Action Changed;

        internal MetaSaveData Data => _data;

        public int MemoryShards => _data.MemoryShards;

        public int MagicShards => _data.MagicShards;

        public int RelicCount => _data.Relics.Count;

        public int GlobalRank(string passiveId)
        {
            return RankOf(_data.Global, passiveId);
        }

        public int TotalGlobalRanks
        {
            get
            {
                int total = 0;

                foreach (RankRecord record in _data.Global)
                    total += record.Rank;

                return total;
            }
        }

        public bool IsParagonUnlocked => _global != null && TotalGlobalRanks >= _global.ParagonUnlockRanks;

        public int ParagonRank(string passiveId)
        {
            return RankOf(_data.Paragon, passiveId);
        }

        public MetaOffer GlobalRankOffer(string passiveId)
        {
            if (_global == null || _global.FindPassive(passiveId) == null)
                return new MetaOffer(MetaStatus.Invalid, 0);

            int rank = GlobalRank(passiveId);

            if (rank >= _global.MaxRank)
                return new MetaOffer(MetaStatus.Maxed, 0);

            return Priced(_global.RankCost(rank), _data.MemoryShards);
        }

        public MetaStatus BuyGlobalRank(string passiveId)
        {
            MetaOffer offer = GlobalRankOffer(passiveId);

            if (offer.CanBuy == false)
                return offer.Status;

            _data.MemoryShards -= offer.Cost;
            SetRank(_data.Global, passiveId, GlobalRank(passiveId) + 1);

            Commit();

            return MetaStatus.Available;
        }

        public MetaOffer ParagonRankOffer(string passiveId)
        {
            if (_global == null || _global.FindParagon(passiveId) == null)
                return new MetaOffer(MetaStatus.Invalid, 0);

            int cost = _global.ParagonCost(ParagonRank(passiveId));

            if (IsParagonUnlocked == false)
                return new MetaOffer(MetaStatus.GateUnmet, cost);

            return Priced(cost, _data.MemoryShards);
        }

        public MetaStatus BuyParagonRank(string passiveId)
        {
            MetaOffer offer = ParagonRankOffer(passiveId);

            if (offer.CanBuy == false)
                return offer.Status;

            _data.MemoryShards -= offer.Cost;
            SetRank(_data.Paragon, passiveId, ParagonRank(passiveId) + 1);

            Commit();

            return MetaStatus.Available;
        }

        public int NodeRank(string characterId, string nodeId)
        {
            HeroTreeConfig tree = TreeFor(characterId);

            if (tree == null)
                return 0;

            HeroTreeNode node = tree.Find(nodeId);

            if (node == null)
                return 0;

            if (node.Type == HeroTreeNodeType.Start)
                return node.MaxRank;

            TreeRecord record = FindTree(characterId, false);

            return record == null ? 0 : RankOf(record.Nodes, nodeId);
        }

        public MetaOffer NodeOffer(string characterId, string nodeId)
        {
            HeroTreeConfig tree = TreeFor(characterId);
            HeroTreeNode node = tree?.Find(nodeId);

            if (node == null)
                return new MetaOffer(MetaStatus.Invalid, 0);

            int rank = NodeRank(characterId, nodeId);

            if (rank >= node.MaxRank)
                return new MetaOffer(MetaStatus.Maxed, 0);

            int cost = node.Cost(rank);

            if (string.IsNullOrEmpty(node.Excludes) == false && NodeRank(characterId, node.Excludes) > 0)
                return new MetaOffer(MetaStatus.Excluded, cost);

            if (node.Gates.Count > 0)
            {
                if (OpenGates(characterId, node) < node.GatesRequired)
                    return new MetaOffer(MetaStatus.GateUnmet, cost);
            }
            else
            {
                foreach (string parent in node.Parents)
                {
                    if (NodeRank(characterId, parent) <= 0)
                        return new MetaOffer(MetaStatus.NeedsParents, cost);
                }
            }

            return Priced(cost, _data.MemoryShards);
        }

        public int OpenGates(string characterId, HeroTreeNode node)
        {
            int open = 0;

            foreach (HeroTreeGate gate in node.Gates)
            {
                foreach (string member in gate.AnyOf)
                {
                    if (NodeRank(characterId, member) > 0)
                    {
                        open++;

                        break;
                    }
                }
            }

            return open;
        }

        public MetaStatus BuyNode(string characterId, string nodeId)
        {
            MetaOffer offer = NodeOffer(characterId, nodeId);

            if (offer.CanBuy == false)
                return offer.Status;

            _data.MemoryShards -= offer.Cost;

            TreeRecord record = FindTree(characterId, true);
            SetRank(record.Nodes, nodeId, RankOf(record.Nodes, nodeId) + 1);

            Commit();

            return MetaStatus.Available;
        }

        public int TreeSpent(string characterId)
        {
            HeroTreeConfig tree = TreeFor(characterId);
            TreeRecord record = FindTree(characterId, false);

            if (tree == null || record == null)
                return 0;

            int spent = 0;

            foreach (RankRecord rank in record.Nodes)
            {
                HeroTreeNode node = tree.Find(rank.Id);

                if (node == null)
                    continue;

                for (int step = 0; step < rank.Rank; step++)
                    spent += node.Cost(step);
            }

            return spent;
        }

        public int ResetTree(string characterId)
        {
            int refund = TreeSpent(characterId);

            TreeRecord record = FindTree(characterId, false);

            if (record == null)
                return 0;

            record.Nodes.Clear();
            _data.MemoryShards += refund;

            Commit();

            return refund;
        }

        public string EquippedWeapon(string characterId)
        {
            foreach (EquipRecord record in _data.Equipped)
            {
                if (record.CharacterId == characterId && IsForged(record.WeaponId))
                    return record.WeaponId;
            }

            UniqueWeaponConfig starting = StartingWeapon(characterId);

            return starting == null ? null : starting.Id;
        }

        public bool IsForged(string weaponId)
        {
            return FindWeapon(weaponId) != null;
        }

        public int WeaponLevel(string weaponId)
        {
            WeaponRecord record = FindWeapon(weaponId);

            return record == null ? 0 : record.Level;
        }

        public MetaOffer ForgeOffer(string weaponId)
        {
            if (_content.TryGet(weaponId, out UniqueWeaponConfig weapon) == false)
                return new MetaOffer(MetaStatus.Invalid, 0);

            if (weapon.ComingLater)
                return new MetaOffer(MetaStatus.Unavailable, 0);

            if (IsForged(weaponId))
                return new MetaOffer(MetaStatus.Maxed, 0);

            return Priced(weapon.ForgeCost, _data.MagicShards);
        }

        public MetaStatus ForgeOrEquip(string characterId, string weaponId)
        {
            if (_content.TryGet(weaponId, out UniqueWeaponConfig weapon) == false || weapon.Character == null || weapon.Character.Id != characterId)
                return MetaStatus.Invalid;

            if (IsForged(weaponId) == false)
            {
                MetaOffer offer = ForgeOffer(weaponId);

                if (offer.CanBuy == false)
                    return offer.Status;

                _data.MagicShards -= offer.Cost;
                _data.Weapons.Add(new WeaponRecord { Id = weaponId, Level = 1 });
            }

            SetEquipped(characterId, weaponId);

            Commit();

            return MetaStatus.Available;
        }

        public MetaOffer WeaponUpgradeOffer(string weaponId)
        {
            WeaponRecord record = FindWeapon(weaponId);

            if (record == null || _content.TryGet(weaponId, out UniqueWeaponConfig weapon) == false)
                return new MetaOffer(MetaStatus.NotForged, 0);

            if (record.Level >= weapon.MaxLevel)
                return new MetaOffer(MetaStatus.Maxed, 0);

            return Priced(weapon.UpgradeCost(record.Level), _data.MagicShards);
        }

        public MetaStatus UpgradeWeapon(string weaponId)
        {
            MetaOffer offer = WeaponUpgradeOffer(weaponId);

            if (offer.CanBuy == false)
                return offer.Status;

            _data.MagicShards -= offer.Cost;
            FindWeapon(weaponId).Level++;

            Commit();

            return MetaStatus.Available;
        }

        public string SocketContent(string weaponId, string socketId)
        {
            foreach (RelicRecord relic in _data.Relics)
            {
                if (relic.WeaponId == weaponId && relic.SocketId == socketId)
                    return relic.Id;
            }

            WeaponRecord record = FindWeapon(weaponId);

            if (record == null)
                return null;

            foreach (SocketRecord socket in record.Sockets)
            {
                if (socket.SocketId == socketId)
                    return socket.GemId;
            }

            return null;
        }

        public int GemCount(string gemId)
        {
            return RankOf(_data.Gems, gemId);
        }

        public MetaStatus InsertGem(string weaponId, string socketId, string gemId)
        {
            MetaStatus status = CheckSocket(weaponId, socketId, out UniqueWeaponConfig _, out WeaponSocket socket);

            if (status != MetaStatus.Available)
                return status;

            if (_content.TryGet(gemId, out SocketGemConfig gem) == false)
                return MetaStatus.Invalid;

            if (socket.Accepts(gem.Color) == false)
                return MetaStatus.WrongColor;

            if (GemCount(gemId) <= 0)
                return MetaStatus.NotOwned;

            RemoveGem(weaponId, socketId);

            SetRank(_data.Gems, gemId, GemCount(gemId) - 1);
            FindWeapon(weaponId).Sockets.Add(new SocketRecord { SocketId = socketId, GemId = gemId });

            Commit();

            return MetaStatus.Available;
        }

        public MetaStatus ClearSocket(string weaponId, string socketId)
        {
            foreach (RelicRecord relic in _data.Relics)
            {
                if (relic.WeaponId == weaponId && relic.SocketId == socketId)
                {
                    relic.WeaponId = null;
                    relic.SocketId = null;

                    Commit();

                    return MetaStatus.Available;
                }
            }

            if (RemoveGem(weaponId, socketId) == false)
                return MetaStatus.Invalid;

            Commit();

            return MetaStatus.Available;
        }

        public bool OwnsRelic(string relicId)
        {
            return FindRelic(relicId) != null;
        }

        public int RelicLevel(string relicId)
        {
            RelicRecord record = FindRelic(relicId);

            return record == null ? 0 : record.Level;
        }

        public bool TryGetRelicPlacement(string relicId, out string weaponId, out string socketId)
        {
            RelicRecord record = FindRelic(relicId);

            weaponId = record?.WeaponId;
            socketId = record?.SocketId;

            return string.IsNullOrEmpty(weaponId) == false;
        }

        public MetaStatus InsertRelic(string weaponId, string socketId, string relicId)
        {
            MetaStatus status = CheckSocket(weaponId, socketId, out UniqueWeaponConfig _, out WeaponSocket socket);

            if (status != MetaStatus.Available)
                return status;

            if (socket.Kind != SocketKind.Relic)
                return MetaStatus.WrongColor;

            RelicRecord record = FindRelic(relicId);

            if (record == null)
                return MetaStatus.NotOwned;

            foreach (RelicRecord other in _data.Relics)
            {
                if (other.WeaponId == weaponId && other.SocketId == socketId)
                {
                    other.WeaponId = null;
                    other.SocketId = null;
                }
            }

            record.WeaponId = weaponId;
            record.SocketId = socketId;

            Commit();

            return MetaStatus.Available;
        }

        public MetaOffer RelicUpgradeOffer(string relicId)
        {
            RelicRecord record = FindRelic(relicId);

            if (record == null || _content.TryGet(relicId, out RelicConfig relic) == false)
                return new MetaOffer(MetaStatus.NotOwned, 0);

            if (record.Level >= relic.MaxLevel)
                return new MetaOffer(MetaStatus.Maxed, 0);

            return Priced(relic.UpgradeCost(record.Level), _data.MagicShards);
        }

        public MetaStatus UpgradeRelic(string relicId)
        {
            MetaOffer offer = RelicUpgradeOffer(relicId);

            if (offer.CanBuy == false)
                return offer.Status;

            _data.MagicShards -= offer.Cost;
            FindRelic(relicId).Level++;

            Commit();

            return MetaStatus.Available;
        }

        public bool IsBossDefeated(string bossId)
        {
            return _data.DefeatedBosses.Contains(bossId);
        }

        public void DebugAddCurrency(int memoryShards, int magicShards)
        {
            _data.MemoryShards += memoryShards;
            _data.MagicShards += magicShards;

            Commit();
        }

        public void DebugGrantGems()
        {
            foreach (SocketGemConfig gem in _content.All<SocketGemConfig>())
                SetRank(_data.Gems, gem.Id, GemCount(gem.Id) + DebugGemCount);

            Commit();
        }

        public void DebugGrantRelics()
        {
            foreach (RelicConfig relic in _content.All<RelicConfig>())
            {
                if (OwnsRelic(relic.Id) == false)
                    _data.Relics.Add(new RelicRecord { Id = relic.Id, Level = 1 });
            }

            Commit();
        }

        public void DebugReset()
        {
            _storage.Delete(FileName);
            _data = Fresh();

            Commit();
        }

        internal void GrantRewards(RunRewards rewards, IReadOnlyList<string> bossesKilled)
        {
            _data.MemoryShards += rewards.MemoryShards;
            _data.MagicShards += rewards.MagicShards;

            if (rewards.RelicGranted)
                _data.Relics.Add(new RelicRecord { Id = rewards.RelicId, Level = 1 });

            foreach (string bossId in bossesKilled)
            {
                if (string.IsNullOrEmpty(bossId) == false && _data.DefeatedBosses.Contains(bossId) == false)
                    _data.DefeatedBosses.Add(bossId);
            }

            Commit();
        }

        internal HeroTreeConfig TreeFor(string characterId)
        {
            foreach (HeroTreeConfig tree in _content.All<HeroTreeConfig>())
            {
                if (tree.Character != null && tree.Character.Id == characterId)
                    return tree;
            }

            return null;
        }

        private UniqueWeaponConfig StartingWeapon(string characterId)
        {
            foreach (UniqueWeaponConfig weapon in _content.All<UniqueWeaponConfig>())
            {
                if (weapon.IsStarting && weapon.Character != null && weapon.Character.Id == characterId)
                    return weapon;
            }

            return null;
        }

        private MetaStatus CheckSocket(string weaponId, string socketId, out UniqueWeaponConfig weapon, out WeaponSocket socket)
        {
            socket = null;

            if (_content.TryGet(weaponId, out weapon) == false)
                return MetaStatus.Invalid;

            socket = weapon.FindSocket(socketId);

            if (socket == null)
                return MetaStatus.Invalid;

            WeaponRecord record = FindWeapon(weaponId);

            if (record == null)
                return MetaStatus.NotForged;

            if (record.Level < socket.UnlockLevel)
                return MetaStatus.SocketLocked;

            return MetaStatus.Available;
        }

        private bool RemoveGem(string weaponId, string socketId)
        {
            WeaponRecord record = FindWeapon(weaponId);

            if (record == null)
                return false;

            for (int index = 0; index < record.Sockets.Count; index++)
            {
                SocketRecord socket = record.Sockets[index];

                if (socket.SocketId != socketId)
                    continue;

                if (string.IsNullOrEmpty(socket.GemId) == false)
                    SetRank(_data.Gems, socket.GemId, GemCount(socket.GemId) + 1);

                record.Sockets.RemoveAt(index);

                return true;
            }

            return false;
        }

        private void SetEquipped(string characterId, string weaponId)
        {
            foreach (EquipRecord record in _data.Equipped)
            {
                if (record.CharacterId == characterId)
                {
                    record.WeaponId = weaponId;

                    return;
                }
            }

            _data.Equipped.Add(new EquipRecord { CharacterId = characterId, WeaponId = weaponId });
        }

        private WeaponRecord FindWeapon(string weaponId)
        {
            foreach (WeaponRecord record in _data.Weapons)
            {
                if (record.Id == weaponId)
                    return record;
            }

            return null;
        }

        private RelicRecord FindRelic(string relicId)
        {
            foreach (RelicRecord record in _data.Relics)
            {
                if (record.Id == relicId)
                    return record;
            }

            return null;
        }

        private TreeRecord FindTree(string characterId, bool create)
        {
            foreach (TreeRecord record in _data.Trees)
            {
                if (record.CharacterId == characterId)
                    return record;
            }

            if (create == false)
                return null;

            TreeRecord created = new TreeRecord { CharacterId = characterId };
            _data.Trees.Add(created);

            return created;
        }

        private static MetaOffer Priced(int cost, int wallet)
        {
            return new MetaOffer(wallet >= cost ? MetaStatus.Available : MetaStatus.NotEnoughCurrency, cost);
        }

        private static int RankOf(List<RankRecord> records, string id)
        {
            foreach (RankRecord record in records)
            {
                if (record.Id == id)
                    return record.Rank;
            }

            return 0;
        }

        private static void SetRank(List<RankRecord> records, string id, int rank)
        {
            foreach (RankRecord record in records)
            {
                if (record.Id == id)
                {
                    record.Rank = rank;

                    return;
                }
            }

            records.Add(new RankRecord { Id = id, Rank = rank });
        }

        private void Commit()
        {
            Save();

            Changed?.Invoke();
        }

        private void Save()
        {
            try
            {
                _storage.Write(FileName, JsonUtility.ToJson(_data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Meta progress was not saved: {exception.Message}");
            }
        }

        private MetaSaveData Load()
        {
            string json;

            try
            {
                if (_storage.TryRead(FileName, out json) == false)
                    return Fresh();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Meta progress could not be read, a fresh one is used: {exception.Message}");

                return Fresh();
            }

            MetaSaveData data;

            try
            {
                data = JsonUtility.FromJson<MetaSaveData>(json);
            }
            catch (Exception exception)
            {
                data = null;
                Debug.LogWarning($"Meta progress is corrupt ({exception.Message}); a copy is kept in {CorruptFileName}, a fresh progress is used.");
            }

            if (data == null)
            {
                KeepCorruptCopy(json);

                return Fresh();
            }

            if (data.Version > MetaSaveData.CurrentVersion)
            {
                Debug.LogWarning($"Meta progress has version {data.Version}, newer than {MetaSaveData.CurrentVersion}; a copy is kept in {CorruptFileName}, a fresh progress is used.");
                KeepCorruptCopy(json);

                return Fresh();
            }

            MetaSaveSanitizer.Sanitize(data, _content, _global);
            EnsureStartingWeapons(data);

            return data;
        }

        private void KeepCorruptCopy(string json)
        {
            try
            {
                _storage.Write(CorruptFileName, json ?? string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"The corrupt meta progress copy was not written: {exception.Message}");
            }
        }

        private MetaSaveData Fresh()
        {
            MetaSaveData data = new MetaSaveData();
            EnsureStartingWeapons(data);

            return data;
        }

        private void EnsureStartingWeapons(MetaSaveData data)
        {
            foreach (UniqueWeaponConfig weapon in _content.All<UniqueWeaponConfig>())
            {
                if (weapon.IsStarting == false)
                    continue;

                bool owned = false;

                foreach (WeaponRecord record in data.Weapons)
                    owned |= record.Id == weapon.Id;

                if (owned == false)
                    data.Weapons.Add(new WeaponRecord { Id = weapon.Id, Level = 1 });
            }
        }
    }
}

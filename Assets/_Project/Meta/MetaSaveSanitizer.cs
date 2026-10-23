using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Meta
{
    public static class MetaSaveSanitizer
    {
        public static void Sanitize(MetaSaveData data, IContentRegistry content, GlobalPowerConfig global)
        {
            data.Global ??= new List<RankRecord>();
            data.Paragon ??= new List<RankRecord>();
            data.Trees ??= new List<TreeRecord>();
            data.Weapons ??= new List<WeaponRecord>();
            data.Equipped ??= new List<EquipRecord>();
            data.Gems ??= new List<RankRecord>();
            data.Relics ??= new List<RelicRecord>();
            data.DefeatedBosses ??= new List<string>();

            data.MemoryShards = Mathf.Max(0, data.MemoryShards);
            data.MagicShards = Mathf.Max(0, data.MagicShards);

            data.Global.RemoveAll(record => Drop(record == null || global == null || global.FindPassive(record.Id) == null, "global passive", record?.Id));
            data.Paragon.RemoveAll(record => Drop(record == null || global == null || global.FindParagon(record.Id) == null, "paragon passive", record?.Id));

            foreach (RankRecord record in data.Global)
                record.Rank = Mathf.Clamp(record.Rank, 0, global.MaxRank);

            foreach (RankRecord record in data.Paragon)
                record.Rank = Mathf.Max(0, record.Rank);

            SanitizeTrees(data, content);
            SanitizeWeapons(data, content);

            data.Gems.RemoveAll(record => Drop(record == null || content.TryGet(record.Id, out SocketGemConfig _) == false, "gem", record?.Id));

            foreach (RankRecord record in data.Gems)
                record.Rank = Mathf.Max(0, record.Rank);

            SanitizeRelics(data, content);

            data.DefeatedBosses.RemoveAll(id => Drop(content.TryGet(id, out BossConfig _) == false, "boss", id));
            data.Equipped.RemoveAll(record => Drop(record == null || content.TryGet(record.WeaponId, out UniqueWeaponConfig _) == false, "equipped weapon", record?.WeaponId));
        }

        private static void SanitizeTrees(MetaSaveData data, IContentRegistry content)
        {
            data.Trees.RemoveAll(record => Drop(record == null || FindTree(content, record.CharacterId) == null, "hero tree of", record?.CharacterId));

            foreach (TreeRecord record in data.Trees)
            {
                HeroTreeConfig tree = FindTree(content, record.CharacterId);

                record.Nodes ??= new List<RankRecord>();
                record.Nodes.RemoveAll(node => Drop(node == null || tree.Find(node.Id) == null, "tree node", node?.Id));

                foreach (RankRecord node in record.Nodes)
                    node.Rank = Mathf.Clamp(node.Rank, 0, tree.Find(node.Id).MaxRank);
            }
        }

        private static void SanitizeWeapons(MetaSaveData data, IContentRegistry content)
        {
            data.Weapons.RemoveAll(record => Drop(record == null || content.TryGet(record.Id, out UniqueWeaponConfig _) == false, "weapon", record?.Id));

            List<RankRecord> refunds = new List<RankRecord>();

            foreach (WeaponRecord record in data.Weapons)
            {
                UniqueWeaponConfig weapon = content.Get<UniqueWeaponConfig>(record.Id);

                record.Level = Mathf.Clamp(record.Level, 1, weapon.MaxLevel);
                record.Sockets ??= new List<SocketRecord>();

                record.Sockets.RemoveAll(socket =>
                {
                    if (Drop(socket == null || weapon.FindSocket(socket.SocketId) == null, "socket", socket?.SocketId))
                        return true;

                    if (Drop(content.TryGet(socket.GemId, out SocketGemConfig gem) == false, "socketed gem", socket.GemId))
                        return true;

                    if (weapon.FindSocket(socket.SocketId).Accepts(gem.Color))
                        return false;

                    Debug.LogWarning($"Meta save: gem '{socket.GemId}' does not fit socket '{socket.SocketId}' of '{record.Id}', it returns to the inventory.");
                    refunds.Add(new RankRecord { Id = socket.GemId, Rank = 1 });

                    return true;
                });
            }

            foreach (RankRecord refund in refunds)
                data.Gems.Add(refund);
        }

        private static void SanitizeRelics(MetaSaveData data, IContentRegistry content)
        {
            data.Relics.RemoveAll(record => Drop(record == null || content.TryGet(record.Id, out RelicConfig _) == false, "relic", record?.Id));

            foreach (RelicRecord record in data.Relics)
            {
                RelicConfig relic = content.Get<RelicConfig>(record.Id);
                record.Level = Mathf.Clamp(record.Level, 1, relic.MaxLevel);

                if (string.IsNullOrEmpty(record.WeaponId))
                    continue;

                bool valid = content.TryGet(record.WeaponId, out UniqueWeaponConfig weapon)
                    && weapon.FindSocket(record.SocketId) is WeaponSocket socket
                    && socket.Kind == SocketKind.Relic;

                if (valid)
                    continue;

                Debug.LogWarning($"Meta save: relic '{record.Id}' sat in an unknown socket '{record.WeaponId}/{record.SocketId}', it is taken out.");
                record.WeaponId = null;
                record.SocketId = null;
            }
        }

        private static HeroTreeConfig FindTree(IContentRegistry content, string characterId)
        {
            foreach (HeroTreeConfig tree in content.All<HeroTreeConfig>())
            {
                if (tree.Character != null && tree.Character.Id == characterId)
                    return tree;
            }

            return null;
        }

        private static bool Drop(bool drop, string kind, string id)
        {
            if (drop)
                Debug.LogWarning($"Meta save: unknown {kind} '{id}' is skipped.");

            return drop;
        }
    }
}

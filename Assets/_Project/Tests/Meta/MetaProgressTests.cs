using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;

namespace Game.Meta.Tests
{
    public sealed class MetaProgressTests
    {
        private const string DatabasePath = "Assets/_Project/Configs/ContentDatabase.asset";
        private const string Hero = "character.berserk";
        private const string Blade = "dh_weapon_blade";
        private const string Axe = "dh_weapon_axe";
        private const string Relic = "relic_knight_banner";

        private ContentRegistry _content;
        private MemoryStorage _storage;
        private MetaProgress _meta;

        [SetUp]
        public void SetUp()
        {
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            _content = new ContentRegistry(database.Entries);
            _storage = new MemoryStorage();
            _meta = new MetaProgress(_content, _storage);
        }

        [Test]
        public void FreshProgressOwnsOnlyTheStartingBlade()
        {
            Assert.AreEqual(0, _meta.MemoryShards);
            Assert.AreEqual(0, _meta.MagicShards);
            Assert.IsTrue(_meta.IsForged(Blade));
            Assert.IsFalse(_meta.IsForged(Axe));
            Assert.AreEqual(Blade, _meta.EquippedWeapon(Hero));
            Assert.AreEqual(MetaStatus.Unavailable, _meta.ForgeOffer("dh_weapon_scythe").Status);
        }

        [Test]
        public void KeystonesExcludeEachOtherAndResetRefundsEverything()
        {
            _meta.DebugAddCurrency(5000, 0);

            BuyToEnd("dh_boiling_blood", 3);
            BuyToEnd("dh_lasting_rage", 3);
            Assert.AreEqual(MetaStatus.Available, _meta.BuyNode(Hero, "dh_endless_whirl"));
            Assert.AreEqual(MetaStatus.Available, _meta.BuyNode(Hero, "dh_cold_rage"));
            Assert.AreEqual(MetaStatus.Excluded, _meta.BuyNode(Hero, "dh_blood_rage"));

            int spent = _meta.TreeSpent(Hero);
            Assert.AreEqual(480 + 620 + 300 + 600, spent);

            int refund = _meta.ResetTree(Hero);

            Assert.AreEqual(spent, refund);
            Assert.AreEqual(5000, _meta.MemoryShards);
            Assert.AreEqual(0, _meta.NodeRank(Hero, "dh_cold_rage"));
            Assert.AreEqual(MetaStatus.NeedsParents, _meta.NodeOffer(Hero, "dh_blood_rage").Status);
        }

        [Test]
        public void GoldenNodeNeedsTwoOfThreeBranchEnds()
        {
            _meta.DebugAddCurrency(20000, 0);

            BuyToEnd("dh_boiling_blood", 3);
            BuyToEnd("dh_lasting_rage", 3);
            _meta.BuyNode(Hero, "dh_endless_whirl");
            _meta.BuyNode(Hero, "dh_blood_rage");

            Assert.AreEqual(MetaStatus.GateUnmet, _meta.NodeOffer(Hero, "dh_double_volley").Status);

            BuyToEnd("dh_heavy_hand", 3);
            _meta.BuyNode(Hero, "dh_keen_executioner");
            _meta.BuyNode(Hero, "dh_burning_leap");

            Assert.AreEqual(MetaStatus.Available, _meta.BuyNode(Hero, "dh_double_volley"));
        }

        [Test]
        public void GemsFitTheirColourAndReturnWhenTakenOut()
        {
            _meta.DebugAddCurrency(0, 5000);
            _meta.DebugGrantGems();

            Assert.AreEqual(MetaStatus.SocketLocked, _meta.InsertGem(Blade, "s2", "gem_sapphire_storm"));

            for (int level = 1; level < 5; level++)
                Assert.AreEqual(MetaStatus.Available, _meta.UpgradeWeapon(Blade));

            Assert.AreEqual(MetaStatus.WrongColor, _meta.InsertGem(Blade, "s1", "gem_sapphire_storm"));
            Assert.AreEqual(MetaStatus.Available, _meta.InsertGem(Blade, "s1", "gem_ruby_wrath"));
            Assert.AreEqual(MetaStatus.Available, _meta.InsertGem(Blade, "s3", "gem_emerald_fortitude"));
            Assert.AreEqual(0, _meta.GemCount("gem_ruby_wrath"));

            Assert.AreEqual(MetaStatus.Available, _meta.ClearSocket(Blade, "s1"));
            Assert.AreEqual(1, _meta.GemCount("gem_ruby_wrath"));
            Assert.IsNull(_meta.SocketContent(Blade, "s1"));
        }

        [Test]
        public void RelicMovesBetweenWeapons()
        {
            _meta.DebugAddCurrency(0, 1000);
            _meta.DebugGrantRelics();

            Assert.AreEqual(MetaStatus.Available, _meta.InsertRelic(Blade, "relic", Relic));
            Assert.AreEqual(MetaStatus.Available, _meta.ForgeOrEquip(Hero, Axe));
            Assert.AreEqual(MetaStatus.Available, _meta.InsertRelic(Axe, "relic", Relic));

            Assert.IsNull(_meta.SocketContent(Blade, "relic"));
            Assert.AreEqual(Relic, _meta.SocketContent(Axe, "relic"));
        }

        [Test]
        public void RelicDropsOnlyOnTheFirstVictory()
        {
            RunRewardService rewards = new RunRewardService(_meta, _content);
            RunReport victory = Report(true, new[] { "boss.knight" });

            RunRewards first = rewards.Grant(victory);
            RunRewards second = rewards.Grant(victory);

            Assert.AreEqual(1843 + 500, first.MemoryShards);
            Assert.AreEqual(6 * 5 + 3 * 15 + 40, first.MagicShards);
            Assert.IsTrue(first.RelicGranted);
            Assert.IsFalse(second.RelicGranted);
            Assert.IsTrue(second.RelicRepeat);
            Assert.AreEqual(1, _meta.RelicCount);

            RunRewards defeat = rewards.Grant(Report(false, new string[0]));

            Assert.Greater(defeat.MemoryShards, 0);
            Assert.IsNull(defeat.RelicId);
        }

        [Test]
        public void SaveSurvivesReloadAndSkipsUnknownIds()
        {
            _meta.DebugAddCurrency(500, 100);
            _meta.BuyGlobalRank("global_armor");

            string json = _storage.Files[MetaProgress.FileName].Replace("\"Id\": \"global_armor\"", "\"Id\": \"global_removed\"");
            _storage.Files[MetaProgress.FileName] = json.Replace("\"Gems\": [", "\"Gems\": [{\"Id\": \"gem_unknown\", \"Rank\": 3}");

            MetaProgress reloaded = new MetaProgress(_content, _storage);

            Assert.AreEqual(440, reloaded.MemoryShards);
            Assert.AreEqual(100, reloaded.MagicShards);
            Assert.AreEqual(0, reloaded.TotalGlobalRanks);
            Assert.AreEqual(0, reloaded.GemCount("gem_unknown"));
        }

        [Test]
        public void CorruptSaveStartsFreshAndKeepsACopy()
        {
            _storage.Files[MetaProgress.FileName] = "{\"Version\":1,\"MemoryShards\":86";

            MetaProgress reloaded = new MetaProgress(_content, _storage);

            Assert.AreEqual(0, reloaded.MemoryShards);
            Assert.IsTrue(_storage.Files.ContainsKey(MetaProgress.CorruptFileName));
        }

        [Test]
        public void LoadoutCarriesHeavyHandAndWeaponLevelAsIncreasedDamage()
        {
            _meta.DebugAddCurrency(1000, 1000);
            BuyToEnd("dh_heavy_hand", 3);

            for (int level = 1; level < 5; level++)
                _meta.UpgradeWeapon(Blade);

            RunLoadout loadout = new MetaLoadoutBuilder(_meta, _content).Build(Hero);
            float increased = 0f;

            foreach (StatModifierSpec spec in loadout.Modifiers)
            {
                if (spec.Stat == StatId.WeaponDamage && spec.Op == StatOp.Increased)
                    increased += spec.Value;
            }

            Assert.AreEqual(0.24f, increased, 1e-5f);
        }

        private void BuyToEnd(string node, int ranks)
        {
            for (int rank = 0; rank < ranks; rank++)
                Assert.AreEqual(MetaStatus.Available, _meta.BuyNode(Hero, node), node);
        }

        private static RunReport Report(bool victory, string[] bosses)
        {
            return new RunReport("level.gameplay", Hero, victory, 872f, 184320f, 251940f, 1912f, 1284f, 1406, 9, 10,
                new[] { 6, 3, 1, 0 }, 1288, 23, bosses);
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();

            public bool TryRead(string fileName, out string content)
            {
                return Files.TryGetValue(fileName, out content);
            }

            public void Write(string fileName, string content)
            {
                Files[fileName] = content;
            }

            public void Delete(string fileName)
            {
                Files.Remove(fileName);
            }
        }
    }
}

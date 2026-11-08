using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Meta.Tests
{
    public sealed class CampaignProgressTests
    {
        private const string First = "level.gameplay";
        private const string Second = "level.troll_lair";
        private const string Boss = "boss.knight";

        private ContentRegistry _content;
        private MemoryStorage _storage;

        [SetUp]
        public void SetUp()
        {
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>("Assets/_Project/Configs/ContentDatabase.asset");
            Assert.NotNull(database);
            _content = new ContentRegistry(database.Entries);
            _storage = new MemoryStorage();
        }

        [Test]
        public void V1MigrationPreservesExistingMetadataAndStartsCampaignEmpty()
        {
            MetaSaveData old = new MetaSaveData
            {
                Version = 1,
                MemoryShards = 123,
                MagicShards = 45,
                Global = new List<RankRecord> { new RankRecord { Id = "global_armor", Rank = 1 } },
                Paragon = new List<RankRecord> { new RankRecord { Id = "paragon_vitality", Rank = 2 } },
                Trees = new List<TreeRecord> { new TreeRecord { CharacterId = "character.berserk" } },
                Weapons = new List<WeaponRecord> { new WeaponRecord { Id = "dh_weapon_blade", Level = 2 } },
                Equipped = new List<EquipRecord> { new EquipRecord { CharacterId = "character.berserk", WeaponId = "dh_weapon_blade" } },
                Gems = new List<RankRecord> { new RankRecord { Id = "gem_ruby_wrath", Rank = 2 } },
                Relics = new List<RelicRecord> { new RelicRecord { Id = "relic_knight_banner", Level = 1 } },
                DefeatedBosses = new List<string> { Boss },
                CompletedLevelIds = new List<string> { First }
            };
            _storage.Files[MetaProgress.FileName] = JsonUtility.ToJson(old);

            MetaProgress progress = NewProgress();
            Assert.IsTrue(progress.CanSave);
            Assert.IsEmpty(progress.CompletedLevelIds);
            Assert.IsFalse(progress.IsAvailable(Second));
            Assert.AreEqual(123, progress.MemoryShards);
            Assert.AreEqual(45, progress.MagicShards);
            Assert.IsTrue(progress.IsBossDefeated(Boss));

            progress.DebugAddCurrency(0, 0);
            MetaSaveData migrated = Saved();
            Assert.AreEqual(2, migrated.Version);
            Assert.AreEqual(1, migrated.Global.Count);
            Assert.AreEqual(2, migrated.Paragon[0].Rank);
            Assert.AreEqual(1, migrated.Trees.Count);
            Assert.AreEqual(2, migrated.Weapons[0].Level);
            Assert.AreEqual(1, migrated.Equipped.Count);
            Assert.AreEqual(2, migrated.Gems[0].Rank);
            Assert.AreEqual(1, migrated.Relics.Count);
            Assert.AreEqual(Boss, migrated.DefeatedBosses[0]);
            Assert.IsEmpty(migrated.CompletedLevelIds);
            Assert.IsEmpty(NewProgress().CompletedLevelIds);
        }

        [Test]
        public void V2MissingOrNullCampaignFieldStartsEmpty()
        {
            _storage.Files[MetaProgress.FileName] = "{\"Version\":2,\"MemoryShards\":7}";
            Assert.IsEmpty(NewProgress().CompletedLevelIds);

            _storage.Files[MetaProgress.FileName] = "{\"Version\":2,\"CompletedLevelIds\":null}";
            Assert.IsEmpty(NewProgress().CompletedLevelIds);
        }

        [Test]
        public void V2DeduplicatesAndPreservesUnknownLevelIdsAcrossReloads()
        {
            _storage.Files[MetaProgress.FileName] = "{\"Version\":2,\"CompletedLevelIds\":[null,\"\",\"  \",\"level.unknown\",\"level.unknown\",\"level.gameplay\"]}";
            MetaProgress progress = NewProgress();
            CollectionAssert.AreEqual(new[] { "level.unknown", First }, progress.CompletedLevelIds);
            Assert.IsTrue(progress.IsAvailable(Second));
            Assert.IsFalse(progress.IsAvailable("level.unknown"));
            progress.DebugAddCurrency(0, 0);
            CollectionAssert.AreEqual(progress.CompletedLevelIds, NewProgress().CompletedLevelIds);
        }

        [Test]
        public void FutureVersionNeverOverwritesOriginal()
        {
            string original = "{\"Version\":99,\"MemoryShards\":321}";
            _storage.Files[MetaProgress.FileName] = original;
            MetaProgress progress = NewProgress();
            Assert.IsFalse(progress.CanSave);
            progress.DebugAddCurrency(1, 0);
            progress.DebugReset();
            Assert.IsFalse(progress.LastSaveSucceeded);
            Assert.AreEqual(original, _storage.Files[MetaProgress.FileName]);
            Assert.AreEqual(0, _storage.MainWrites);
            Assert.AreEqual(0, _storage.Deletes);
        }

        [Test]
        public void CorruptJsonNeverOverwritesOriginalOrExistingBackup()
        {
            string original = "{\"Version\":2,invalid";
            _storage.Files[MetaProgress.FileName] = original;
            _storage.Files[MetaProgress.CorruptFileName] = "older backup";
            MetaProgress progress = NewProgress();
            progress.DebugAddCurrency(1, 0);
            Assert.IsFalse(progress.CanSave);
            Assert.AreEqual(original, _storage.Files[MetaProgress.FileName]);
            Assert.AreEqual("older backup", _storage.Files[MetaProgress.CorruptFileName]);
            Assert.AreEqual(0, _storage.MainWrites);
        }

        [Test]
        public void ReadFailureBlocksWritesAndPreservesOriginal()
        {
            string original = "{\"Version\":2,\"MemoryShards\":21}";
            _storage.Files[MetaProgress.FileName] = original;
            _storage.ThrowOnRead = true;
            MetaProgress progress = NewProgress();
            progress.DebugAddCurrency(1, 0);
            Assert.IsFalse(progress.CanSave);
            Assert.AreEqual(original, _storage.Files[MetaProgress.FileName]);
            Assert.AreEqual(0, _storage.MainWrites);
        }

        [Test]
        public void WriteFailureReportsFailureAndKeepsOriginal()
        {
            string original = "{\"Version\":2,\"MemoryShards\":21}";
            _storage.Files[MetaProgress.FileName] = original;
            MetaProgress progress = NewProgress();
            _storage.ThrowOnWrite = true;
            LogAssert.Expect(LogType.Error, "Meta progress was not saved: simulated write failure");
            progress.DebugAddCurrency(1, 0);
            Assert.IsFalse(progress.LastSaveSucceeded);
            Assert.AreEqual(original, _storage.Files[MetaProgress.FileName]);
        }

        [Test]
        public void AccessFollowsCampaignPrerequisitesAndIgnoresOldBossHistory()
        {
            MetaProgress progress = NewProgress();
            Assert.IsTrue(progress.IsAvailable(First));
            Assert.IsFalse(progress.IsAvailable(Second));
            Assert.IsFalse(progress.IsAvailable("level.stress"));
            Assert.IsFalse(progress.IsAvailable("level.unknown"));

            _storage.Files[MetaProgress.FileName] = "{\"Version\":2,\"DefeatedBosses\":[\"boss.knight\"]}";
            Assert.IsFalse(NewProgress().IsAvailable(Second));
        }

        [Test]
        public void StoryVictoryWithExpectedBossCompletesOnceAndSurvivesReload()
        {
            MetaProgress progress = NewProgress();
            RunRewardService rewards = new RunRewardService(progress, _content);
            rewards.Grant(Report(First, true, new[] { Boss }, RunMode.Story));
            Assert.IsTrue(progress.IsCompleted(First));
            Assert.IsTrue(progress.IsAvailable(Second));
            Assert.IsTrue(progress.LastSaveSucceeded);
            Assert.AreEqual(1, _storage.MainWrites);
            rewards.Grant(Report(First, true, new[] { Boss }, RunMode.Story));
            Assert.AreEqual(1, progress.CompletedLevelIds.Count);
            Assert.IsTrue(NewProgress().IsAvailable(Second));
            CollectionAssert.AreEqual(new[] { First }, Saved().CompletedLevelIds);
        }

        [TestCase(true, new string[0], RunMode.Story, First)]
        [TestCase(false, new[] { Boss }, RunMode.Story, First)]
        [TestCase(true, new[] { Boss }, RunMode.Endless, First)]
        [TestCase(true, new[] { Boss }, RunMode.Story, "level.stress")]
        [TestCase(true, new[] { "boss.unknown" }, RunMode.Story, First)]
        public void OtherOutcomesDoNotCompleteCampaign(bool victory, string[] bosses, RunMode mode, string level)
        {
            MetaProgress progress = NewProgress();
            new RunRewardService(progress, _content).Grant(Report(level, victory, bosses, mode));
            Assert.IsEmpty(progress.CompletedLevelIds);
            Assert.IsFalse(progress.IsAvailable(Second));
        }

        private MetaProgress NewProgress() => new MetaProgress(_content, _storage);

        private MetaSaveData Saved() => JsonUtility.FromJson<MetaSaveData>(_storage.Files[MetaProgress.FileName]);

        private static RunReport Report(string level, bool victory, string[] bosses, RunMode mode)
        {
            return new RunReport(level, "character.berserk", victory, 30f, 100f, 100f, 0f, 0f, 1, 0, 0,
                Array.Empty<int>(), 0, 0, bosses, mode);
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool ThrowOnRead;
            public bool ThrowOnWrite;
            public int MainWrites;
            public int Deletes;

            public bool TryRead(string fileName, out string content)
            {
                if (ThrowOnRead)
                    throw new InvalidOperationException("simulated read failure");
                return Files.TryGetValue(fileName, out content);
            }

            public void Write(string fileName, string content)
            {
                if (ThrowOnWrite)
                    throw new InvalidOperationException("simulated write failure");
                Files[fileName] = content;
                if (fileName == MetaProgress.FileName)
                    MainWrites++;
            }

            public void Delete(string fileName)
            {
                Files.Remove(fileName);
                Deletes++;
            }
        }
    }
}

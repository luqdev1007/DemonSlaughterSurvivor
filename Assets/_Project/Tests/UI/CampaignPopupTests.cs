using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Tests
{
    public sealed class CampaignPopupTests
    {
        private CampaignConfig _campaign;
        private ContentRegistry _content;

        [SetUp]
        public void SetUp()
        {
            _campaign = AssetDatabase.LoadAssetAtPath<CampaignConfig>("Assets/_Project/Configs/Campaign.asset");
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>("Assets/_Project/Configs/ContentDatabase.asset");
            Assert.NotNull(_campaign);
            Assert.NotNull(database);
            _content = new ContentRegistry(database.Entries);
        }

        [Test]
        public void FreshCampaignShowsReadyFirstLevelAndLockedSecondLevel()
        {
            StubProgress progress = new StubProgress();
            Assert.AreEqual(CampaignTileState.Ready, CampaignPopupPresenter.StateFor(_campaign.Levels[0], progress, _content));
            Assert.AreEqual(CampaignTileState.Locked, CampaignPopupPresenter.StateFor(_campaign.Levels[1], progress, _content));
        }

        [Test]
        public void CompletedFirstLevelMakesRegisteredTrollLairReady()
        {
            StubProgress progress = new StubProgress { FirstCompleted = true };
            Assert.AreEqual(CampaignTileState.Ready, CampaignPopupPresenter.StateFor(_campaign.Levels[1], progress, _content));
            Assert.IsTrue(_content.TryGet(_campaign.Levels[1].Level.Id, out LevelConfig _));
        }

        [Test]
        public void PopupBuildsCatalogOrderWithPreviewFallbackAndNonInteractiveLockedTile()
        {
            MetaUiStyle style = AssetDatabase.LoadAssetAtPath<MetaUiStyle>("Assets/_Project/Prefabs/UI/MetaUiStyle.asset");
            Assert.NotNull(style);
            GameObject canvas = new GameObject("TestCanvas", typeof(RectTransform));

            try
            {
                CampaignPopupView view = UiKit.Rect("CampaignPopup", canvas.transform).gameObject.AddComponent<CampaignPopupView>();
                view.Initialize(style);
                view.SetLevels(_campaign.Levels, new[] { CampaignTileState.Ready, CampaignTileState.Locked });

                Transform levels = view.transform.Find("Panel/Viewport/Levels");
                Assert.NotNull(levels);
                Assert.AreEqual(2, levels.childCount);
                Assert.AreEqual("Level_level.gameplay", levels.GetChild(0).name);
                Assert.AreEqual("Level_level.troll_lair", levels.GetChild(1).name);
                Assert.NotNull(levels.GetChild(0).Find("Preview/PreviewFallback"));
                Assert.IsTrue(levels.GetChild(0).Find("SelectButton").GetComponent<Button>().interactable);
                Assert.IsFalse(levels.GetChild(1).Find("SelectButton").GetComponent<Button>().interactable);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        private sealed class StubProgress : ICampaignProgress
        {
            public bool FirstCompleted;
            public IReadOnlyList<string> CompletedLevelIds => FirstCompleted ? new[] { CampaignConfig.FirstLevelId } : new string[0];
            public bool IsCompleted(string levelId) => FirstCompleted && levelId == CampaignConfig.FirstLevelId;
            public bool IsAvailable(string levelId) => levelId == CampaignConfig.FirstLevelId ||
                levelId == "level.troll_lair" && FirstCompleted;
        }
    }
}

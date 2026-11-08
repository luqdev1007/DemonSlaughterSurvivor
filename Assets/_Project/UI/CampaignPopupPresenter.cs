using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;

namespace Game.UI
{
    public sealed class CampaignPopupPresenter : IDisposable
    {
        private readonly CampaignPopupView _view;
        private readonly CampaignConfig _campaign;
        private readonly ICampaignProgress _progress;
        private readonly IContentRegistry _content;

        public CampaignPopupPresenter(CampaignPopupView view, CampaignConfig campaign, ICampaignProgress progress, IContentRegistry content)
        {
            _view = view;
            _campaign = campaign;
            _progress = progress;
            _content = content;
            _view.LevelSelected += OnSelected;
            _view.Closed += OnClosed;

            List<CampaignTileState> states = new List<CampaignTileState>();

            foreach (CampaignLevel entry in campaign.Levels)
                states.Add(StateFor(entry, progress, content));

            _view.SetLevels(campaign.Levels, states);
        }

        public event Action<string> LevelSelected;
        public event Action Closed;

        public void Open() => _view.Show();

        public void Close() => _view.Hide();

        public void Dispose()
        {
            _view.LevelSelected -= OnSelected;
            _view.Closed -= OnClosed;
        }

        public static CampaignTileState StateFor(CampaignLevel entry, ICampaignProgress progress, IContentRegistry content)
        {
            if (progress.IsAvailable(entry.Level.Id) == false)
                return CampaignTileState.Locked;

            LevelConfig level = entry.Level;

            if (content.TryGet(level.Id, out LevelConfig registered) == false ||
                ReferenceEquals(level, registered) == false || string.IsNullOrWhiteSpace(level.SceneName) ||
                level.Waves == null || level.CameraRigPrefab == null || level.HudPrefab == null || level.Feedback == null)
                return CampaignTileState.ComingSoon;

            return CampaignTileState.Ready;
        }

        private void OnSelected(string levelId)
        {
            foreach (CampaignLevel entry in _campaign.Levels)
            {
                if (entry.Level.Id != levelId)
                    continue;

                if (StateFor(entry, _progress, _content) == CampaignTileState.Ready)
                    LevelSelected?.Invoke(levelId);

                return;
            }
        }

        private void OnClosed() => Closed?.Invoke();
    }
}

using Game.Configs;
using Game.Core;
using R3;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MetaHubPresenter : IDisposable
    {
        private const int ArsenalTab = 0;
        private const int TreeTab = 1;
        private const int GlobalTab = 2;
        private const int DebugMemoryShards = 1000;
        private const int DebugMagicShards = 300;

        private readonly MetaHubView _view;
        private readonly IMetaProgress _meta;
        private readonly IContentRegistry _content;
        private readonly IHeroSelection _heroes;
        private readonly MetaArsenalTab _arsenal;
        private readonly MetaTreeTab _tree;
        private readonly MetaGlobalTab _global;
        private readonly IDisposable _subscription;

        private int _tab;
        private bool _isOpen;
        private string _characterId;

        public MetaHubPresenter(MetaHubView view, IMetaProgress meta, IContentRegistry content, IHeroSelection heroes)
        {
            _view = view;
            _meta = meta;
            _content = content;
            _heroes = heroes;

            _arsenal = new MetaArsenalTab(view.Page(ArsenalTab), view, meta, content);
            _tree = new MetaTreeTab(view.Page(TreeTab), view, meta, content);
            _global = new MetaGlobalTab(view.Page(GlobalTab), view, meta, content);

            _view.TabSelected += SelectTab;
            _view.Closed += Close;
            _view.HideImmediate();

            _subscription = Observable.FromEvent(handler => _meta.Changed += handler, handler => _meta.Changed -= handler)
                .Subscribe(this, (_, presenter) => presenter.Refresh());
        }

        public event Action Closed;

        public void Open(string characterId)
        {
            _isOpen = true;
            _characterId = characterId;

            BuildHeroes();
            BuildDebug();

            _view.Open();
            SelectTab(_tab);
        }

        public void Close()
        {
            if (_isOpen == false)
                return;

            _isOpen = false;
            _view.Close();

            Closed?.Invoke();
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _view.TabSelected -= SelectTab;
            _view.Closed -= Close;
        }

        private void SelectTab(int tab)
        {
            _tab = tab;
            _view.ShowTab(tab);
            Refresh();
        }

        private void Refresh()
        {
            if (_isOpen == false)
                return;

            _view.SetWallet(_meta.MemoryShards, _meta.MagicShards, _meta.RelicCount);

            _arsenal.SetCharacter(_characterId);
            _tree.SetCharacter(_characterId);

            switch (_tab)
            {
                case ArsenalTab:
                    _arsenal.Refresh();
                    break;
                case TreeTab:
                    _tree.Refresh();
                    break;
                default:
                    _global.Refresh();
                    break;
            }
        }

        private void BuildHeroes()
        {
            RectTransform row = _view.HeroRow;
            UiKit.Clear(row);

            List<CharacterConfig> heroes = new List<CharacterConfig>(_content.All<CharacterConfig>());
            heroes.Sort((left, right) => left.MenuOrder.CompareTo(right.MenuOrder));

            foreach (CharacterConfig hero in heroes)
            {
                string id = hero.Id;
                Button button = UiKit.Button(row, id, _view.Style, hero.DisplayName, () => SelectHero(id), 22f);
                ((RectTransform)button.transform).sizeDelta = new Vector2(190f, 56f);
                button.GetComponent<Image>().sprite = id == _characterId ? _view.Style.Button : _view.Style.ButtonOff;
            }
        }

        private void SelectHero(string characterId)
        {
            _characterId = characterId;
            _heroes.SelectCharacter(characterId);

            BuildHeroes();
            Refresh();
        }

        private void BuildDebug()
        {
            RectTransform row = _view.DevRow;
            UiKit.Clear(row);

            if (Debug.isDebugBuild == false)
                return;

            TMP_Text label = UiKit.Text(row, "Label", _view.Style.BodyFont, 18f, MetaPalette.Faint, TextAlignmentOptions.Right, "тест:");
            label.rectTransform.sizeDelta = new Vector2(60f, 40f);

            DebugButton(row, "+1000 воспоминаний", () => _meta.DebugAddCurrency(DebugMemoryShards, 0));
            DebugButton(row, "+300 магических", () => _meta.DebugAddCurrency(0, DebugMagicShards));
            DebugButton(row, "самоцветы", _meta.DebugGrantGems);
            DebugButton(row, "реликвия", _meta.DebugGrantRelics);
            DebugButton(row, "сброс меты", _meta.DebugReset);
        }

        private void DebugButton(RectTransform row, string label, Action onClick)
        {
            Button button = UiKit.Button(row, label, _view.Style, label, onClick, 17f);
            button.GetComponent<Image>().sprite = _view.Style.ButtonOff;
            ((RectTransform)button.transform).sizeDelta = new Vector2(label.Length > 12 ? 200f : 150f, 40f);
        }
    }
}

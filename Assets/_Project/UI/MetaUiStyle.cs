using TMPro;
using UnityEngine;

namespace Game.UI
{
    [CreateAssetMenu(fileName = "MetaUiStyle", menuName = "Game/UI/Meta UI Style")]
    public sealed class MetaUiStyle : ScriptableObject
    {
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset _titleFont;
        [SerializeField] private TMP_FontAsset _bodyFont;

        [Header("GUI_Parts")]
        [SerializeField] private Sprite _panelBack;
        [SerializeField] private Sprite _panelFrame;
        [SerializeField] private Sprite _cardBack;
        [SerializeField] private Sprite _cardFrame;
        [SerializeField] private Sprite _button;
        [SerializeField] private Sprite _buttonOff;
        [SerializeField] private Sprite _roundFrame;
        [SerializeField] private Sprite _roundBack;
        [SerializeField] private Sprite _bar;

        [Header("Icons")]
        [SerializeField] private Sprite _memoryIcon;
        [SerializeField] private Sprite _magicIcon;
        [SerializeField] private Sprite _relicIcon;
        [SerializeField] private Sprite _relicOffIcon;
        [SerializeField] private Sprite _redGem;
        [SerializeField] private Sprite _blueGem;
        [SerializeField] private Sprite _greenGem;
        [SerializeField] private Sprite _softGlow;

        [Header("Shapes")]
        [SerializeField] private Sprite _circle;
        [SerializeField] private Sprite _circleRing;
        [SerializeField] private Sprite _square;
        [SerializeField] private Sprite _squareRing;
        [SerializeField] private Sprite _diamond;
        [SerializeField] private Sprite _diamondRing;

        [Header("Weapons")]
        [SerializeField] private Sprite _bladeArt;
        [SerializeField] private Sprite _axeArt;

        public TMP_FontAsset TitleFont => _titleFont;
        public TMP_FontAsset BodyFont => _bodyFont;
        public Sprite PanelBack => _panelBack;
        public Sprite PanelFrame => _panelFrame;
        public Sprite CardBack => _cardBack;
        public Sprite CardFrame => _cardFrame;
        public Sprite Button => _button;
        public Sprite ButtonOff => _buttonOff;
        public Sprite RoundFrame => _roundFrame;
        public Sprite RoundBack => _roundBack;
        public Sprite Bar => _bar;
        public Sprite MemoryIcon => _memoryIcon;
        public Sprite MagicIcon => _magicIcon;
        public Sprite RelicIcon => _relicIcon;
        public Sprite RelicOffIcon => _relicOffIcon;
        public Sprite RedGem => _redGem;
        public Sprite BlueGem => _blueGem;
        public Sprite GreenGem => _greenGem;
        public Sprite SoftGlow => _softGlow;
        public Sprite Circle => _circle;
        public Sprite CircleRing => _circleRing;
        public Sprite Square => _square;
        public Sprite SquareRing => _squareRing;
        public Sprite Diamond => _diamond;
        public Sprite DiamondRing => _diamondRing;
        public Sprite BladeArt => _bladeArt;
        public Sprite AxeArt => _axeArt;

        public bool IsWired => _titleFont != null && _bodyFont != null && _panelBack != null && _button != null && _memoryIcon != null && _circle != null;
    }
}

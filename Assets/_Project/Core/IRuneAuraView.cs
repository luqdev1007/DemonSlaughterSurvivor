using UnityEngine;

namespace Game.Core
{
    public interface IRuneAuraView
    {
        void SetRune(bool active, Color color, Texture glyph, float fraction, float remainingSeconds);
    }
}

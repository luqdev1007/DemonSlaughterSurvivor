using UnityEngine;

namespace Game.Core
{
    public interface ILightningView
    {
        void Strike(Vector3 from, Vector3 to);
    }
}

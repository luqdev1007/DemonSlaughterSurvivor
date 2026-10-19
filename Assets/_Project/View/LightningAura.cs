using UnityEngine;

namespace Game.View
{
    public sealed class LightningAura : MonoBehaviour
    {
        [SerializeField] private BerserkLightningFx _arcs;

        private void OnEnable()
        {
            if (_arcs != null)
                _arcs.SetShowing(true);
        }

        private void OnDisable()
        {
            if (_arcs != null)
                _arcs.ResetImmediate();
        }
    }
}

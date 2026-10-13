using UnityEngine;

namespace Game.View
{
    public sealed class ViewPoolTicker : MonoBehaviour
    {
        internal ViewPool Pool { get; set; }

        private void Update()
        {
            Pool?.Tick(Time.time, Time.deltaTime);
        }
    }
}

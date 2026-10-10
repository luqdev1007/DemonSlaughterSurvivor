using System;
using UnityEngine;

namespace Game.View
{
    internal sealed class GoreRunner : MonoBehaviour
    {
        private Action<float> _tick;

        internal void Bind(Action<float> tick)
        {
            _tick = tick;
        }

        private void Update()
        {
            _tick?.Invoke(Time.deltaTime);
        }
    }
}

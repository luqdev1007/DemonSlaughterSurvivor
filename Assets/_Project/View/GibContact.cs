using System;
using UnityEngine;

namespace Game.View
{
    internal sealed class GibContact : MonoBehaviour
    {
        private Action<int, Collision> _onContact;
        private int _index;

        internal void Bind(int index, Action<int, Collision> onContact)
        {
            _index = index;
            _onContact = onContact;
        }

        private void OnCollisionEnter(Collision collision)
        {
            _onContact?.Invoke(_index, collision);
        }
    }
}

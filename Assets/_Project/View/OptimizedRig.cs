using System;
using UnityEngine;

namespace Game.View
{
    public sealed class OptimizedRig : MonoBehaviour
    {
        [SerializeField] private string[] _exposedTransforms = Array.Empty<string>();

        public string[] ExposedTransforms => _exposedTransforms;
    }
}

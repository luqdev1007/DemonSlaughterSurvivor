using UnityEngine;

namespace Game.View
{
    public sealed class ParticleReplay : MonoBehaviour
    {
        private ParticleSystem[] _systems;

        private void OnEnable()
        {
            if (_systems == null)
                _systems = GetComponentsInChildren<ParticleSystem>(true);

            for (int index = 0; index < _systems.Length; index++)
            {
                _systems[index].Clear(false);
                _systems[index].Play(false);
            }
        }
    }
}

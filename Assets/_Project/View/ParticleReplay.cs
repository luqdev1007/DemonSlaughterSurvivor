using UnityEngine;

namespace Game.View
{
    public sealed class ParticleReplay : MonoBehaviour
    {
        private ParticleSystem[] _systems;
        private TrailRenderer[] _trails;

        private void OnEnable()
        {
            if (_systems == null)
            {
                _systems = GetComponentsInChildren<ParticleSystem>(true);
                _trails = GetComponentsInChildren<TrailRenderer>(true);
            }

            for (int index = 0; index < _trails.Length; index++)
                _trails[index].Clear();

            for (int index = 0; index < _systems.Length; index++)
            {
                _systems[index].Clear(false);
                _systems[index].Play(false);
            }
        }
    }
}

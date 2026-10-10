using UnityEngine;

namespace Game.View
{
    internal sealed class BloodFxPool
    {
        private sealed class Instance
        {
            public GameObject Object;
            public Transform Transform;
            public ParticleSystem System;
            public bool Active;
            public bool Stopping;
            public float StopTime;
            public float ReleaseTime;
            public Transform Follow;
            public Vector3 LocalOffset;
            public Quaternion LocalRotation;
        }

        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Instance[] _instances;
        private readonly float _oneShotSeconds;
        private readonly float _fadeSeconds;
        private readonly float _scale;

        private int _count;

        public BloodFxPool(GameObject prefab, int capacity, Transform parent, float scale)
        {
            _prefab = prefab;
            _scale = scale;
            _parent = parent;
            _instances = new Instance[Mathf.Max(0, capacity)];

            ResolveTimes(prefab, out _oneShotSeconds, out _fadeSeconds);
        }

        public int Play(Vector3 position, Quaternion rotation)
        {
            int handle = Take();

            if (handle < 0)
                return -1;

            Instance instance = _instances[handle];

            Start(instance, position, rotation);

            instance.StopTime = 0f;
            instance.ReleaseTime = Time.time + _oneShotSeconds;
            instance.Stopping = true;

            return handle;
        }

        public int PlayAttached(Transform follow, Vector3 localOffset, Quaternion localRotation, float seconds)
        {
            int handle = Take();

            if (handle < 0)
                return -1;

            Instance instance = _instances[handle];

            instance.Follow = follow;
            instance.LocalOffset = localOffset;
            instance.LocalRotation = localRotation;

            Start(instance, follow.TransformPoint(localOffset), follow.rotation * localRotation);

            instance.StopTime = seconds > 0f ? Time.time + seconds : 0f;
            instance.Stopping = false;

            return handle;
        }

        public void Stop(int handle, Transform follow)
        {
            if (handle < 0 || handle >= _count)
                return;

            Instance instance = _instances[handle];

            if (instance.Active == false || instance.Follow != follow)
                return;

            BeginStop(instance);
        }

        public void Tick(float now)
        {
            for (int index = 0; index < _count; index++)
            {
                Instance instance = _instances[index];

                if (instance.Active == false)
                    continue;

                if (instance.Follow != null)
                {
                    if (instance.Follow.gameObject.activeInHierarchy == false)
                    {
                        BeginStop(instance);
                    }
                    else
                    {
                        instance.Transform.SetPositionAndRotation(
                            instance.Follow.TransformPoint(instance.LocalOffset),
                            instance.Follow.rotation * instance.LocalRotation);
                    }
                }

                if (instance.Stopping == false && instance.StopTime > 0f && now >= instance.StopTime)
                    BeginStop(instance);

                if (instance.Stopping && now >= instance.ReleaseTime)
                {
                    instance.Active = false;
                    instance.Follow = null;
                    instance.Object.SetActive(false);
                }
            }
        }

        private void Start(Instance instance, Vector3 position, Quaternion rotation)
        {
            instance.Active = true;
            instance.Transform.SetPositionAndRotation(position, rotation);
            instance.Object.SetActive(true);
            instance.System.Clear(true);
            instance.System.Play(true);
        }

        private void BeginStop(Instance instance)
        {
            if (instance.Stopping)
                return;

            instance.Stopping = true;
            instance.Follow = null;
            instance.ReleaseTime = Time.time + _fadeSeconds;
            instance.System.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private int Take()
        {
            if (_prefab == null)
                return -1;

            for (int index = 0; index < _count; index++)
            {
                if (_instances[index].Active == false)
                    return index;
            }

            if (_count >= _instances.Length)
                return -1;

            GameObject created = Object.Instantiate(_prefab, _parent);
            created.SetActive(false);
            created.transform.localScale *= _scale;

            ParticleSystem system = created.GetComponent<ParticleSystem>();

            if (system == null)
                system = created.GetComponentInChildren<ParticleSystem>(true);

            _instances[_count] = new Instance
            {
                Object = created,
                Transform = created.transform,
                System = system
            };

            _count++;

            return _count - 1;
        }

        private static void ResolveTimes(GameObject prefab, out float oneShotSeconds, out float fadeSeconds)
        {
            oneShotSeconds = 0f;
            fadeSeconds = 0f;

            if (prefab == null)
                return;

            ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(true);

            for (int index = 0; index < systems.Length; index++)
            {
                ParticleSystem.MainModule main = systems[index].main;
                float lifetime = main.startLifetime.constantMax;

                oneShotSeconds = Mathf.Max(oneShotSeconds, main.startDelay.constantMax + main.duration + lifetime);
                fadeSeconds = Mathf.Max(fadeSeconds, lifetime);
            }
        }
    }
}

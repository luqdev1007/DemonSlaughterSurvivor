using Game.Configs;
using Game.Core;
using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.View
{
    public sealed class CameraService : ICameraService, IDisposable
    {
        private readonly GameObject _rig;
        private readonly CinemachineCamera _camera;

        public CameraService(LevelConfig level)
        {
            if (level.CameraRigPrefab == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no camera rig prefab assigned.");

            _rig = UnityEngine.Object.Instantiate(level.CameraRigPrefab);
            _rig.name = level.CameraRigPrefab.name;

            _camera = _rig.GetComponentInChildren<CinemachineCamera>();

            if (_camera == null)
                throw new InvalidOperationException(
                    $"Camera rig prefab '{level.CameraRigPrefab.name}' has no {nameof(CinemachineCamera)} in its hierarchy.");

            if (_camera.TryGetComponent(out CinemachineFollow follow) == false)
                throw new InvalidOperationException(
                    $"{nameof(CinemachineCamera)} on '{level.CameraRigPrefab.name}' has no {nameof(CinemachineFollow)} component.");

            Quaternion rotation = Quaternion.Euler(level.CameraPitch, level.CameraYaw, 0f);
            Vector3 offset = rotation * (Vector3.back * level.CameraDistance);

            follow.FollowOffset = offset;

            _camera.transform.SetPositionAndRotation(offset, rotation);
            _camera.PreviousStateIsValid = false;
        }

        public void SetFollowTarget(Transform target)
        {
            _camera.Target.TrackingTarget = target;
        }

        public void Dispose()
        {
            if (_rig != null)
                UnityEngine.Object.Destroy(_rig);
        }
    }
}

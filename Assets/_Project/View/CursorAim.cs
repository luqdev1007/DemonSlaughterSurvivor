using Game.Core;
using UnityEngine;

namespace Game.View
{
    public sealed class CursorAim : IAimInput
    {
        private static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);

        private readonly IPointerInput _pointer;
        private readonly CameraService _cameraService;

        public CursorAim(IPointerInput pointer, CameraService cameraService)
        {
            _pointer = pointer;
            _cameraService = cameraService;
        }

        public bool TryGetAimPoint(out Vector3 point)
        {
            point = default;

            Camera camera = _cameraService.OutputCamera;

            if (camera == null)
                return false;

            Ray ray = camera.ScreenPointToRay(_pointer.PointerPosition);

            if (Ground.Raycast(ray, out float distance) == false)
                return false;

            point = ray.GetPoint(distance);

            return true;
        }
    }
}

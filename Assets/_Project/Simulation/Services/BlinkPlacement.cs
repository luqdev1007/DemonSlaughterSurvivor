using UnityEngine;

namespace Game.Simulation.Services
{
    public static class BlinkPlacement
    {
        private const float MinDistance = 0.05f;

        public static Vector3 Target(Vector3 origin, Vector3 aimPoint, bool hasAim, Vector3 facing, float maxDistance)
        {
            Vector3 offset = hasAim ? aimPoint - origin : facing * maxDistance;
            offset.y = 0f;

            if (offset.sqrMagnitude < MinDistance * MinDistance)
            {
                offset = facing * maxDistance;
                offset.y = 0f;
            }

            return origin + Vector3.ClampMagnitude(offset, maxDistance);
        }

        public static Vector3 ClampToArena(Vector3 point, float arenaRadius, float bodyRadius)
        {
            float limit = Mathf.Max(0f, arenaRadius - bodyRadius);
            Vector3 planar = new Vector3(point.x, 0f, point.z);

            if (planar.sqrMagnitude <= limit * limit)
                return point;

            planar = planar.normalized * limit;

            return new Vector3(planar.x, point.y, planar.z);
        }

        public static bool Overlaps(Vector3 point, float bodyRadius, Vector3 solid, float solidRadius)
        {
            float dx = point.x - solid.x;
            float dz = point.z - solid.z;
            float contact = bodyRadius + solidRadius;

            return dx * dx + dz * dz < contact * contact;
        }
    }
}

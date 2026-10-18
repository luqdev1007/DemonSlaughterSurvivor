using UnityEngine;

namespace Game.Simulation.Services
{
    public static class YawTurn
    {
        public static Vector3 Towards(Vector3 from, Vector3 to, float maxDegrees)
        {
            from.y = 0f;
            to.y = 0f;

            if (to.sqrMagnitude <= Mathf.Epsilon)
                return from;

            if (from.sqrMagnitude <= Mathf.Epsilon)
                return to.normalized;

            float angle = Vector3.SignedAngle(from, to, Vector3.up);
            float step = Mathf.Clamp(angle, -maxDegrees, maxDegrees);

            return (Quaternion.AngleAxis(step, Vector3.up) * from).normalized;
        }
    }
}

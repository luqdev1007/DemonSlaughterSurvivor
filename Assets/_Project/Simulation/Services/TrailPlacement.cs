using System;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class TrailPlacement
    {
        public static int Place(Vector3 from, Vector3 to, float spacing, ref float carried, Vector3[] output)
        {
            if (spacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Trail spacing must be positive.");

            float x = to.x - from.x;
            float z = to.z - from.z;
            float length = Mathf.Sqrt(x * x + z * z);

            if (length <= 0f)
                return 0;

            int count = 0;
            float next = spacing - carried;

            while (next <= length)
            {
                if (count == output.Length)
                    throw new InvalidOperationException(
                        $"Trail buffer holds {output.Length} points, but one tick of {length} m at spacing {spacing} m needs more; enlarge the buffer where it is created.");

                output[count] = Vector3.Lerp(from, to, next / length);
                count++;
                next += spacing;
            }

            carried = length - (next - spacing);

            return count;
        }
    }
}

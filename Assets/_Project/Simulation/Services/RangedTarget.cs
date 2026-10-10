using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class RangedTarget
    {
        public static bool TryFindLiving(EcsWorld world, EcsPackedEntity target, EcsPool<Position> positions, EcsPool<Dead> dead, out Vector3 position)
        {
            position = default;

            if (target.Unpack(world, out int entity) == false)
                return false;

            if (positions.Has(entity) == false || dead.Has(entity))
                return false;

            position = positions.Get(entity).Value;

            return true;
        }

        public static bool IsWithin(Vector3 from, Vector3 to, float range)
        {
            float deltaX = to.x - from.x;
            float deltaZ = to.z - from.z;

            return deltaX * deltaX + deltaZ * deltaZ <= range * range;
        }

        public static bool SweptHit(Vector3 boltFrom, Vector3 boltTo, Vector3 bodyFrom, Vector3 bodyTo, float contact)
        {
            float offsetX = bodyFrom.x - boltFrom.x;
            float offsetZ = bodyFrom.z - boltFrom.z;
            float driftX = (bodyTo.x - bodyFrom.x) - (boltTo.x - boltFrom.x);
            float driftZ = (bodyTo.z - bodyFrom.z) - (boltTo.z - boltFrom.z);

            float driftSquared = driftX * driftX + driftZ * driftZ;
            float closest = 0f;

            if (driftSquared > 1e-12f)
                closest = Mathf.Clamp01(-(offsetX * driftX + offsetZ * driftZ) / driftSquared);

            float gapX = offsetX + driftX * closest;
            float gapZ = offsetZ + driftZ * closest;

            return gapX * gapX + gapZ * gapZ <= contact * contact;
        }
    }
}

using Game.Simulation.Components;
using Leopotam.EcsLite;
using System;

namespace Game.Simulation.Services
{
    public static class TakenPerkLookup
    {
        public static int Find(EcsWorld world, EcsFilter taken, EcsPool<TakenPerk> pool, int owner, string perkId)
        {
            foreach (int entity in taken)
            {
                ref TakenPerk perk = ref pool.Get(entity);

                if (string.Equals(perk.PerkId, perkId, StringComparison.Ordinal) == false)
                    continue;

                if (perk.Owner.Unpack(world, out int unpacked) == false || unpacked != owner)
                    continue;

                return entity;
            }

            return -1;
        }
    }
}

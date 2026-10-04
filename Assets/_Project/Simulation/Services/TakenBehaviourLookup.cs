using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class TakenBehaviourLookup
    {
        public static bool TryFind<T>(EcsWorld world, EcsFilter taken, EcsPool<TakenPerk> pool, IContentRegistry content, int owner, out T behaviour, out int level)
            where T : PerkBehaviourConfig
        {
            foreach (int entity in taken)
            {
                ref TakenPerk perk = ref pool.Get(entity);

                if (perk.Owner.Unpack(world, out int unpacked) == false || unpacked != owner)
                    continue;

                if (content.Get<PerkConfig>(perk.PerkId).Behaviour is T found)
                {
                    behaviour = found;
                    level = perk.Level;

                    return true;
                }
            }

            behaviour = null;
            level = 0;

            return false;
        }
    }
}

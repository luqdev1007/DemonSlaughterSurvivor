using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class AbilityCooldowns
    {
        public static float Scale(EcsPool<AbilityCooldownScale> pool, int hero)
        {
            return pool.Has(hero) ? pool.Get(hero).Value : 1f;
        }
    }
}

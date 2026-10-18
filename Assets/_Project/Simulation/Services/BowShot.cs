using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class BowShot
    {
        public static bool IsBlocked(int owner, EcsPool<Dead> dead)
        {
            return dead.Has(owner);
        }
    }
}

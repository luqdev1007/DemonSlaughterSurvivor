using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class BowShot
    {
        public static bool IsBlocked(int owner, EcsPool<Dead> dead, EcsPool<Backleaping> backleaps, bool shootsInAir)
        {
            return dead.Has(owner) || (backleaps.Has(owner) && shootsInAir == false);
        }
    }
}

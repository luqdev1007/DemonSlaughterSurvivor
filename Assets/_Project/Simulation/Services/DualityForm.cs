using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class DualityForm
    {
        public static bool IsDemonic(EcsPool<Duality> dualities, int hero)
        {
            if (dualities.Has(hero) == false)
                return false;

            ref Duality duality = ref dualities.Get(hero);

            return duality.Demonic > duality.Config.FormThreshold;
        }

        public static bool IsHuman(EcsPool<Duality> dualities, int hero)
        {
            if (dualities.Has(hero) == false)
                return false;

            ref Duality duality = ref dualities.Get(hero);

            return 1f - duality.Demonic > duality.Config.FormThreshold;
        }
    }
}

using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class ConcentrationBonus
    {
        public static float Fraction(EcsPool<Concentration> concentrations, int hero)
        {
            if (concentrations.Has(hero) == false)
                return 0f;

            ref Concentration concentration = ref concentrations.Get(hero);

            return Mathf.Clamp01(concentration.Value / concentration.Config.Max);
        }

        public static float Damage(EcsPool<Concentration> concentrations, int hero)
        {
            return concentrations.Has(hero) ? 1f + concentrations.Get(hero).Config.DamageBonus * Fraction(concentrations, hero) : 1f;
        }

        public static float Area(EcsPool<Concentration> concentrations, int hero)
        {
            return concentrations.Has(hero) ? 1f + concentrations.Get(hero).Config.AreaBonus * Fraction(concentrations, hero) : 1f;
        }

        public static float Area(EcsWorld world, EcsPool<Concentration> concentrations, EcsPackedEntity owner)
        {
            return owner.Unpack(world, out int hero) ? Area(concentrations, hero) : 1f;
        }

        public static float CastSpeed(EcsPool<Concentration> concentrations, int hero)
        {
            return concentrations.Has(hero) ? 1f + concentrations.Get(hero).Config.CastSpeedBonus * Fraction(concentrations, hero) : 1f;
        }
    }
}

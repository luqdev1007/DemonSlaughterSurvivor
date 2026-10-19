using Game.Simulation.Components;
using Leopotam.EcsLite;

namespace Game.Simulation.Services
{
    public static class ElementalStatus
    {
        public static bool IsAffected(EcsPool<Burning> burning, EcsPool<Frozen> frozen, EcsPool<Slowed> slowed, EcsPool<ElementMark> marks, int entity)
        {
            return burning.Has(entity) || frozen.Has(entity) || slowed.Has(entity) || marks.Has(entity);
        }
    }
}

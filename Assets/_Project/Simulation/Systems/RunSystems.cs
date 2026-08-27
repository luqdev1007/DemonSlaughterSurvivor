using Leopotam.EcsLite;

namespace Game.Simulation.Systems
{
    public static class RunSystems
    {
        public static IEcsSystems Build(EcsWorld world)
        {
            EcsSystems systems = new EcsSystems(world);

            // 1.  Input
            // 2.  Abilities
            // 3.  Spawn
            // 4.  AI
            // 5.  Movement
            // 6.  Weapons
            // 7.  AttackLifetime
            // 8.  Collision
            // 9.  Damage
            // 10. Death
            // 11. Progression
            // 12. ViewSync
            // 13. Cleanup

            return systems;
        }
    }
}

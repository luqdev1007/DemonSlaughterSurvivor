using Leopotam.EcsLite;

namespace Game.Simulation.Systems
{
    public static class RunSystems
    {
        public static IEcsSystems Build(EcsWorld world)
        {
            EcsSystems systems = new EcsSystems(world);

            // 1.  Input
            systems.Add(new ReadMoveInputSystem());
            // 2.  Abilities
            // 3.  Spawn
            systems.Add(new SpawnPlayerSystem());
            // 4.  AI
            // 5.  Movement
            systems.Add(new ApplyMoveSpeedSystem());
            systems.Add(new FaceVelocitySystem());
            systems.Add(new MoveSystem());
            // 6.  Weapons
            // 7.  AttackLifetime
            // 8.  Collision
            // 9.  Damage
            // 10. Death
            // 11. Progression
            // 12. ViewSync
            systems.Add(new SyncViewSystem());
            // 13. Cleanup

            return systems;
        }
    }
}

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
            systems.Add(new ReadDashInputSystem());
            // 2.  Abilities
            systems.Add(new TickDashCooldownSystem());
            systems.Add(new AdvanceDashSystem());
            systems.Add(new StartDashSystem());
            systems.Add(new ExpireDashRequestSystem());
            // 3.  Spawn
            systems.Add(new SpawnPlayerSystem());
            systems.Add(new SpawnWaveSystem());
            // 4.  AI
            systems.Add(new AcquireChaseTargetSystem());
            systems.Add(new ChaseTargetSystem());
            systems.Add(new SeparateNeighborsSystem());
            // 5.  Movement
            systems.Add(new StorePreviousPositionSystem());
            systems.Add(new ApplyDashVelocitySystem());
            systems.Add(new ApplyMoveSpeedSystem());
            systems.Add(new FaceVelocitySystem());
            systems.Add(new ApplyPushSystem());
            systems.Add(new ResolveBodyOverlapSystem());
            systems.Add(new MoveSystem());
            // 6.  Weapons
            systems.Add(new DebugDamageSystem());
            // 7.  AttackLifetime
            // 8.  Collision
            systems.Add(new SweepDashPushSystem());
            systems.Add(new DetectContactDamageSystem());
            // 9.  Damage
            systems.Add(new TickInvulnerabilitySystem());
            systems.Add(new ApplyDamageSystem());
            // 10. Death
            systems.Add(new MarkDeadSystem());
            systems.Add(new TickPendingFinishSystem());
            systems.Add(new FinishRunOnPlayerDeathSystem());
            systems.Add(new ReapDeadEnemiesSystem());
            // 11. Progression
            // 12. ViewSync
            systems.Add(new SyncViewSystem());
            systems.Add(new PlayHitFeedbackSystem());
            systems.Add(new PlayDeathFeedbackSystem());
            systems.Add(new SyncInvulnerabilityViewSystem());
            systems.Add(new PublishPlayerVitalsSystem());
            systems.Add(new BindCameraTargetSystem());
            // 13. Cleanup
            systems.Add(new CleanupEventsSystem());
            systems.Add(new RebuildSpatialGridSystem());

            return systems;
        }
    }
}

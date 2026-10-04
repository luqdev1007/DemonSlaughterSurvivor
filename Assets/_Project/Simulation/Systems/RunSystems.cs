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
            systems.Add(new ReadUltimateInputSystem());
            // 2.  Abilities
            systems.Add(new TickDashCooldownSystem());
            systems.Add(new AdvanceDashSystem());
            systems.Add(new StartDashSystem());
            systems.Add(new ExpireDashRequestSystem());
            systems.Add(new TickBerserkModeSystem());
            systems.Add(new StartBerserkSystem());
            systems.Add(new ExpireUltimateRequestSystem());
            // 3.  Spawn
            systems.Add(new SpawnPlayerSystem());
            systems.Add(new EquipStartingWeaponSystem());
            systems.Add(new SpawnWaveSystem());
            systems.Add(new RecomputeStatsSystem());
            // 4.  AI
            systems.Add(new AcquireChaseTargetSystem());
            systems.Add(new ChaseTargetSystem());
            systems.Add(new SeparateNeighborsSystem());
            // 5.  Movement
            systems.Add(new StorePreviousPositionSystem());
            systems.Add(new ApplyDashVelocitySystem());
            systems.Add(new ApplyMoveSpeedSystem());
            systems.Add(new FaceVelocitySystem());
            systems.Add(new FaceNearestEnemySystem());
            systems.Add(new ApplyPushSystem());
            systems.Add(new ApplyGemFlightSystem());
            systems.Add(new ResolveBodyOverlapSystem());
            systems.Add(new MoveSystem());
            // 6.  Weapons
            systems.Add(new TickWeaponCooldownSystem());
            systems.Add(new InterruptSwingSystem());
            systems.Add(new ExpireSpecialAttackSystem());
            systems.Add(new StartSwingSystem());
            // 7.  AttackLifetime
            systems.Add(new AdvanceSwingSystem());
            systems.Add(new AdvanceHeroicLeapSystem());
            // 8.  Collision
            systems.Add(new SweepDashPushSystem());
            systems.Add(new DetectContactDamageSystem());
            systems.Add(new StartGemFlightSystem());
            // 9.  Damage
            systems.Add(new TickInvulnerabilitySystem());
            systems.Add(new ApplyDamageSystem());
            // 10. Death
            systems.Add(new MarkDeadSystem());
            systems.Add(new DropGemsSystem());
            systems.Add(new TickPendingFinishSystem());
            systems.Add(new FinishRunOnPlayerDeathSystem());
            systems.Add(new ReapDeadEnemiesSystem());
            // 11. Progression
            systems.Add(new TriggerCounterStrikeSystem());
            systems.Add(new AccumulateRageSystem());
            systems.Add(new DecayRageSystem());
            systems.Add(new DebugLevelUpSystem());
            systems.Add(new CollectGemsSystem());
            systems.Add(new AdvanceLevelSystem());
            systems.Add(new OfferUpgradesSystem());
            // 12. ViewSync
            systems.Add(new SyncViewSystem());
            systems.Add(new SyncGemViewSystem());
            systems.Add(new PlayHitFeedbackSystem());
            systems.Add(new PlaySwingFeedbackSystem());
            systems.Add(new PlayDeathFeedbackSystem());
            systems.Add(new SyncInvulnerabilityViewSystem());
            systems.Add(new SyncBerserkViewSystem());
            systems.Add(new SyncDashViewSystem());
            systems.Add(new SyncSpecialAttackViewSystem());
            systems.Add(new SyncLocomotionViewSystem());
            systems.Add(new PublishPlayerVitalsSystem());
            systems.Add(new PublishExperienceSystem());
            systems.Add(new BindCameraTargetSystem());
            // 13. Cleanup
            systems.Add(new CleanupEventsSystem());
            systems.Add(new SweepOrphanModifiersSystem());
            systems.Add(new SweepOrphanOwnedSystem());
            systems.Add(new RebuildSpatialGridSystem());

            return systems;
        }

        public static IEcsSystems BuildChoice(EcsWorld world)
        {
            EcsSystems systems = new EcsSystems(world);

            // Runs instead of the 13 groups while the upgrade choice holds the world.
            systems.Add(new ApplyUpgradeChoiceSystem());

            return systems;
        }
    }
}

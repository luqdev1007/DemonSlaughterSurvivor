using Leopotam.EcsLite;

namespace Game.Simulation.Systems
{
    public static class RunSystems
    {
        public static IEcsSystems Build(EcsWorld world)
        {
            EcsSystems systems = new EcsSystems(world);

            // 1.  Input
            systems.Add(new ReadInputStateSystem());
            systems.Add(new ReadDashInputSystem());
            systems.Add(new ReadUltimateInputSystem());
            // 2.  Abilities
            systems.Add(new TickDashCooldownSystem());
            systems.Add(new AdvanceDashSystem());
            systems.Add(new StartDashSystem());
            systems.Add(new ExpireDashRequestSystem());
            systems.Add(new TickBerserkModeSystem());
            systems.Add(new TickPowerUpSystem());
            systems.Add(new StartBerserkSystem());
            systems.Add(new ExpireUltimateRequestSystem());
            // 3.  Spawn
            systems.Add(new SpawnPlayerSystem());
            systems.Add(new SpawnBreakablesSystem());
            systems.Add(new EquipStartingWeaponSystem());
            systems.Add(new SpawnWaveSystem());
            systems.Add(new ApplyLowHealthFurySystem());
            systems.Add(new RecomputeStatsSystem());
            // 4.  AI
            systems.Add(new AcquireChaseTargetSystem());
            systems.Add(new ChaseTargetSystem());
            systems.Add(new AdvanceEnemyChargeSystem());
            systems.Add(new AdvanceEnemySpinSystem());
            systems.Add(new AdvanceEnemyHeavyStrikeSystem());
            systems.Add(new BossDecideSystem());
            systems.Add(new HoldForEnemyActionSystem());
            systems.Add(new HoldForRangedAttackSystem());
            systems.Add(new HoldStaggeredSystem());
            systems.Add(new SeparateNeighborsSystem());
            // 5.  Movement
            systems.Add(new StorePreviousPositionSystem());
            systems.Add(new ApplyDashVelocitySystem());
            systems.Add(new ApplyEnemyChargeVelocitySystem());
            systems.Add(new ApplyMoveSpeedSystem());
            systems.Add(new SteerEnemySpinSystem());
            systems.Add(new FaceVelocitySystem());
            systems.Add(new FaceNearestEnemySystem());
            systems.Add(new FaceExecutionTargetSystem());
            systems.Add(new FaceRangedTargetSystem());
            systems.Add(new FaceEnemyChargeTargetSystem());
            systems.Add(new FaceEnemyHeavyStrikeTargetSystem());
            systems.Add(new FaceAimSystem());
            systems.Add(new AssistSwingFacingSystem());
            systems.Add(new ApplyPushSystem());
            systems.Add(new ApplyGemFlightSystem());
            systems.Add(new ResolveBodyOverlapSystem());
            systems.Add(new MoveSystem());
            // 6.  Weapons
            systems.Add(new SpawnFireTrailSystem());
            systems.Add(new TickWeaponCooldownSystem());
            systems.Add(new StartHeroicLeapSystem());
            systems.Add(new InterruptSwingSystem());
            systems.Add(new ExpireSpecialAttackSystem());
            systems.Add(new StartDashFinisherSystem());
            systems.Add(new StartExecutionSystem());
            systems.Add(new ApplyExecutionThirstSystem());
            systems.Add(new StartCounterStrikeSystem());
            systems.Add(new StartSwingSystem());
            systems.Add(new ReleaseRangedAttackSystem());
            systems.Add(new StartRangedAttackSystem());
            // 7.  AttackLifetime
            systems.Add(new AdvanceSwingSystem());
            systems.Add(new GrantHarvestHealSystem());
            systems.Add(new AdvanceHeroicLeapSystem());
            systems.Add(new AdvanceExecutionSystem());
            systems.Add(new AdvanceDashFinisherSystem());
            systems.Add(new ResolveAreaStrikeSystem());
            systems.Add(new PulseFireSegmentsSystem());
            systems.Add(new TickFireSegmentsSystem());
            systems.Add(new AdvanceProjectilesSystem());
            systems.Add(new PulseEnemySpinSystem());
            systems.Add(new ImpactEnemyHeavyStrikeSystem());
            // 8.  Collision
            systems.Add(new SweepDashPushSystem());
            systems.Add(new SweepSlicingDashSystem());
            systems.Add(new SweepEnemyChargeSystem());
            systems.Add(new InterruptCastSystem());
            systems.Add(new PunishLeapLandingSystem());
            systems.Add(new DetectContactDamageSystem());
            systems.Add(new ApplyBossBlindZoneSystem());
            systems.Add(new StartGemFlightSystem());
            // 9.  Damage
            systems.Add(new TickInvulnerabilitySystem());
            systems.Add(new TickProjectilePassThroughSystem());
            systems.Add(new ApplyDamageSystem());
            // 10. Death
            systems.Add(new MarkDeadSystem());
            systems.Add(new DropGemsSystem());
            systems.Add(new DropHealOrbsSystem());
            systems.Add(new TickPendingFinishSystem());
            systems.Add(new FinishRunOnPlayerDeathSystem());
            systems.Add(new FinishRunOnBossDeathSystem());
            systems.Add(new DebugVictorySystem());
            systems.Add(new GrantLifeStealSystem());
            systems.Add(new ReapDeadEnemiesSystem());
            // 11. Progression
            systems.Add(new TriggerCounterStrikeSystem());
            systems.Add(new AccumulateRageSystem());
            systems.Add(new DecayRageSystem());
            systems.Add(new DebugLevelUpSystem());
            systems.Add(new CollectGemsSystem());
            systems.Add(new CollectHealOrbsSystem());
            systems.Add(new ApplyHealSystem());
            systems.Add(new AdvanceLevelSystem());
            systems.Add(new OfferUpgradesSystem());
            // 12. ViewSync
            systems.Add(new SyncViewSystem());
            systems.Add(new SyncGemViewSystem());
            systems.Add(new PlayHitFeedbackSystem());
            systems.Add(new PlaySwingFeedbackSystem());
            systems.Add(new PlayLeapFeedbackSystem());
            systems.Add(new PlayExecutionFeedbackSystem());
            systems.Add(new PlayDashFinisherFeedbackSystem());
            systems.Add(new TickLeapImpactFxSystem());
            systems.Add(new SpawnLeapImpactFxSystem());
            systems.Add(new SyncLeapLiftViewSystem());
            systems.Add(new SyncPushLiftViewSystem());
            systems.Add(new PlayRangedAttackFeedbackSystem());
            systems.Add(new PlayCastInterruptFeedbackSystem());
            systems.Add(new PlayDeathFeedbackSystem());
            systems.Add(new SyncInvulnerabilityViewSystem());
            systems.Add(new SyncBerserkViewSystem());
            systems.Add(new SyncPowerUpViewSystem());
            systems.Add(new SyncDashViewSystem());
            systems.Add(new SyncSpecialAttackViewSystem());
            systems.Add(new SyncLocomotionViewSystem());
            systems.Add(new SyncEnemyActionViewSystem());
            systems.Add(new PublishPlayerVitalsSystem());
            systems.Add(new PublishAbilityPanelSystem());
            systems.Add(new PublishBossVitalsSystem());
            systems.Add(new PublishExperienceSystem());
            systems.Add(new BindCameraTargetSystem());
            // 13. Cleanup
            systems.Add(new ReapBreakablesSystem());
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

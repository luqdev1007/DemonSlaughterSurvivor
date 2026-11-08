using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class DemonLordBossTests
    {
        private const float Tick = 1f / 60f;

        [Test]
        public void BossDecideDoesNotRequireUnassignedAbilities()
        {
            EnemyConfig enemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/_Project/Configs/Enemies/Enemy_DemonLord.asset");
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>("Assets/_Project/Configs/ContentDatabase.asset");
            EcsWorld world = new EcsWorld();
            EcsSystems systems = new EcsSystems(world);

            try
            {
                Assert.NotNull(enemy);
                Assert.IsNull(enemy.Charge);
                Assert.IsNull(enemy.Spin);
                Assert.IsNull(enemy.HeavyStrike);
                Assert.IsNull(enemy.Jump);

                int hero = world.NewEntity();
                world.GetPool<Player>().Add(hero);
                world.GetPool<Position>().Add(hero).Value = Vector3.forward * 10f;

                int demon = world.NewEntity();
                world.GetPool<Boss>().Add(demon).Config = enemy.Boss;
                world.GetPool<EnemyAction>().Add(demon);
                world.GetPool<ChaseTarget>().Add(demon).Value = world.PackEntity(hero);
                world.GetPool<Position>().Add(demon).Value = Vector3.zero;
                world.GetPool<Facing>().Add(demon).Value = Vector3.forward;

                SimulationClock clock = new SimulationClock();
                RunContext context = new RunContext("level.troll_lair", "character.test", RunMode.Story, 1, Array.Empty<StatModifierSpec>());
                systems.Add(new BossDecideSystem());
                systems.Inject(new ContentRegistry(database.Entries), context, clock);
                Assert.DoesNotThrow(systems.Init);

                for (int tick = 0; tick < 120; tick++)
                {
                    clock.Advance(Tick);
                    systems.Run();
                }

                Assert.AreEqual(EnemyActionPhase.None, world.GetPool<EnemyAction>().Get(demon).Phase);
            }
            finally
            {
                systems.Destroy();
                world.Destroy();
            }
        }

        [Test]
        public void DemonLordDeathReportsBossIdAndWinsAfterDeathDelay()
        {
            BossConfig boss = AssetDatabase.LoadAssetAtPath<BossConfig>("Assets/_Project/Configs/Enemies/Boss_DemonLord.asset");
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Configs/Levels/Level_TrollLair.asset");
            EcsWorld world = new EcsWorld();
            EcsSystems systems = new EcsSystems(world);

            try
            {
                int hero = world.NewEntity();
                world.GetPool<Player>().Add(hero);

                int demon = world.NewEntity();
                world.GetPool<Boss>().Add(demon).Config = boss;
                world.GetPool<DiedEvent>().Add(demon);

                SimulationClock clock = new SimulationClock();
                RunOutcome outcome = new RunOutcome();
                BossLog stats = new BossLog();
                systems.Add(new FinishRunOnBossDeathSystem());
                systems.Inject(outcome, clock, level, (IRunStatsSink)stats);
                systems.Init();

                clock.Advance(Tick);
                systems.Run();
                CollectionAssert.AreEqual(new[] { "boss.demon_lord" }, stats.BossesKilled);
                Assert.IsFalse(outcome.IsFinished);

                world.DelEntity(demon);

                for (int tick = 0; tick < Mathf.RoundToInt(boss.DeathSeconds / Tick); tick++)
                {
                    clock.Advance(Tick);
                    systems.Run();
                }

                Assert.AreEqual(RunEndReason.Victory, outcome.Reason);
            }
            finally
            {
                systems.Destroy();
                world.Destroy();
            }
        }

        private sealed class BossLog : IRunStatsSink
        {
            public readonly List<string> BossesKilled = new List<string>();

            public void RecordDamageTaken(float amount) { }
            public void RecordGemCollected() { }
            public void RecordEnemyKilled() { }
            public void RecordItemFound() { }
            public void RecordItemTaken(int rarity) { }
            public void RecordDamageDealt(float amount, float effective) { }
            public void RecordEliteKilled() { }
            public void RecordBreakableDestroyed() { }
            public void RecordBossKilled(string bossId) => BossesKilled.Add(bossId);
        }
    }
}

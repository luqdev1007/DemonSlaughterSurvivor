using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class BossVitalsPublishTests
    {
        [Test]
        public void TheBossPositionAndFlatDistanceToTheHeroArePublished()
        {
            BossConfig config = ScriptableObject.CreateInstance<BossConfig>();
            EcsWorld world = new EcsWorld();
            RecordingSink sink = new RecordingSink();

            try
            {
                int boss = world.NewEntity();
                world.GetPool<Boss>().Add(boss).Config = config;
                world.GetPool<Health>().Add(boss).Current = 300f;
                world.GetPool<MaxHealth>().Add(boss).Value = 500f;
                world.GetPool<Position>().Add(boss).Value = new Vector3(3f, 0f, 4f);

                int hero = world.NewEntity();
                world.GetPool<Player>().Add(hero);
                world.GetPool<Position>().Add(hero).Value = new Vector3(0f, 1f, 0f);

                EcsSystems systems = new EcsSystems(world);
                systems.Add(new PublishBossVitalsSystem());
                systems.Inject((IBossVitalsSink)sink);
                systems.Init();
                systems.Run();

                Assert.IsTrue(sink.Visible);
                Assert.AreEqual(new Vector3(3f, 0f, 4f), sink.Position);
                Assert.AreEqual(5f, sink.Distance, 1e-5f, "height does not count");

                world.GetPool<Dead>().Add(boss);
                systems.Run();

                Assert.IsFalse(sink.Visible);

                systems.Destroy();
            }
            finally
            {
                world.Destroy();
                Object.DestroyImmediate(config);
            }
        }

        private sealed class RecordingSink : IBossVitalsSink
        {
            public bool Visible;
            public Vector3 Position;
            public float Distance;

            public void Publish(bool visible, string displayName, float health, float maxHealth, Vector3 position, float distanceToHero)
            {
                Visible = visible;
                Position = position;
                Distance = distanceToHero;
            }
        }
    }
}

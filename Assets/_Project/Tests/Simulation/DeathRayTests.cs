using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Game.Simulation.Systems;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class DeathRayTests
    {
        private const float Tick = 1f / 60f;
        private const float ChargeSeconds = 0.6f;
        private const float BeamSeconds = 1f;
        private const float InitialDamage = 25f;
        private const float TickDamage = 6f;
        private const float CooldownSeconds = 12f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private RecordingView _heroView;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            GameObject fx = new GameObject("Fx");
            _assets.Add(fx);

            DeathRayConfig ray = Asset<DeathRayConfig>();
            DeathRayLevel level = new DeathRayLevel();
            Set(level, "_cooldownSeconds", CooldownSeconds);
            Set(level, "_initialDamage", InitialDamage);
            Set(level, "_tickDamage", TickDamage);
            Set(ray, "_levels", new[] { level });
            Set(ray, "_chargeSeconds", ChargeSeconds);
            Set(ray, "_maxBeamSeconds", 3f);
            Set(ray, "_tickSeconds", 0.1f);
            Set(ray, "_length", 16f);
            Set(ray, "_halfWidth", 0.45f);
            Set(ray, "_beamPrefab", fx);

            PerkConfig perk = Asset<PerkConfig>();
            Set(perk, "_id", "perk.death-ray");
            Set(perk, "_behaviour", ray);
            Set(perk, "_levels", new[] { new PerkLevel() });

            TorsoAimConfig aim = Asset<TorsoAimConfig>();

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            _systems = new EcsSystems(_world);
            _systems.Add(new HoldChannelingSystem());
            _systems.Add(new DeathRaySystem());
            _systems.Inject(_clock, (IViewFactory)_views, (IContentRegistry)new ContentRegistry(new IContentEntry[] { perk }));
            _systems.Init();

            _heroView = new RecordingView();
            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);
            _world.GetPool<Facing>().Add(_hero).Value = Vector3.forward;
            _world.GetPool<MoveIntent>().Add(_hero);
            _world.GetPool<View>().Add(_hero).Value = _heroView;
            _world.GetPool<InputState>().Add(_hero).StandHeld = true;

            ref TorsoAim torso = ref _world.GetPool<TorsoAim>().Add(_hero);
            torso.Config = aim;
            torso.Direction = Vector3.forward;
            torso.Desired = Vector3.forward;

            ref TakenPerk taken = ref _world.GetPool<TakenPerk>().Add(_world.NewEntity());
            taken.Owner = _world.PackEntity(_hero);
            taken.PerkId = perk.Id;
            taken.Level = 1;
        }

        [TearDown]
        public void TearDown()
        {
            _systems.Destroy();
            _world.Destroy();

            foreach (Object asset in _assets)
                Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void TheRayChargesThenBurnsEverythingOnItsLineAndNothingBeside()
        {
            int near = Enemy(new Vector3(0f, 0f, 4f));
            int far = Enemy(new Vector3(0.3f, 0f, 14f));
            int beside = Enemy(new Vector3(3f, 0f, 6f));
            Target(near);

            Step(1);

            CollectionAssert.AreEqual(new[] { "CastRay" }, _heroView.Triggers);
            Assert.IsTrue(_world.GetPool<Channeling>().Has(_hero), "the mage holds still while he channels");

            Step(Mathf.RoundToInt(ChargeSeconds / Tick) - 2);

            Assert.AreEqual(0f, Total(near), "no damage while the energy gathers");

            Step(Mathf.RoundToInt((ChargeSeconds + BeamSeconds) / Tick) - Mathf.RoundToInt(ChargeSeconds / Tick) + 2);

            Assert.IsTrue(_world.GetPool<Channeling>().Has(_hero), "the beam burns on while the button is held");

            _world.GetPool<InputState>().Get(_hero).StandHeld = false;

            Step(1);

            float expected = InitialDamage + TickDamage * 10f;
            TestContext.WriteLine($"near {Total(near)}, far {Total(far)}, beside {Total(beside)}");

            Assert.AreEqual(expected, Total(near), TickDamage + 1e-3f, "the first burst and ten ticks a second");
            Assert.AreEqual(Total(near), Total(far), 1e-3f, "the ray passes through the first enemy to the far one");
            Assert.AreEqual(0f, Total(beside));
            Assert.IsFalse(_world.GetPool<Channeling>().Has(_hero), "the mage is free again");
            Assert.AreEqual("CastCancel", _heroView.Triggers[_heroView.Triggers.Count - 1]);
        }

        [Test]
        public void TheChannelHoldsTheMageInPlace()
        {
            int enemy = Enemy(new Vector3(0f, 0f, 4f));
            Target(enemy);

            Step(1);

            _world.GetPool<MoveIntent>().Get(_hero).Value = Vector3.right;

            Step(1);

            Assert.AreEqual(Vector3.zero, _world.GetPool<MoveIntent>().Get(_hero).Value);
        }

        [Test]
        public void NoRayUnlessTheMageStandsWithTheButtonHeld()
        {
            _world.GetPool<InputState>().Get(_hero).StandHeld = false;

            Step(30);

            _world.GetPool<InputState>().Get(_hero).StandHeld = true;
            _world.GetPool<InputState>().Get(_hero).MoveRequested = true;

            Step(30);

            Assert.AreEqual(0, _heroView.Triggers.Count);
            Assert.AreEqual(0, _world.Filter<DeathRay>().End().GetEntitiesCount());
        }

        [Test]
        public void MovingBreaksTheChannelAndStartsTheCooldown()
        {
            Step(Mathf.RoundToInt(ChargeSeconds / Tick) + 10);

            _world.GetPool<InputState>().Get(_hero).MoveRequested = true;

            Step(1);

            Assert.IsFalse(_world.GetPool<Channeling>().Has(_hero));
            Assert.AreEqual(0, _world.Filter<DeathRay>().End().GetEntitiesCount());
            Assert.IsTrue(_world.GetPool<DeathRayCooldown>().Has(_hero));
        }

        [Test]
        public void TheBeamStopsByItselfAfterItsLongestChannel()
        {
            Step(Mathf.RoundToInt((ChargeSeconds + 3f) / Tick) + 2);

            Assert.IsFalse(_world.GetPool<Channeling>().Has(_hero));
            Assert.IsTrue(_world.GetPool<DeathRayCooldown>().Has(_hero));
        }

        private void Target(int enemy)
        {
            ref TorsoAim aim = ref _world.GetPool<TorsoAim>().Get(_hero);
            aim.HasTarget = true;
            aim.Target = _world.PackEntity(enemy);
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<BodyRadius>().Add(enemy).Value = 0.4f;

            return enemy;
        }

        private float Total(int target)
        {
            float total = 0f;
            EcsPool<DamageEvent> pool = _world.GetPool<DamageEvent>();

            foreach (int entity in _world.Filter<DamageEvent>().End())
            {
                DamageEvent hit = pool.Get(entity);

                if (hit.Target.Unpack(_world, out int hitEntity) && hitEntity == target)
                    total += hit.Amount;
            }

            return total;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);

            return asset;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}

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
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class HunterMarkTests
    {
        private const float Tick = 1f / 60f;
        private const int Targets = 3;
        private const float Duration = 2f;
        private const float DamageBonus = 0.3f;
        private const float CritBonus = 0.15f;

        private readonly List<Object> _assets = new List<Object>();
        private readonly SimulationClock _clock = new SimulationClock();

        private EcsWorld _world;
        private EcsSystems _systems;
        private RecordingViewFactory _views;
        private int _hero;

        [SetUp]
        public void SetUp()
        {
            HunterMarkConfig config = ScriptableObject.CreateInstance<HunterMarkConfig>();
            _assets.Add(config);

            HunterMarkLevel level = new HunterMarkLevel();
            Set(level, "_targets", Targets);
            Set(level, "_durationSeconds", Duration);
            Set(level, "_damageTakenBonus", DamageBonus);
            Set(level, "_critChanceBonus", CritBonus);
            Set(config, "_levels", new[] { level });
            Set(config, "_radius", 7f);

            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            _assets.Add(perk);
            Set(perk, "_id", "perk.hunter-mark");
            Set(perk, "_behaviour", config);
            Set(perk, "_levels", new[] { new PerkLevel() });

            _world = new EcsWorld();
            _views = new RecordingViewFactory();

            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { perk });
            RunContext context = new RunContext("level.test", "character.test", RunMode.Story, 3, Array.Empty<StatModifierSpec>());

            _systems = new EcsSystems(_world);
            _systems.Add(new MarkOnBackleapSystem());
            _systems.Add(new TickHunterMarksSystem());
            _systems.Add(new ResolveHitOutcomeSystem());
            _systems.Inject(_clock, (IContentRegistry)registry, (IViewFactory)_views, context);
            _systems.Init();

            _hero = _world.NewEntity();
            _world.GetPool<Player>().Add(_hero);
            _world.GetPool<Position>().Add(_hero);

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
        public void TheBackleapMarksTheNearestEnemiesInsideTheRadius()
        {
            int a = Enemy(new Vector3(1f, 0f, 0f));
            int b = Enemy(new Vector3(0f, 0f, 2f));
            int c = Enemy(new Vector3(-3f, 0f, 0f));
            int d = Enemy(new Vector3(0f, 0f, -5f));
            int outside = Enemy(new Vector3(9f, 0f, 0f));

            Leap(BackleapPhase.Leap);
            Step(1);

            EcsPool<HunterMark> marks = _world.GetPool<HunterMark>();

            Assert.IsTrue(marks.Has(a) && marks.Has(b) && marks.Has(c), "the three nearest are marked");
            Assert.IsFalse(marks.Has(d), "only three targets at this level");
            Assert.IsFalse(marks.Has(outside));
            Assert.AreEqual(Targets, _views.Created, "one mark effect per marked enemy");
        }

        [Test]
        public void AMarkExpiresWithItsEffect()
        {
            int enemy = Enemy(new Vector3(1f, 0f, 0f));

            Leap(BackleapPhase.Hop);
            Step(1);
            _world.GetPool<Backleaping>().Del(_hero);

            Step(Mathf.RoundToInt(Duration / Tick));

            Assert.IsFalse(_world.GetPool<HunterMark>().Has(enemy));
            Assert.AreEqual(_views.Created, _views.Released, "the mark effect is released with the mark");
        }

        [Test]
        public void AMarkedEnemyTakesMoreDamageAndIsEasierToCrit()
        {
            int enemy = Enemy(new Vector3(1f, 0f, 0f));

            Leap(BackleapPhase.Leap);
            Step(1);

            int hit = _world.NewEntity();
            ref DamageEvent damage = ref _world.GetPool<DamageEvent>().Add(hit);
            damage.Target = _world.PackEntity(enemy);
            damage.Amount = 10f;

            Step(1);

            ref DamageEvent resolved = ref _world.GetPool<DamageEvent>().Get(hit);
            Assert.AreEqual(10f * (1f + DamageBonus), resolved.Amount, 1e-4f);
            Assert.AreEqual(CritBonus, resolved.CritChanceBonus, 1e-6f);
        }

        [Test]
        public void ALaterPhaseOfTheSameBackleapDoesNotMarkAgain()
        {
            Enemy(new Vector3(1f, 0f, 0f));

            Leap(BackleapPhase.Fall);
            Step(1);

            Assert.AreEqual(0, _world.Filter<HunterMark>().End().GetEntitiesCount());
        }

        private void Leap(BackleapPhase phase)
        {
            ref Backleaping leap = ref _world.GetPool<Backleaping>().Add(_hero);
            leap.Phase = phase;
            leap.Ticks = 0;
            leap.PhaseTicks = 10;
        }

        private int Enemy(Vector3 position)
        {
            int enemy = _world.NewEntity();
            _world.GetPool<Enemy>().Add(enemy);
            _world.GetPool<Position>().Add(enemy).Value = position;
            _world.GetPool<Health>().Add(enemy).Current = 100f;

            return enemy;
        }

        private void Step(int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                _clock.Advance(Tick);
                _systems.Run();
            }
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");
            field.SetValue(target, value);
        }
    }
}

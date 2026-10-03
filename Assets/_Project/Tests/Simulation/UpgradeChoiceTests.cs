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

namespace Game.Simulation.Tests
{
    public sealed class UpgradeChoiceTests
    {
        private const int Seed = 987654321;
        private const float BaseMoveSpeed = 5f;
        private const float BaseWeaponDamage = 20f;
        private const float HeroMaxHealth = 100f;

        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private readonly List<Fixture> _fixtures = new List<Fixture>();

        [TearDown]
        public void TearDown()
        {
            foreach (Fixture fixture in _fixtures)
                fixture.Dispose();

            _fixtures.Clear();

            foreach (ScriptableObject asset in _assets)
                UnityEngine.Object.DestroyImmediate(asset);

            _assets.Clear();
        }

        [Test]
        public void RetakingRaisesTheLevelInsteadOfStacking()
        {
            Fixture fixture = Build(Seed, Perk("perk.move", 5, StatId.MoveSpeed, StatOp.Increased, 0.1f));

            fixture.Take("perk.move");
            fixture.Take("perk.move");
            fixture.Step();

            Assert.AreEqual(1, fixture.CountTakenPerks(), "a second take must reuse the taken perk");
            Assert.AreEqual(2, fixture.LevelOf("perk.move"));
            Assert.AreEqual(1, fixture.CountModifiers("perk.move"), "the level 2 modifier must replace the level 1 one");
            Assert.AreEqual(BaseMoveSpeed * 1.2f, fixture.MoveSpeed(), 1e-5f, "level 2 is +20% in total, not +10% twice on top of +20%");
        }

        [Test]
        public void TakeOrderDoesNotChangeTheTotals()
        {
            PerkConfig increased = Perk("perk.a-increased", 5, StatId.MoveSpeed, StatOp.Increased, 0.1f);
            PerkConfig more = Perk("perk.b-more", 5, StatId.MoveSpeed, StatOp.More, 0.3f);
            PerkConfig damage = Perk("perk.c-damage", 5, StatId.WeaponDamage, StatOp.Increased, 0.2f);

            Fixture forward = Build(Seed, increased, more, damage);
            forward.Take("perk.a-increased");
            forward.Take("perk.b-more");
            forward.Take("perk.c-damage");
            forward.Take("perk.a-increased");
            forward.Step();

            Fixture backward = Build(Seed, increased, more, damage);
            backward.Take("perk.c-damage");
            backward.Take("perk.a-increased");
            backward.Take("perk.a-increased");
            backward.Take("perk.b-more");
            backward.Step();

            Assert.AreEqual(forward.MoveSpeed(), backward.MoveSpeed(), "move speed must be bit-equal whatever the take order");
            Assert.AreEqual(forward.WeaponDamage(), backward.WeaponDamage(), "weapon damage must be bit-equal whatever the take order");
            Assert.AreEqual(BaseWeaponDamage * 1.2f, forward.WeaponDamage(), 1e-5f, "a hero perk on WeaponDamage must reach the weapon through OwnerLink");
        }

        [Test]
        public void PerkAtMaxLevelIsNotOffered()
        {
            Fixture fixture = Build(Seed,
                Perk("perk.once", 1, StatId.MoveSpeed, StatOp.Increased, 0.1f),
                Perk("perk.five", 5, StatId.MaxHealth, StatOp.Flat, 10f));

            fixture.Take("perk.once");

            for (int level = 1; level <= 5; level++)
            {
                fixture.LevelUp();

                Assert.AreEqual(1, fixture.LastOfferCount, $"only perk.five may be offered on level up {level}");
                Assert.AreEqual("perk.five", fixture.LastOffer(0).PerkId);
                Assert.AreEqual(level, fixture.LastOffer(0).NextLevel);

                fixture.Choose(0);
            }

            Assert.AreEqual(5, fixture.LevelOf("perk.five"));
        }

        [Test]
        public void EmptyPoolSkipsTheChoiceWithoutPausing()
        {
            Fixture fixture = Build(Seed, Perk("perk.once", 1, StatId.MoveSpeed, StatOp.Increased, 0.1f));

            fixture.Take("perk.once");

            int shows = fixture.Sink.Shows.Count;

            fixture.LevelUp();

            Assert.IsFalse(fixture.Gate.IsAwaiting, "nothing to offer, so the world must keep ticking");
            Assert.IsFalse(fixture.HasPendingChoice());
            Assert.AreEqual(0, fixture.PendingLevelUps());
            Assert.AreEqual(shows, fixture.Sink.Shows.Count);
        }

        [Test]
        public void OffersAreDeterministicOnOneSeed()
        {
            PerkConfig[] perks = FivePerks();

            List<string> first = Sequence(Build(Seed, perks), 12);
            List<string> second = Sequence(Build(Seed, perks), 12);
            List<string> other = Sequence(Build(Seed + 1, perks), 12);

            CollectionAssert.AreEqual(first, second, "the same seed must offer the same perks in the same slots");
            CollectionAssert.AreNotEqual(first, other, "a different seed is expected to change the offers");
        }

        [Test]
        public void OfferCountFollowsThePool()
        {
            PerkConfig[] perks = FivePerks();

            for (int size = 1; size <= perks.Length; size++)
            {
                PerkConfig[] pool = new PerkConfig[size];
                Array.Copy(perks, pool, size);

                Fixture fixture = Build(Seed, pool);
                fixture.LevelUp();

                Assert.AreEqual(Math.Min(size, 3), fixture.LastOfferCount, $"pool of {size}");
                Assert.AreEqual(Math.Min(size, 3), fixture.World.GetPool<PendingChoice>().Get(fixture.Hero).Count);
            }
        }

        [Test]
        public void LevelUpOnTheDeathTickOpensNoChoice()
        {
            Fixture fixture = Build(Seed, FivePerks());

            fixture.World.GetPool<Dead>().Add(fixture.Hero);
            fixture.LevelUp();

            Assert.IsFalse(fixture.HasPendingChoice(), "a dead hero must not be offered a perk");
            Assert.IsFalse(fixture.Gate.IsAwaiting, "the world must not pause on the death tick");
            Assert.AreEqual(0, fixture.Sink.Shows.Count);
            Assert.IsFalse(fixture.World.GetPool<LevelUpEvent>().Has(fixture.Hero), "the event must be cleaned up even on a dead hero");
        }

        [Test]
        public void IndexOutsideTheOffersKeepsTheChoiceOpen()
        {
            Fixture fixture = Build(Seed,
                Perk("perk.a", 5, StatId.MoveSpeed, StatOp.Increased, 0.1f),
                Perk("perk.b", 5, StatId.MaxHealth, StatOp.Flat, 10f));

            fixture.LevelUp();

            Assert.AreEqual(2, fixture.LastOfferCount);

            fixture.Choose(2);

            Assert.IsTrue(fixture.Gate.IsAwaiting, "key 3 with two offers must be ignored");
            Assert.IsTrue(fixture.HasPendingChoice());

            fixture.Choose(1);

            Assert.IsFalse(fixture.Gate.IsAwaiting);
            Assert.AreEqual(1, fixture.Sink.Hides);
        }

        [Test]
        public void PressBeforeTheOfferIsDropped()
        {
            Fixture fixture = Build(Seed, FivePerks());

            fixture.Input.Index = 0;
            fixture.LevelUp();

            Assert.IsTrue(fixture.Gate.IsAwaiting);

            fixture.Step();

            Assert.IsTrue(fixture.Gate.IsAwaiting, "a key pressed before the window opened must not pick a perk");
            Assert.AreEqual(0, fixture.CountTakenPerks());
        }

        [Test]
        public void MaxHealthLevelUpHealsTheLevelDelta()
        {
            Fixture fixture = Build(Seed, Perk("perk.health", 5, StatId.MaxHealth, StatOp.Flat, 20f));

            fixture.World.GetPool<Health>().Get(fixture.Hero).Current = 60f;

            fixture.Take("perk.health");
            fixture.Step();

            Assert.AreEqual(120f, fixture.MaxHealth());
            Assert.AreEqual(80f, fixture.Health());

            fixture.Take("perk.health");
            fixture.Step();

            Assert.AreEqual(140f, fixture.MaxHealth());
            Assert.AreEqual(100f, fixture.Health(), "level 2 heals the 20 between the levels, not the 40 of the new level");
        }

        [Test]
        public void SeveralLevelUpsInOneTickOfferOneAfterAnother()
        {
            Fixture fixture = Build(Seed, FivePerks());

            fixture.LevelUp(2);

            Assert.AreEqual(2, fixture.PendingLevelUps());
            Assert.AreEqual(1, fixture.Sink.Shows.Count);

            fixture.Choose(0);
            fixture.Step();

            Assert.IsTrue(fixture.Gate.IsAwaiting, "the second level must open its own choice");
            Assert.AreEqual(2, fixture.Sink.Shows.Count);

            fixture.Choose(0);

            Assert.AreEqual(0, fixture.PendingLevelUps());
            Assert.IsFalse(fixture.Gate.IsAwaiting);
        }

        [Test]
        public void ValidatorRejectsAStatTwiceInOneLevel()
        {
            PerkConfig perk = Perk("perk.twice", 1, StatId.MoveSpeed, StatOp.Increased, 0.1f);
            PerkLevel level = new PerkLevel();
            SetField(level, "_modifiers", new[]
            {
                Entry(StatId.MoveSpeed, StatOp.Increased, 0.1f),
                Entry(StatId.MoveSpeed, StatOp.More, 0.1f),
            });
            SetField(perk, "_levels", new[] { level });

            Assert.Throws<InvalidOperationException>(() => new PerkPool(new[] { perk }));
        }

        private List<string> Sequence(Fixture fixture, int levels)
        {
            List<string> offers = new List<string>();

            for (int level = 0; level < levels; level++)
            {
                fixture.LevelUp();

                for (int slot = 0; slot < fixture.LastOfferCount; slot++)
                    offers.Add(fixture.LastOffer(slot).PerkId + "@" + fixture.LastOffer(slot).NextLevel);

                offers.Add("|");

                fixture.Choose(level % fixture.LastOfferCount);
            }

            return offers;
        }

        private PerkConfig[] FivePerks()
        {
            return new[]
            {
                Perk("perk.sword-damage", 5, StatId.WeaponDamage, StatOp.Increased, 0.2f),
                Perk("perk.attack-speed", 5, StatId.AttackSpeed, StatOp.Increased, 0.1f),
                Perk("perk.move-speed", 5, StatId.MoveSpeed, StatOp.Increased, 0.1f),
                Perk("perk.max-health", 5, StatId.MaxHealth, StatOp.Flat, 20f),
                Perk("perk.dash-cooldown", 5, StatId.DashCooldown, StatOp.Increased, -0.1f),
            };
        }

        private PerkConfig Perk(string id, int maxLevel, StatId stat, StatOp op, float perLevel)
        {
            PerkConfig perk = ScriptableObject.CreateInstance<PerkConfig>();
            SetField(perk, "_id", id);

            PerkLevel[] levels = new PerkLevel[maxLevel];

            for (int index = 0; index < maxLevel; index++)
            {
                levels[index] = new PerkLevel();
                SetField(levels[index], "_modifiers", new[] { Entry(stat, op, perLevel * (index + 1)) });
            }

            SetField(perk, "_levels", levels);
            _assets.Add(perk);

            return perk;
        }

        private Fixture Build(int seed, params PerkConfig[] perks)
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            _assets.Add(level);

            Fixture fixture = new Fixture(seed, perks, level);
            _fixtures.Add(fixture);

            return fixture;
        }

        private static StatModifierEntry Entry(StatId stat, StatOp op, float value)
        {
            StatModifierEntry entry = new StatModifierEntry();
            SetField(entry, "_stat", stat);
            SetField(entry, "_op", op);
            SetField(entry, "_value", value);

            return entry;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }

        private sealed class Fixture : IDisposable
        {
            private const float TickSeconds = 1f / 60f;

            public readonly EcsWorld World;
            public readonly UpgradeChoiceGate Gate = new UpgradeChoiceGate();
            public readonly FakeChoiceInput Input = new FakeChoiceInput();
            public readonly FakeSink Sink = new FakeSink();
            public readonly int Hero;
            public readonly int Weapon;

            private readonly EcsSystems _systems;
            private readonly EcsSystems _choiceSystems;
            private readonly SimulationClock _clock = new SimulationClock();

            public Fixture(int seed, PerkConfig[] perks, LevelConfig level)
            {
                IContentEntry[] entries = new IContentEntry[perks.Length];
                Array.Copy(perks, entries, perks.Length);

                ContentRegistry registry = new ContentRegistry(entries);
                RunContext context = new RunContext("level.test", "character.test", RunMode.Story, seed, Array.Empty<StatModifierSpec>());

                World = new EcsWorld();
                StatModifiers modifiers = new StatModifiers(World);
                EnemyMotionBounds bounds = new EnemyMotionBounds(2.5f, 0.9f, 5f, 0.5f);

                _systems = new EcsSystems(World);
                _systems.Add(new RecomputeStatsSystem());
                _systems.Add(new OfferUpgradesSystem());
                _systems.Add(new CleanupEventsSystem());
                _systems.Inject(context, registry, level, _clock, modifiers, bounds, Gate, Input, Sink);
                _systems.Init();

                _choiceSystems = new EcsSystems(World);
                _choiceSystems.Add(new ApplyUpgradeChoiceSystem());
                _choiceSystems.Inject(context, registry, level, _clock, modifiers, bounds, Gate, Input, Sink);
                _choiceSystems.Init();

                Hero = World.NewEntity();
                World.GetPool<Player>().Add(Hero);

                ref MoveSpeed speed = ref World.GetPool<MoveSpeed>().Add(Hero);
                speed.Base = BaseMoveSpeed;
                speed.Value = BaseMoveSpeed;

                ref MaxHealth maxHealth = ref World.GetPool<MaxHealth>().Add(Hero);
                maxHealth.Base = HeroMaxHealth;
                maxHealth.Value = HeroMaxHealth;

                World.GetPool<Health>().Add(Hero).Current = HeroMaxHealth;

                Weapon = World.NewEntity();

                ref WeaponDamage damage = ref World.GetPool<WeaponDamage>().Add(Weapon);
                damage.Base = BaseWeaponDamage;
                damage.Value = BaseWeaponDamage;

                ref AttackSpeed attackSpeed = ref World.GetPool<AttackSpeed>().Add(Weapon);
                attackSpeed.Base = 1f;
                attackSpeed.Value = 1f;

                World.GetPool<OwnerLink>().Add(Weapon).Owner = World.PackEntity(Hero);

                modifiers.MarkDirty(Hero);
                modifiers.MarkDirty(Weapon);

                Step();
            }

            public int LastOfferCount => Sink.Shows[Sink.Shows.Count - 1].Length;

            public UpgradeOffer LastOffer(int slot)
            {
                return Sink.Shows[Sink.Shows.Count - 1][slot];
            }

            public void Step()
            {
                if (Gate.IsAwaiting)
                {
                    _choiceSystems.Run();

                    return;
                }

                _clock.Advance(TickSeconds);
                _systems.Run();
            }

            public void LevelUp(int count = 1)
            {
                World.GetPool<LevelUpEvent>().Add(Hero).Count = count;

                Step();
            }

            public void Choose(int index)
            {
                Input.Index = index;

                Step();
            }

            public void Take(string perkId)
            {
                LevelUp();

                Assert.IsTrue(Gate.IsAwaiting, $"fixture: no offer opened to take '{perkId}'");

                for (int slot = 0; slot < LastOfferCount; slot++)
                {
                    if (LastOffer(slot).PerkId != perkId)
                        continue;

                    Choose(slot);

                    return;
                }

                Assert.Fail($"fixture: '{perkId}' was not among the offers; widen the pool or change the seed");
            }

            public bool HasPendingChoice()
            {
                return World.GetPool<PendingChoice>().Has(Hero);
            }

            public int PendingLevelUps()
            {
                EcsPool<PendingLevelUps> pool = World.GetPool<PendingLevelUps>();

                return pool.Has(Hero) ? pool.Get(Hero).Count : 0;
            }

            public int CountTakenPerks()
            {
                int count = 0;

                foreach (int entity in World.Filter<TakenPerk>().End())
                    count++;

                return count;
            }

            public int LevelOf(string perkId)
            {
                EcsPool<TakenPerk> pool = World.GetPool<TakenPerk>();

                foreach (int entity in World.Filter<TakenPerk>().End())
                {
                    if (pool.Get(entity).PerkId == perkId)
                        return pool.Get(entity).Level;
                }

                return 0;
            }

            public int CountModifiers(string sourceId)
            {
                int count = 0;
                EcsPool<StatModifier> pool = World.GetPool<StatModifier>();

                foreach (int entity in World.Filter<StatModifier>().End())
                {
                    if (pool.Get(entity).SourceId == sourceId)
                        count++;
                }

                return count;
            }

            public float MoveSpeed()
            {
                return World.GetPool<MoveSpeed>().Get(Hero).Value;
            }

            public float WeaponDamage()
            {
                return World.GetPool<WeaponDamage>().Get(Weapon).Value;
            }

            public float MaxHealth()
            {
                return World.GetPool<MaxHealth>().Get(Hero).Value;
            }

            public float Health()
            {
                return World.GetPool<Health>().Get(Hero).Current;
            }

            public void Dispose()
            {
                _choiceSystems.Destroy();
                _systems.Destroy();
                World.Destroy();
            }
        }

        private sealed class FakeChoiceInput : IUpgradeChoiceInput
        {
            public int Index = -1;

            public bool TryConsume(out int index)
            {
                index = Index;

                if (Index < 0)
                    return false;

                Index = -1;

                return true;
            }

            public void Clear()
            {
                Index = -1;
            }
        }

        private sealed class FakeSink : IUpgradeChoiceSink
        {
            public readonly List<UpgradeOffer[]> Shows = new List<UpgradeOffer[]>();
            public int Hides;

            public void Show(UpgradeOffer first, UpgradeOffer second, UpgradeOffer third, int count)
            {
                UpgradeOffer[] offers = new UpgradeOffer[count];
                UpgradeOffer[] all = { first, second, third };
                Array.Copy(all, offers, count);

                Shows.Add(offers);
            }

            public void Hide()
            {
                Hides++;
            }
        }
    }
}

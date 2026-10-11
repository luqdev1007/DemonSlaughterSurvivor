using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnBreakablesSystem : IEcsInitSystem
    {
        private const string RandomStreamId = "spawn.breakables";
        private const int AttemptsPerBreakable = 64;

        private readonly EcsWorldInject _world = default;

        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<BodyHeight> _bodyHeights = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<HealOrbDrop> _drops = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;

        public void Init(IEcsSystems systems)
        {
            LevelConfig level = _level.Value;

            if (level.Breakable == null || level.BreakableCount <= 0)
                return;

            Validate(level, _motionBounds.Value.MaxEnemyBodyRadius);

            Vector3[] placed = Place(level, SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId));

            for (int index = 0; index < placed.Length; index++)
                Create(level.Breakable, placed[index]);
        }

        private void Create(BreakableConfig config, Vector3 position)
        {
            int entity = _world.Value.NewEntity();

            _breakables.Value.Add(entity);
            _positions.Value.Add(entity).Value = position;
            _facings.Value.Add(entity).Value = Vector3.forward;
            _bodyRadii.Value.Add(entity).Value = config.BodyRadius;
            _bodyHeights.Value.Add(entity).Value = config.BodyHeight;

            ref MaxHealth maxHealth = ref _maxHealths.Value.Add(entity);
            maxHealth.Base = config.MaxHealth;
            maxHealth.Value = config.MaxHealth;

            _healths.Value.Add(entity).Current = config.MaxHealth;

            if (config.HealOrb != null && config.HealOrbChance > 0f)
            {
                ref HealOrbDrop drop = ref _drops.Value.Add(entity);
                drop.Config = config.HealOrb;
                drop.Chance = config.HealOrbChance;
            }

            _views.Value.Add(entity).Value = _viewFactory.Value.Create(config.ViewPrefab, position);
        }

        private static Vector3[] Place(LevelConfig level, uint state)
        {
            int count = level.BreakableCount;
            float minSqr = level.BreakableMinRadius * level.BreakableMinRadius;
            float maxSqr = level.BreakableMaxRadius * level.BreakableMaxRadius;
            float spacingSqr = level.BreakableSpacing * level.BreakableSpacing;

            Vector3[] placed = new Vector3[count];
            int done = 0;
            int attempts = count * AttemptsPerBreakable;

            while (done < count)
            {
                if (attempts-- <= 0)
                    throw new InvalidOperationException(
                        $"{nameof(LevelConfig)} '{level.Id}': placed {done} of {count} breakables in the ring " +
                        $"{level.BreakableMinRadius}..{level.BreakableMaxRadius} m with spacing {level.BreakableSpacing} m. " +
                        "Widen the ring, lower the count or the spacing.");

                float radius = Mathf.Sqrt(Mathf.Lerp(minSqr, maxSqr, SimulationRandom.NextUnit(ref state)));
                float angle = SimulationRandom.NextUnit(ref state) * Mathf.PI * 2f;
                Vector3 candidate = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (TooClose(placed, done, candidate, spacingSqr))
                    continue;

                placed[done] = candidate;
                done++;
            }

            return placed;
        }

        private static bool TooClose(Vector3[] placed, int count, Vector3 candidate, float spacingSqr)
        {
            for (int index = 0; index < count; index++)
            {
                Vector3 offset = placed[index] - candidate;

                if (offset.x * offset.x + offset.z * offset.z < spacingSqr)
                    return true;
            }

            return false;
        }

        private static void Validate(LevelConfig level, float maxEnemyBodyRadius)
        {
            BreakableConfig config = level.Breakable;
            string owner = $"{nameof(BreakableConfig)} '{config.Id}'";

            if (config.ViewPrefab == null)
                throw new InvalidOperationException($"{owner} has no view prefab.");

            if (config.MaxHealth <= 0f)
                throw new InvalidOperationException($"{owner} has {nameof(BreakableConfig.MaxHealth)} {config.MaxHealth}; it must be above 0.");

            if (config.BodyRadius <= 0f || config.BodyHeight <= 0f)
                throw new InvalidOperationException($"{owner} needs a positive body radius and height.");

            if (config.BodyRadius > maxEnemyBodyRadius)
                throw new InvalidOperationException(
                    $"{owner} has body radius {config.BodyRadius}, wider than the widest enemy ({maxEnemyBodyRadius}). " +
                    "Weapon queries pad their radius by the widest enemy only, so the sword would miss its edge.");

            if (config.HealOrb != null)
            {
                if (config.HealOrb.ViewPrefab == null)
                    throw new InvalidOperationException($"{nameof(HealOrbConfig)} '{config.HealOrb.Id}' has no view prefab.");

                if (config.HealOrb.HealFraction <= 0f)
                    throw new InvalidOperationException(
                        $"{nameof(HealOrbConfig)} '{config.HealOrb.Id}' heals {config.HealOrb.HealFraction} of max health; it must be above 0.");
            }

            if (level.BreakableMinRadius < 0f || level.BreakableMaxRadius < level.BreakableMinRadius || level.BreakableMaxRadius > level.ArenaRadius)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' needs 0 <= breakable min radius <= max radius <= arena radius ({level.ArenaRadius}).");

            if (level.BreakableSpacing < 0f)
                throw new InvalidOperationException($"{nameof(LevelConfig)} '{level.Id}' has a negative breakable spacing.");
        }
    }
}

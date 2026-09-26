using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using System;

namespace Game.Simulation.Services
{
    public sealed class StatModifiers
    {
        private readonly EcsWorld _world;
        private readonly EcsPool<StatModifier> _modifiers;
        private readonly EcsPool<StatsDirty> _dirty;
        private readonly EcsFilter _filter;

        public StatModifiers(EcsWorld world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            _world = world;
            _modifiers = world.GetPool<StatModifier>();
            _dirty = world.GetPool<StatsDirty>();
            _filter = world.Filter<StatModifier>().End();
        }

        public void Add(int target, StatId stat, StatOp op, float value, string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentException(
                    "A stat modifier needs a source id. Without it the modifier cannot be removed by its source " +
                    "and cannot be told apart from another modifier of the same stat.",
                    nameof(sourceId));

            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Stat modifier '{sourceId}' on {stat} has a non-finite value, which would poison the final stat.");

            foreach (int entity in _filter)
            {
                ref StatModifier existing = ref _modifiers.Get(entity);

                if (existing.Stat != stat)
                    continue;

                if (string.Equals(existing.SourceId, sourceId, StringComparison.Ordinal) == false)
                    continue;

                if (IsTarget(existing.Target, target) == false)
                    continue;

                existing.Op = op;
                existing.Value = value;

                MarkDirty(target);

                return;
            }

            int modifierEntity = _world.NewEntity();

            ref StatModifier modifier = ref _modifiers.Add(modifierEntity);
            modifier.Target = _world.PackEntity(target);
            modifier.Stat = stat;
            modifier.Op = op;
            modifier.Value = value;
            modifier.SourceId = sourceId;

            MarkDirty(target);
        }

        public int RemoveBySource(int target, string sourceId)
        {
            int removed = 0;

            foreach (int entity in _filter)
            {
                ref StatModifier existing = ref _modifiers.Get(entity);

                if (string.Equals(existing.SourceId, sourceId, StringComparison.Ordinal) == false)
                    continue;

                if (IsTarget(existing.Target, target) == false)
                    continue;

                _world.DelEntity(entity);

                removed++;
            }

            if (removed > 0)
                MarkDirty(target);

            return removed;
        }

        public void MarkDirty(int target)
        {
            if (_dirty.Has(target))
                return;

            _dirty.Add(target);
        }

        private bool IsTarget(EcsPackedEntity packed, int target)
        {
            if (packed.Unpack(_world, out int unpacked) == false)
                return false;

            return unpacked == target;
        }
    }
}

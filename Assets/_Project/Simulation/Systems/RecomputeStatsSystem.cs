using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Text;

namespace Game.Simulation.Systems
{
    public sealed class RecomputeStatsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int InitialModifierCapacity = 64;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<StatsDirty>> _dirtyTargets = default;
        private readonly EcsFilterInject<Inc<OwnerLink>, Exc<StatsDirty>> _cleanOwned = default;
        private readonly EcsFilterInject<Inc<StatModifier>> _modifiers = default;

        private readonly EcsPoolInject<StatModifier> _modifierPool = default;
        private readonly EcsPoolInject<StatsDirty> _dirty = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<MoveSpeed> _moveSpeeds = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<ContactDamage> _contactDamages = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Position> _positions = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;

        private GatheredModifier[] _gathered;
        private int _gatheredCount;

        private float _maxEnemyMoveSpeed;

        public void Init(IEcsSystems systems)
        {
            _maxEnemyMoveSpeed = WaveTimelineValidator.ResolveMaxMoveSpeed(_level.Value.Waves);

            _gathered = new GatheredModifier[InitialModifierCapacity];
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int owned in _cleanOwned.Value)
            {
                ref OwnerLink link = ref _ownerLinks.Value.Get(owned);

                if (link.Owner.Unpack(world, out int owner) == false)
                    continue;

                if (_dirty.Value.Has(owner) == false)
                    continue;

                _dirty.Value.Add(owned);
            }

            foreach (int target in _dirtyTargets.Value)
            {
                Gather(world, target);
                SortGathered();
                Apply(target);
                GuardEnemySpeed(target);

                _dirty.Value.Del(target);
            }
        }

        private void Gather(EcsWorld world, int target)
        {
            _gatheredCount = 0;

            int owner = -1;

            if (_ownerLinks.Value.Has(target))
            {
                ref OwnerLink link = ref _ownerLinks.Value.Get(target);

                if (link.Owner.Unpack(world, out int unpackedOwner))
                    owner = unpackedOwner;
            }

            foreach (int entity in _modifiers.Value)
            {
                ref StatModifier modifier = ref _modifierPool.Value.Get(entity);

                if (modifier.Target.Unpack(world, out int modified) == false)
                    continue;

                bool own = modified == target;
                bool inherited = owner >= 0 && modified == owner && Carries(target, modifier.Stat);

                if (own == false && inherited == false)
                    continue;

                Push(modifier);
            }
        }

        private void Push(in StatModifier modifier)
        {
            if (_gatheredCount == _gathered.Length)
                Array.Resize(ref _gathered, _gathered.Length * 2);

            ref GatheredModifier slot = ref _gathered[_gatheredCount];
            slot.Stat = modifier.Stat;
            slot.Op = modifier.Op;
            slot.Value = modifier.Value;
            slot.SourceId = modifier.SourceId;

            _gatheredCount++;
        }

        private void SortGathered()
        {
            for (int index = 1; index < _gatheredCount; index++)
            {
                GatheredModifier current = _gathered[index];

                int position = index - 1;

                while (position >= 0 && Compare(_gathered[position], current) > 0)
                {
                    _gathered[position + 1] = _gathered[position];
                    position--;
                }

                _gathered[position + 1] = current;
            }
        }

        private static int Compare(in GatheredModifier left, in GatheredModifier right)
        {
            int byStat = ((int)left.Stat).CompareTo((int)right.Stat);

            if (byStat != 0)
                return byStat;

            int bySource = string.CompareOrdinal(left.SourceId, right.SourceId);

            if (bySource != 0)
                return bySource;

            int byOp = ((int)left.Op).CompareTo((int)right.Op);

            if (byOp != 0)
                return byOp;

            return left.Value.CompareTo(right.Value);
        }

        private StatTotals Total(StatId stat)
        {
            StatTotals totals = StatTotals.Neutral;

            for (int index = 0; index < _gatheredCount; index++)
            {
                ref GatheredModifier modifier = ref _gathered[index];

                if (modifier.Stat != stat)
                    continue;

                StatFormula.Accumulate(ref totals, modifier.Op, modifier.Value);
            }

            return totals;
        }

        private void Apply(int target)
        {
            if (_moveSpeeds.Value.Has(target))
            {
                ref MoveSpeed speed = ref _moveSpeeds.Value.Get(target);
                speed.Value = StatFormula.Evaluate(speed.Base, Total(StatId.MoveSpeed));
            }

            if (_maxHealths.Value.Has(target))
                ApplyMaxHealth(target);

            if (_contactDamages.Value.Has(target))
            {
                ref ContactDamage damage = ref _contactDamages.Value.Get(target);
                damage.Value = StatFormula.Evaluate(damage.Base, Total(StatId.ContactDamage));
            }

            if (_dashStats.Value.Has(target))
            {
                ref DashStats dash = ref _dashStats.Value.Get(target);
                dash.Cooldown = StatFormula.Evaluate(dash.CooldownBase, Total(StatId.DashCooldown));
            }
        }

        private void ApplyMaxHealth(int target)
        {
            ref MaxHealth maxHealth = ref _maxHealths.Value.Get(target);

            float previous = maxHealth.Value;
            float current = StatFormula.Evaluate(maxHealth.Base, Total(StatId.MaxHealth));

            maxHealth.Value = current;

            if (_healths.Value.Has(target) == false)
                return;

            if (_dead.Value.Has(target))
                return;

            ref Health health = ref _healths.Value.Get(target);

            if (current > previous)
            {
                health.Current += current - previous;

                return;
            }

            if (health.Current > current)
                health.Current = current;
        }

        private bool Carries(int target, StatId stat)
        {
            switch (stat)
            {
                case StatId.MoveSpeed:
                    return _moveSpeeds.Value.Has(target);
                case StatId.MaxHealth:
                    return _maxHealths.Value.Has(target);
                case StatId.ContactDamage:
                    return _contactDamages.Value.Has(target);
                case StatId.DashCooldown:
                    return _dashStats.Value.Has(target);
                default:
                    return false;
            }
        }

        private void GuardEnemySpeed(int target)
        {
            if (_enemies.Value.Has(target) == false)
                return;

            if (_moveSpeeds.Value.Has(target) == false)
                return;

            ref MoveSpeed speed = ref _moveSpeeds.Value.Get(target);

            if (speed.Value <= _maxEnemyMoveSpeed)
                return;

            StringBuilder sources = new StringBuilder();

            for (int index = 0; index < _gatheredCount; index++)
            {
                ref GatheredModifier modifier = ref _gathered[index];

                if (modifier.Stat != StatId.MoveSpeed)
                    continue;

                sources.Append($" '{modifier.SourceId}' {modifier.Op} {modifier.Value};");
            }

            string position = _positions.Value.Has(target) ? _positions.Value.Get(target).Value.ToString() : "<no position>";

            throw new InvalidOperationException(
                $"Enemy entity {target} at {position} has a final {nameof(StatId.MoveSpeed)} of {speed.Value} " +
                $"(base {speed.Base}), above the fastest enemy speed {_maxEnemyMoveSpeed} in {nameof(WaveTimelineConfig)} " +
                $"'{(_level.Value.Waves == null ? "<none>" : _level.Value.Waves.Id)}'. Modifiers:{sources} " +
                $"{nameof(DetectContactDamageSystem)} derives its spatial query slack from that fastest speed once at start, " +
                "so a faster enemy can leave its cell unseen and deal no contact damage without any message. " +
                "Either keep enemy speed modifiers at or below the timeline maximum, " +
                "or make the query slack follow the final enemy speeds instead of the config.");
        }

        private struct GatheredModifier
        {
            public StatId Stat;
            public StatOp Op;
            public float Value;
            public string SourceId;
        }
    }
}

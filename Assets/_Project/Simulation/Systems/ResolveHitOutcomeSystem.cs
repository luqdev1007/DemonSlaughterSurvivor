using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ResolveHitOutcomeSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "stream.combat-rolls";

        private const float PowerScale = 100f;
        private const float DefaultBodyHeight = 1.8f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;

        private readonly EcsPoolInject<HunterMark> _marks = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<DamageEvaded> _evaded = default;
        private readonly EcsPoolInject<CombatStats> _combatStats = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyHeight> _bodyHeights = default;
        private readonly EcsPoolInject<Concentration> _concentrations = default;

        private readonly EcsCustomInject<RunContext> _context = default;

        private uint _random;

        public void Init(IEcsSystems systems)
        {
            _random = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent damage = ref _damageEvents.Value.Get(entity);

                if (damage.Amount <= 0f)
                    continue;

                if (damage.Target.Unpack(world, out int target) == false)
                    continue;

                if (_healths.Value.Has(target) == false || _invulnerables.Value.Has(target))
                    continue;

                if (_marks.Value.Has(target))
                {
                    ref HunterMark mark = ref _marks.Value.Get(target);
                    damage.Amount *= 1f + mark.DamageTakenBonus;
                    damage.CritChanceBonus += mark.CritChanceBonus;
                }

                if (damage.Source.Unpack(world, out int source) && source != target && damage.IsMagic)
                    damage.Amount *= ConcentrationBonus.Damage(_concentrations.Value, source);

                if (damage.Source.Unpack(world, out source) && source != target && _combatStats.Value.Has(source))
                    Empower(ref damage, ref _combatStats.Value.Get(source));

                if (_combatStats.Value.Has(target))
                    Defend(entity, target, ref damage, ref _combatStats.Value.Get(target));
            }
        }

        private void Empower(ref DamageEvent damage, ref CombatStats attacker)
        {
            float power = damage.IsMagic ? attacker.SpellPower : attacker.AttackPower;

            damage.Amount *= (1f + Mathf.Max(0f, power) / PowerScale) * Mathf.Max(0f, attacker.DamageDealt);

            if (damage.IsCrit)
                return;

            if (SimulationRandom.NextUnit(ref _random) >= attacker.CritChance + damage.CritChanceBonus)
                return;

            damage.Amount *= attacker.CritMultiplier;
            damage.IsCrit = true;
        }

        private void Defend(int entity, int target, ref DamageEvent damage, ref CombatStats defender)
        {
            float evasion = Mathf.Clamp(defender.Evasion, 0f, defender.MaxEvasion);

            if (evasion > 0f && SimulationRandom.NextUnit(ref _random) < evasion)
            {
                damage.Amount = 0f;

                ref DamageEvaded evaded = ref _evaded.Value.Add(entity);
                evaded.TargetKey = target;
                evaded.Anchor = Anchor(target);

                return;
            }

            float defense = Mathf.Max(0f, damage.IsMagic ? defender.MagicResist : defender.Armor);

            damage.Amount *= defender.DefenseScale / (defender.DefenseScale + defense);
        }

        private Vector3 Anchor(int target)
        {
            if (_positions.Value.Has(target) == false)
                return Vector3.zero;

            float height = _bodyHeights.Value.Has(target) ? _bodyHeights.Value.Get(target).Value : DefaultBodyHeight;

            return _positions.Value.Get(target).Value + Vector3.up * height;
        }
    }
}

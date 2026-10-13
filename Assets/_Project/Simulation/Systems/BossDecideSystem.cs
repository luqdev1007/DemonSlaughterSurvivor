using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class BossDecideSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "boss.knight";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Boss, EnemyAction, ChaseTarget, Position>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<EnemyHeavyStrike> _heavyStrikes = default;
        private readonly EcsPoolInject<BossRandom> _randoms = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Init(IEcsSystems systems)
        {
            IReadOnlyList<EnemyConfig> registered = _content.Value.All<EnemyConfig>();

            for (int index = 0; index < registered.Count; index++)
            {
                EnemyConfig enemy = registered[index];

                if (enemy == null)
                    continue;

                if (enemy.Boss != null)
                    enemy.Boss.Validate(enemy);

                if (enemy.Charge != null)
                    enemy.Charge.Validate(enemy);

                if (enemy.Spin != null)
                    enemy.Spin.Validate(enemy);

                if (enemy.HeavyStrike != null)
                    enemy.HeavyStrike.Validate(enemy);
            }
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (action.Phase != EnemyActionPhase.None)
                    continue;

                if (action.GlobalCooldownTicks > 0)
                {
                    action.GlobalCooldownTicks--;

                    continue;
                }

                if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                    continue;

                float distance = FlatDistance(_positions.Value.Get(entity).Value, target);
                BossConfig boss = _bosses.Value.Get(entity).Config;

                float chargeWeight = CanCharge(entity, distance) ? boss.ChargeWeight : 0f;
                float spinWeight = CanSpin(entity, distance) ? boss.SpinWeight : 0f;
                float strikeWeight = CanHeavyStrike(entity, distance) ? boss.HeavyStrikeWeight : 0f;
                float total = chargeWeight + spinWeight + strikeWeight;

                if (total <= 0f)
                    continue;

                float roll = SimulationRandom.NextUnit(ref ResolveRandom(entity).State) * total;

                if (roll < chargeWeight)
                    StartCharge(entity, ref action, delta);
                else if (roll < chargeWeight + spinWeight)
                    StartSpin(entity, ref action, delta);
                else
                    StartHeavyStrike(entity, ref action, delta);
            }
        }

        private bool CanSpin(int entity, float distance)
        {
            if (_spins.Value.Has(entity) == false)
                return false;

            ref EnemySpin spin = ref _spins.Value.Get(entity);

            return spin.CooldownTicks <= 0 && distance <= spin.Config.TriggerRange;
        }

        private void StartSpin(int entity, ref EnemyAction action, float delta)
        {
            ref EnemySpin spin = ref _spins.Value.Get(entity);

            spin.CooldownTicks = EnemyActionPhases.ToCooldownTicks(spin.Config.CooldownSeconds, delta);

            EnemyActionPhases.Enter(ref action, EnemyActionPhase.SpinRaise, spin.Config.RaiseSeconds, delta);
        }

        private bool CanHeavyStrike(int entity, float distance)
        {
            if (_heavyStrikes.Value.Has(entity) == false)
                return false;

            ref EnemyHeavyStrike strike = ref _heavyStrikes.Value.Get(entity);

            return strike.CooldownTicks <= 0 && distance <= strike.Config.TriggerRange;
        }

        private void StartHeavyStrike(int entity, ref EnemyAction action, float delta)
        {
            ref EnemyHeavyStrike strike = ref _heavyStrikes.Value.Get(entity);

            strike.CooldownTicks = EnemyActionPhases.ToCooldownTicks(strike.Config.CooldownSeconds, delta);
            strike.ImpactPending = false;

            EnemyActionPhases.Enter(ref action, EnemyActionPhase.StrikeWindup, strike.Config.WindupSeconds, delta);
        }

        private bool CanCharge(int entity, float distance)
        {
            if (_charges.Value.Has(entity) == false)
                return false;

            ref EnemyCharge charge = ref _charges.Value.Get(entity);

            return charge.CooldownTicks <= 0 && distance >= charge.Config.MinRange && distance <= charge.Config.MaxRange;
        }

        private void StartCharge(int entity, ref EnemyAction action, float delta)
        {
            ref EnemyCharge charge = ref _charges.Value.Get(entity);

            charge.CooldownTicks = EnemyActionPhases.ToCooldownTicks(charge.Config.CooldownSeconds, delta);

            EnemyActionPhases.Enter(ref action, EnemyActionPhase.ChargeWindup, charge.Config.WindupSeconds, delta);
        }

        private ref BossRandom ResolveRandom(int entity)
        {
            if (_randoms.Value.Has(entity))
                return ref _randoms.Value.Get(entity);

            ref BossRandom random = ref _randoms.Value.Add(entity);
            random.State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

            return ref random;
        }

        private static float FlatDistance(Vector3 from, Vector3 to)
        {
            float deltaX = to.x - from.x;
            float deltaZ = to.z - from.z;

            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }
    }
}

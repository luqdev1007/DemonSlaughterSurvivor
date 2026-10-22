using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Services
{
    public sealed class EnemyFactory
    {
        private readonly EcsWorld _world;
        private readonly IViewFactory _viewFactory;
        private readonly StatModifiers _statModifiers;

        private readonly EcsPool<Enemy> _enemies;
        private readonly EcsPool<Position> _positions;
        private readonly EcsPool<Facing> _facings;
        private readonly EcsPool<MoveIntent> _intents;
        private readonly EcsPool<MoveSpeed> _speeds;
        private readonly EcsPool<TurnSpeed> _turnSpeeds;
        private readonly EcsPool<BodyRadius> _bodyRadii;
        private readonly EcsPool<BodyHeight> _bodyHeights;
        private readonly EcsPool<ContactDamage> _contactDamages;
        private readonly EcsPool<Health> _healths;
        private readonly EcsPool<MaxHealth> _maxHealths;
        private readonly EcsPool<Velocity> _velocities;
        private readonly EcsPool<ChaseTarget> _chaseTargets;
        private readonly EcsPool<Separation> _separations;
        private readonly EcsPool<View> _views;
        private readonly EcsPool<GemDrop> _gemDrops;
        private readonly EcsPool<RangedAttack> _rangedAttacks;
        private readonly EcsPool<AttackSpeed> _attackSpeeds;
        private readonly EcsPool<Boss> _bosses;
        private readonly EcsPool<Elite> _elites;
        private readonly EcsPool<EliteAffix> _eliteAffixes;
        private readonly EcsPool<NotPushable> _notPushables;
        private readonly EcsPool<Interruptible> _interruptibles;
        private readonly EcsPool<PreviousPosition> _previousPositions;
        private readonly EcsPool<EnemyAction> _actions;
        private readonly EcsPool<EnemyCharge> _charges;
        private readonly EcsPool<EnemySpin> _spins;
        private readonly EcsPool<EnemyHeavyStrike> _heavyStrikes;
        private readonly EcsPool<EnemyJump> _jumps;

        public EnemyFactory(EcsWorld world, IViewFactory viewFactory, StatModifiers statModifiers)
        {
            _world = world;
            _viewFactory = viewFactory;
            _statModifiers = statModifiers;

            _enemies = world.GetPool<Enemy>();
            _positions = world.GetPool<Position>();
            _facings = world.GetPool<Facing>();
            _intents = world.GetPool<MoveIntent>();
            _speeds = world.GetPool<MoveSpeed>();
            _turnSpeeds = world.GetPool<TurnSpeed>();
            _bodyRadii = world.GetPool<BodyRadius>();
            _bodyHeights = world.GetPool<BodyHeight>();
            _contactDamages = world.GetPool<ContactDamage>();
            _healths = world.GetPool<Health>();
            _maxHealths = world.GetPool<MaxHealth>();
            _velocities = world.GetPool<Velocity>();
            _chaseTargets = world.GetPool<ChaseTarget>();
            _separations = world.GetPool<Separation>();
            _views = world.GetPool<View>();
            _gemDrops = world.GetPool<GemDrop>();
            _rangedAttacks = world.GetPool<RangedAttack>();
            _attackSpeeds = world.GetPool<AttackSpeed>();
            _bosses = world.GetPool<Boss>();
            _elites = world.GetPool<Elite>();
            _eliteAffixes = world.GetPool<EliteAffix>();
            _notPushables = world.GetPool<NotPushable>();
            _interruptibles = world.GetPool<Interruptible>();
            _previousPositions = world.GetPool<PreviousPosition>();
            _actions = world.GetPool<EnemyAction>();
            _charges = world.GetPool<EnemyCharge>();
            _spins = world.GetPool<EnemySpin>();
            _heavyStrikes = world.GetPool<EnemyHeavyStrike>();
            _jumps = world.GetPool<EnemyJump>();
        }

        public const string SpawnModifierSource = "enemy.spawn";
        public const string EliteModifierSource = "enemy.elite";

        public int Create(EnemyConfig config, Vector3 position, float variationRoll, EliteAffixConfig affix = null)
        {
            int entity = _world.NewEntity();

            _enemies.Add(entity);

            ref Position entityPosition = ref _positions.Add(entity);
            entityPosition.Value = position;

            ref Facing facing = ref _facings.Add(entity);
            facing.Value = Vector3.forward;

            ref MoveSpeed speed = ref _speeds.Add(entity);
            speed.Base = config.MoveSpeed;
            speed.Value = config.MoveSpeed;

            ref TurnSpeed turnSpeed = ref _turnSpeeds.Add(entity);
            turnSpeed.Value = config.TurnSpeed;

            ref BodyRadius bodyRadius = ref _bodyRadii.Add(entity);
            bodyRadius.Value = config.BodyRadius * (affix == null ? 1f : affix.Scale);

            ref BodyHeight bodyHeight = ref _bodyHeights.Add(entity);
            bodyHeight.Value = config.BodyHeight;

            ref ContactDamage contactDamage = ref _contactDamages.Add(entity);
            contactDamage.Base = config.ContactDamage;
            contactDamage.Value = config.ContactDamage;

            ref MaxHealth maxHealth = ref _maxHealths.Add(entity);
            maxHealth.Base = config.MaxHealth;
            maxHealth.Value = config.MaxHealth;

            ref Health health = ref _healths.Add(entity);
            health.Current = config.MaxHealth;

            ref Separation separation = ref _separations.Add(entity);
            separation.Radius = config.SeparationRadius;
            separation.Strength = config.SeparationStrength;

            _intents.Add(entity);
            _velocities.Add(entity);
            _chaseTargets.Add(entity);

            ref View view = ref _views.Add(entity);
            view.Value = _viewFactory.Create(config.ViewPrefab, position);

            if (config.Gem != null)
                _gemDrops.Add(entity).Config = config.Gem;

            if (config.RangedAttack != null)
            {
                _rangedAttacks.Add(entity).Config = config.RangedAttack;

                ref AttackSpeed attackSpeed = ref _attackSpeeds.Add(entity);
                attackSpeed.Base = 1f;
                attackSpeed.Value = 1f;
            }

            if (config.RangedAttack != null && config.RangedAttack.Interruptible)
                _interruptibles.Add(entity);

            if (config.Boss != null)
                AddBoss(entity, config.Boss, position);

            if (config.Charge != null)
                _charges.Add(entity).Config = config.Charge;

            if (config.Spin != null)
                _spins.Add(entity).Config = config.Spin;

            if (config.HeavyStrike != null)
                _heavyStrikes.Add(entity).Config = config.HeavyStrike;

            if (config.Jump != null)
                _jumps.Add(entity).Config = config.Jump;

            if (config.IsElite || affix != null)
            {
                if (_elites.Has(entity) == false)
                    _elites.Add(entity);
            }

            if (affix != null)
                ApplyAffix(entity, affix, view.Value);

            if (config.MoveSpeedSpread > 0f)
                _statModifiers.Add(entity, StatId.MoveSpeed, StatOp.More, (variationRoll * 2f - 1f) * config.MoveSpeedSpread, SpawnModifierSource);

            _statModifiers.MarkDirty(entity);

            return entity;
        }

        private void ApplyAffix(int entity, EliteAffixConfig affix, IView view)
        {
            _eliteAffixes.Add(entity).Config = affix;

            IReadOnlyList<StatModifierEntry> modifiers = affix.Modifiers;

            for (int index = 0; index < modifiers.Count; index++)
                _statModifiers.Add(entity, modifiers[index].Stat, modifiers[index].Op, modifiers[index].Value, EliteModifierSource);

            if (view is IEliteView eliteView)
                eliteView.SetElite(affix.Scale, affix.VfxPrefab, affix.VfxOffset);
        }

        private void AddBoss(int entity, BossConfig boss, Vector3 position)
        {
            _bosses.Add(entity).Config = boss;
            _notPushables.Add(entity);
            _previousPositions.Add(entity).Value = position;
            _actions.Add(entity);
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnPlayerSystem : IEcsInitSystem
    {
        private static readonly Vector3 StartPosition = Vector3.zero;

        private readonly EcsWorldInject _world = default;

        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<HitInvulnerability> _hitInvulnerabilities = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            DashAbilityConfig dash = character.Dash;

            int entity = _world.Value.NewEntity();

            _players.Value.Add(entity);

            ref Position position = ref _positions.Value.Add(entity);
            position.Value = StartPosition;

            ref Facing facing = ref _facings.Value.Add(entity);
            facing.Value = Vector3.forward;

            ref MoveSpeed speed = ref _speeds.Value.Add(entity);
            speed.Value = character.MoveSpeed;

            ref TurnSpeed turnSpeed = ref _turnSpeeds.Value.Add(entity);
            turnSpeed.Value = character.TurnSpeed;

            ref BodyRadius bodyRadius = ref _bodyRadii.Value.Add(entity);
            bodyRadius.Value = character.BodyRadius;

            ref MaxHealth maxHealth = ref _maxHealths.Value.Add(entity);
            maxHealth.Value = character.MaxHealth;

            ref Health health = ref _healths.Value.Add(entity);
            health.Current = character.MaxHealth;

            ref HitInvulnerability hitInvulnerability = ref _hitInvulnerabilities.Value.Add(entity);
            hitInvulnerability.Seconds = character.HitInvulnerabilitySeconds;

            ref DashStats dashStats = ref _dashStats.Value.Add(entity);
            dashStats.Distance = dash.Distance;
            dashStats.Duration = dash.Duration;
            dashStats.Cooldown = dash.Cooldown;
            dashStats.Direction = dash.Direction;

            _intents.Value.Add(entity);
            _velocities.Value.Add(entity);

            ref View view = ref _views.Value.Add(entity);
            view.Value = _viewFactory.Value.Create(character.ViewPrefab, StartPosition);
        }
    }
}

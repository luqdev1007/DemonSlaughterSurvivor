using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyLowHealthFurySystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string ModifierSource = "character.low-health-fury";

        private const float StepTolerance = 1e-4f;

        private readonly EcsFilterInject<Inc<Player, Health, MaxHealth>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<LowHealthFury> _furies = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        private LowHealthFuryConfig _config;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            _config = character.LowHealthFury;

            if (_config != null)
                _config.Validate($"{nameof(CharacterConfig)} '{character.Id}'");
        }

        public void Run(IEcsSystems systems)
        {
            if (_config == null)
                return;

            foreach (int hero in _heroes.Value)
            {
                float max = _maxHealths.Value.Get(hero).Value;

                if (max <= 0f)
                    continue;

                float missing = 1f - Mathf.Clamp01(_healths.Value.Get(hero).Current / max);
                int step = Mathf.FloorToInt(missing / _config.Step + StepTolerance);

                bool known = _furies.Value.Has(hero);

                if (known && _furies.Value.Get(hero).Step == step)
                    continue;

                if (known == false)
                    _furies.Value.Add(hero);

                _furies.Value.Get(hero).Step = step;

                _statModifiers.Value.RemoveBySource(hero, ModifierSource);

                if (step == 0)
                    continue;

                float share = Mathf.Min(1f, step * _config.Step);

                Add(hero, StatId.WeaponDamage, _config.WeaponDamage * share);
                Add(hero, StatId.AttackSpeed, _config.AttackSpeed * share);
                Add(hero, StatId.MoveSpeed, _config.MoveSpeed * share);
                Add(hero, StatId.LifeOnKill, _config.LifeSteal * share);
                Add(hero, StatId.LifeOnHit, _config.LifeSteal * share);
            }
        }

        private void Add(int hero, StatId stat, float value)
        {
            if (value <= 0f)
                return;

            _statModifiers.Value.Add(hero, stat, StatOp.More, value, ModifierSource);
        }
    }
}

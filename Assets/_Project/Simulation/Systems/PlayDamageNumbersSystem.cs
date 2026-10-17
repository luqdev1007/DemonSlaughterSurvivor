using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PlayDamageNumbersSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _events = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<DamageApplied> _applied = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyHeight> _bodyHeights = default;

        private readonly EcsCustomInject<IDamageNumberSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            if (_sink.Value == null)
                return;

            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                float amount = _applied.Value.Get(entity).Amount;

                if (amount <= 0f)
                    continue;

                ref DamageEvent damage = ref _damageEvents.Value.Get(entity);

                if (damage.Target.Unpack(world, out int target) == false || _enemies.Value.Has(target) == false || _positions.Value.Has(target) == false)
                    continue;

                float height = _bodyHeights.Value.Has(target) ? _bodyHeights.Value.Get(target).Value : 1.8f;
                Vector3 position = _positions.Value.Get(target).Value + Vector3.up * height;

                _sink.Value.Show(target, position, amount, ResolveStyle(damage.IsCrit, damage.IsMagic));
            }
        }

        private static DamageNumberStyle ResolveStyle(bool crit, bool magic)
        {
            if (magic)
                return crit ? DamageNumberStyle.MagicCrit : DamageNumberStyle.Magic;

            return crit ? DamageNumberStyle.Crit : DamageNumberStyle.Normal;
        }
    }
}

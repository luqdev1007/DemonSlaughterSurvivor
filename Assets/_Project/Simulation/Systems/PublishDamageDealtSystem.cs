using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PublishDamageDealtSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _events = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageEvaded>> _evasions = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<DamageApplied> _applied = default;
        private readonly EcsPoolInject<DamageEvaded> _evaded = default;

        private readonly EcsCustomInject<IDamageNumberSink> _sink = default;
        private readonly EcsCustomInject<IDamageDealtSink> _dealt = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            if (_dealt.Value != null)
                _dealt.Value.Advance(_clock.Value.Elapsed);

            if (_sink.Value == null && _dealt.Value == null)
                return;

            foreach (int entity in _events.Value)
            {
                ref DamageApplied applied = ref _applied.Value.Get(entity);

                if (applied.Amount <= 0f || applied.OnEnemy == false)
                    continue;

                if (_dealt.Value != null)
                    _dealt.Value.Record(applied.Amount);

                if (_sink.Value == null)
                    continue;

                ref DamageEvent damage = ref _damageEvents.Value.Get(entity);

                _sink.Value.Show(applied.TargetKey, applied.Anchor, applied.Amount, ResolveStyle(damage.IsCrit, damage.IsMagic));
            }

            if (_sink.Value == null)
                return;

            foreach (int entity in _evasions.Value)
            {
                ref DamageEvaded evaded = ref _evaded.Value.Get(entity);

                _sink.Value.Show(evaded.TargetKey, evaded.Anchor, 0f, DamageNumberStyle.Evade);
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

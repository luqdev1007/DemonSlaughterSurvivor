using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class MarkOnBackleapSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int MaxTargets = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Backleaping, Position>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<HunterMark> _marks = default;
        private readonly EcsPoolInject<MarkFx> _markFx = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private int[] _picked;
        private float[] _distances;

        public void Init(IEcsSystems systems)
        {
            _picked = new int[MaxTargets];
            _distances = new float[MaxTargets];
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref Backleaping leap = ref _backleaps.Value.Get(hero);

                if (leap.Ticks != 0 || (leap.Phase != BackleapPhase.Leap && leap.Phase != BackleapPhase.Hop))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out HunterMarkConfig config, out int level) == false)
                    continue;

                HunterMarkLevel entry = config.Level(level);
                int count = Pick(_positions.Value.Get(hero).Value, config.Radius, Mathf.Min(entry.Targets, MaxTargets));
                int ticks = Mathf.Max(1, Mathf.RoundToInt(entry.DurationSeconds / _clock.Value.Delta));

                for (int index = 0; index < count; index++)
                    Mark(world, _picked[index], entry, ticks, config);
            }
        }

        private int Pick(Vector3 origin, float radius, int limit)
        {
            int count = 0;
            float radiusSquared = radius * radius;

            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;
                float distance = offset.x * offset.x + offset.z * offset.z;

                if (distance > radiusSquared)
                    continue;

                if (count == limit && distance >= _distances[count - 1])
                    continue;

                int slot = count < limit ? count++ : count - 1;

                while (slot > 0 && _distances[slot - 1] > distance)
                {
                    _distances[slot] = _distances[slot - 1];
                    _picked[slot] = _picked[slot - 1];
                    slot--;
                }

                _distances[slot] = distance;
                _picked[slot] = enemy;
            }

            return count;
        }

        private void Mark(EcsWorld world, int enemy, HunterMarkLevel entry, int ticks, HunterMarkConfig config)
        {
            bool fresh = _marks.Value.Has(enemy) == false;
            ref HunterMark mark = ref fresh ? ref _marks.Value.Add(enemy) : ref _marks.Value.Get(enemy);

            mark.RemainingTicks = Mathf.Max(mark.RemainingTicks, ticks);
            mark.DamageTakenBonus = Mathf.Max(mark.DamageTakenBonus, entry.DamageTakenBonus);
            mark.CritChanceBonus = Mathf.Max(mark.CritChanceBonus, entry.CritChanceBonus);

            if (fresh == false && mark.Fx.Unpack(world, out _))
                return;

            Vector3 position = _positions.Value.Get(enemy).Value;
            int fx = world.NewEntity();

            _markFx.Value.Add(fx).Target = world.PackEntity(enemy);
            _positions.Value.Add(fx).Value = position;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(config.MarkPrefab, position);

            mark.Fx = world.PackEntity(fx);
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnWaveSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<WaveState>> _timelines = default;
        private readonly EcsFilterInject<Inc<Enemy>> _live = default;
        private readonly EcsFilterInject<Inc<Player, Position>> _anchors = default;

        private readonly EcsPoolInject<WaveState> _waveStates = default;
        private readonly EcsPoolInject<Position> _positions = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        private RingSpawnPlacement _placement;
        private WeightedEnemyPicker _picker;
        private SimulationRandom _random;
        private EnemyFactory _factory;

        private WaveTimelineConfig _timeline;
        private bool[] _firedGuaranteed;
        private bool _timelineFinished;

        private float _spawnAccumulator;
        private float _refillAccumulator;

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();

            _timeline = _level.Value.Waves;

            WaveTimelineValidator.Validate(_timeline);

            _placement = new RingSpawnPlacement();
            _picker = new WeightedEnemyPicker();
            _random = new SimulationRandom(_context.Value.Seed);
            _factory = new EnemyFactory(world, _viewFactory.Value);

            _firedGuaranteed = new bool[WaveTimelineValidator.ResolveMaxGuaranteedCount(_timeline)];
            _timelineFinished = false;

            _spawnAccumulator = 0f;
            _refillAccumulator = 0f;

            int entity = world.NewEntity();

            ref WaveState state = ref _waveStates.Value.Add(entity);
            state.Index = 0;
            state.Elapsed = 0f;
        }

        public void Run(IEcsSystems systems)
        {
            if (_timeline == null)
                return;

            IReadOnlyList<Wave> waves = _timeline.Waves;

            if (waves == null || waves.Count == 0)
                return;

            float delta = _clock.Value.Delta;

            foreach (int entity in _timelines.Value)
            {
                ref WaveState state = ref _waveStates.Value.Get(entity);

                state.Elapsed += delta;

                if (_timelineFinished)
                    continue;

                if (state.Elapsed >= waves[state.Index].DurationSeconds)
                {
                    if (TryAdvanceWave(ref state, waves) == false)
                    {
                        _timelineFinished = true;
                        continue;
                    }

                    if (state.Index < 0 || state.Index >= waves.Count)
                        throw new InvalidOperationException(
                            $"{nameof(TryAdvanceWave)} left {nameof(WaveState)}.{nameof(WaveState.Index)} = {state.Index}, " +
                            $"which is outside 0..{waves.Count - 1}.");

                    Array.Clear(_firedGuaranteed, 0, _firedGuaranteed.Length);
                }

                Wave wave = waves[state.Index];

                bool hasAnchor = false;
                Vector3 anchor = Vector3.zero;

                foreach (int candidate in _anchors.Value)
                {
                    anchor = _positions.Value.Get(candidate).Value;
                    hasAnchor = true;
                    break;
                }

                if (hasAnchor == false)
                    continue;

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int index = 0; index < guaranteed.Count; index++)
                {
                    if (_firedGuaranteed[index])
                        continue;

                    GuaranteedSpawn entry = guaranteed[index];

                    if (state.Elapsed < entry.OffsetSeconds)
                        continue;

                    _firedGuaranteed[index] = true;

                    for (int copy = 0; copy < entry.Count; copy++)
                        Spawn(entry.Enemy, anchor);
                }

                int live = _live.Value.GetEntitiesCount();
                int fromRate = ResolveSpawnCount(ref _spawnAccumulator, wave.SpawnRatePerSecond, delta, live, wave.LiveCap);

                for (int index = 0; index < fromRate; index++)
                    SpawnFromWave(wave, anchor);

                live = _live.Value.GetEntitiesCount();

                if (live >= wave.LiveFloor)
                    continue;

                int fromRefill = ResolveSpawnCount(ref _refillAccumulator, wave.RefillRatePerSecond, delta, live, wave.LiveFloor);

                for (int index = 0; index < fromRefill; index++)
                    SpawnFromWave(wave, anchor);
            }
        }

        private int ResolveSpawnCount(ref float accumulator, float ratePerSecond, float deltaSeconds, int liveCount, int liveLimit)
        {
            accumulator += ratePerSecond * deltaSeconds;

            int result = (int)accumulator;

            accumulator -= result;

            if (liveCount + result > liveLimit)
                result = liveLimit - liveCount;

            return Math.Max(0, result);
        }

        private bool TryAdvanceWave(ref WaveState state, IReadOnlyList<Wave> waves)
        {
            if (state.Index + 1 < waves.Count)
            {
                state.Index++;
                state.Elapsed = 0f;

                return true;
            }

            return false;
        }

        private void SpawnFromWave(Wave wave, Vector3 anchor)
        {
            EnemyConfig enemy = _picker.Pick(wave.Enemies, _random.NextUnit());

            if (enemy == null)
                return;

            Spawn(enemy, anchor);
        }

        private void Spawn(EnemyConfig enemy, Vector3 anchor)
        {
            Vector3 spawnPosition = _placement.Resolve(
                anchor,
                _timeline.SpawnRadius,
                _level.Value.ArenaRadius,
                _random.NextUnit());

            _factory.Create(enemy, spawnPosition);
        }
    }
}

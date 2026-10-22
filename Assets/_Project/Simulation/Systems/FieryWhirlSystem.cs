using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FieryWhirlSystem : IEcsRunSystem
    {
        private const float FullCircleHalfAngle = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing, SwingStarted, SpecialSwing>> _started = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Swing> _swings = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<FlameWave> _waves = default;
        private readonly EcsPoolInject<FlameWavePillar> _pillars = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref Swing swing = ref _swings.Value.Get(entity);

                if ((swing.SourcePerk is CounterStrikeConfig) == false)
                    continue;

                if (swing.Owner.Unpack(world, out int hero) == false || swing.Weapon.Unpack(world, out int weapon) == false)
                    continue;

                if (_weaponDamages.Value.Has(weapon) == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out FieryWhirlConfig whirl, out int _) == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out FlameSwingConfig flame, out int flameLevel) == false)
                    continue;

                FlameSwingLevel entry = flame.Level(flameLevel);
                FlameWaveShape shape = FlameWaveShape.Of(flame, entry, FullCircleHalfAngle, whirl.PillarCount, whirl.SizeScale);
                float damage = _weaponDamages.Value.Get(weapon).Value * entry.DamageShare * whirl.DamageScale;

                Vector3 direction = _facings.Value.Get(hero).Value;
                direction.y = 0f;
                direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;

                FlameWaves.Spawn(world, hero, flame, _positions.Value.Get(hero).Value, direction, damage, in shape,
                    _waves.Value, _pillars.Value, _positions.Value, _facings.Value, _notIndexed.Value, _views.Value, _viewFactory.Value);
            }
        }
    }
}

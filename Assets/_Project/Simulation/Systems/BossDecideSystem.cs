using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;

namespace Game.Simulation.Systems
{
    public sealed class BossDecideSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Boss, EnemyAction>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;

        public void Init(IEcsSystems systems)
        {
            IReadOnlyList<EnemyConfig> registered = _content.Value.All<EnemyConfig>();

            for (int index = 0; index < registered.Count; index++)
            {
                EnemyConfig enemy = registered[index];

                if (enemy != null && enemy.Boss != null)
                    enemy.Boss.Validate(enemy);
            }
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref EnemyAction action = ref _actions.Value.Get(entity);

                if (action.Phase != EnemyActionPhase.None)
                    continue;

                if (action.GlobalCooldownTicks > 0)
                    action.GlobalCooldownTicks--;
            }
        }
    }
}

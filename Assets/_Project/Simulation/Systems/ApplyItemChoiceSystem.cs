using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ApplyItemChoiceSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, PendingItemChoice, PendingItemDrops>> _choosing = default;
        private readonly EcsFilterInject<Inc<EquippedItem>> _equipped = default;

        private readonly EcsPoolInject<PendingItemChoice> _choices = default;
        private readonly EcsPoolInject<PendingItemDrops> _drops = default;
        private readonly EcsPoolInject<EquippedItem> _equippedPool = default;

        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;
        private readonly EcsCustomInject<UpgradeChoiceGate> _gate = default;
        private readonly EcsCustomInject<IUpgradeChoiceInput> _input = default;
        private readonly EcsCustomInject<IItemChoiceSink> _sink = default;
        private readonly EcsCustomInject<ItemChoiceState> _state = default;

        public void Run(IEcsSystems systems)
        {
            if (_choosing.Value.GetEntitiesCount() == 0)
                return;

            if (_input.Value.TryConsume(out int index) == false)
                return;

            ItemChoiceState state = _state.Value;

            foreach (int hero in _choosing.Value)
            {
                if (index < 0 || index >= state.Offers.Count)
                    continue;

                Equip(hero, state.Slots[index], state.Offers[index]);

                _choices.Value.Del(hero);
                _drops.Value.Get(hero).Count--;
                _sink.Value.Hide();
                _gate.Value.Release();
            }
        }

        private void Equip(int hero, ItemSlot slot, ItemOfferData roll)
        {
            EcsWorld world = _world.Value;
            int target = -1;

            foreach (int entity in _equipped.Value)
            {
                ref EquippedItem item = ref _equippedPool.Value.Get(entity);

                if (item.Slot == slot && item.Owner.Unpack(world, out int owner) && owner == hero)
                    target = entity;
            }

            if (target < 0)
            {
                target = world.NewEntity();

                ref EquippedItem created = ref _equippedPool.Value.Add(target);
                created.Owner = world.PackEntity(hero);
                created.Slot = slot;
            }

            _equippedPool.Value.Get(target).Roll = roll;

            string source = ItemChoiceState.ModifierSource(slot);
            StatModifiers modifiers = _statModifiers.Value;

            modifiers.RemoveBySource(hero, source);
            modifiers.Add(hero, roll.Main.Stat, roll.Main.Op, roll.Main.Value, source);

            for (int minor = 0; minor < roll.Minors.Count; minor++)
                modifiers.Add(hero, roll.Minors[minor].Stat, roll.Minors[minor].Op, roll.Minors[minor].Value, source);
        }
    }
}

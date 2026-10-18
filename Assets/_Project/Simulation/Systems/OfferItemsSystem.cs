using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;

namespace Game.Simulation.Systems
{
    public sealed class OfferItemsSystem : IEcsRunSystem
    {
        public const string RandomStreamId = "stream.item-offers";

        private const int DuplicateRerolls = 8;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, PendingItemDrops>, Exc<Dead, PendingChoice, PendingItemChoice>> _waiting = default;
        private readonly EcsFilterInject<Inc<EquippedItem>> _equipped = default;

        private readonly EcsPoolInject<PendingItemDrops> _drops = default;
        private readonly EcsPoolInject<PendingItemChoice> _choices = default;
        private readonly EcsPoolInject<ItemRandom> _randoms = default;
        private readonly EcsPoolInject<EquippedItem> _equippedPool = default;
        private readonly EcsPoolInject<ItemProgress> _progress = default;
        private readonly EcsPoolInject<CombatStats> _combatStats = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<UpgradeChoiceGate> _gate = default;
        private readonly EcsCustomInject<IUpgradeChoiceInput> _input = default;
        private readonly EcsCustomInject<IItemChoiceSink> _sink = default;
        private readonly EcsCustomInject<ItemChoiceState> _state = default;

        private readonly List<int> _scratch = new List<int>();
        private readonly ItemFloors _floors = new ItemFloors();

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _waiting.Value)
            {
                ref PendingItemDrops drops = ref _drops.Value.Get(hero);

                if (drops.Count <= 0)
                    continue;

                ItemLootConfig loot = _level.Value.Waves == null ? null : _level.Value.Waves.ItemLoot;

                if (loot == null || _sink.Value == null || _state.Value == null)
                {
                    drops.Count = 0;

                    continue;
                }

                if (_randoms.Value.Has(hero) == false)
                    _randoms.Value.Add(hero).State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

                ref ItemRandom random = ref _randoms.Value.Get(hero);
                ItemChoiceState state = _state.Value;

                state.Offers.Clear();
                state.Slots.Clear();

                if (_progress.Value.Has(hero) == false)
                    _progress.Value.Add(hero);

                ref ItemProgress progress = ref _progress.Value.Get(hero);

                FillFloors(hero, loot.RankFloor(progress.Windows));
                progress.Windows++;

                float luck = _combatStats.Value.Has(hero) ? _combatStats.Value.Get(hero).Luck : 0f;

                for (int index = 0; index < loot.OfferCount; index++)
                {
                    ItemOfferData offer = ItemRoller.Roll(loot, ref random.State, _scratch, luck, _floors);

                    for (int reroll = 0; reroll < DuplicateRerolls && IsOffered(state, offer.ItemId); reroll++)
                        offer = ItemRoller.Roll(loot, ref random.State, _scratch, luck, _floors);

                    ItemSlot slot = _content.Value.Get<ItemConfig>(offer.ItemId).Slot;

                    offer.Equipped = FindEquipped(hero, slot);
                    state.Offers.Add(offer);
                    state.Slots.Add(slot);
                }

                _choices.Value.Add(hero);
                _gate.Value.Await();
                _input.Value.Clear();
                _sink.Value.Show(state.Offers);
            }
        }

        private static bool IsOffered(ItemChoiceState state, string itemId)
        {
            for (int index = 0; index < state.Offers.Count; index++)
            {
                if (state.Offers[index].ItemId == itemId)
                    return true;
            }

            return false;
        }

        private void FillFloors(int hero, int minRank)
        {
            EcsWorld world = _world.Value;

            _floors.Clear();
            _floors.MinRank = minRank;

            foreach (int entity in _equipped.Value)
            {
                ref EquippedItem item = ref _equippedPool.Value.Get(entity);

                if (item.Roll == null || item.Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                _floors.SetEquipped(item.Slot, item.Roll.Rank, item.Roll.Rarity);
            }
        }

        private ItemOfferData FindEquipped(int hero, ItemSlot slot)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _equipped.Value)
            {
                ref EquippedItem item = ref _equippedPool.Value.Get(entity);

                if (item.Slot == slot && item.Owner.Unpack(world, out int owner) && owner == hero)
                    return item.Roll;
            }

            return null;
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class OfferUpgradesSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string RandomStreamId = "stream.upgrade-offers";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, LevelUpEvent>, Exc<Dead>> _levelUps = default;
        private readonly EcsFilterInject<Inc<Player, PendingLevelUps>, Exc<Dead, PendingChoice>> _waiting = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<LevelUpEvent> _events = default;
        private readonly EcsPoolInject<PendingLevelUps> _pending = default;
        private readonly EcsPoolInject<PendingChoice> _choices = default;
        private readonly EcsPoolInject<UpgradeRandom> _randoms = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<UpgradeChoiceGate> _gate = default;
        private readonly EcsCustomInject<IUpgradeChoiceInput> _input = default;
        private readonly EcsCustomInject<IUpgradeChoiceSink> _sink = default;

        private PerkPool _pool;

        public void Init(IEcsSystems systems)
        {
            _pool = new PerkPool(_content.Value.All<PerkConfig>());
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _levelUps.Value)
            {
                ref LevelUpEvent levelUp = ref _events.Value.Get(hero);

                if (_pending.Value.Has(hero) == false)
                    _pending.Value.Add(hero);

                _pending.Value.Get(hero).Count += levelUp.Count;
            }

            foreach (int hero in _waiting.Value)
            {
                ref PendingLevelUps pending = ref _pending.Value.Get(hero);

                if (pending.Count <= 0)
                    continue;

                int offered = Draw(hero);

                if (offered == 0)
                {
                    pending.Count = 0;

                    continue;
                }

                Publish(hero, offered);
            }
        }

        private int Draw(int hero)
        {
            int[] candidates = _pool.Candidates;
            int candidateCount = 0;

            for (int index = 0; index < _pool.Count; index++)
            {
                if (LevelOf(hero, _pool[index].Id) >= _pool[index].MaxLevel)
                    continue;

                candidates[candidateCount] = index;
                candidateCount++;
            }

            if (candidateCount == 0)
                return 0;

            if (_randoms.Value.Has(hero) == false)
                _randoms.Value.Add(hero).State = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);

            return _pool.Draw(candidateCount, ref _randoms.Value.Get(hero).State);
        }

        private void Publish(int hero, int offered)
        {
            int[] candidates = _pool.Candidates;

            ref PendingChoice choice = ref _choices.Value.Add(hero);
            choice.Count = offered;
            choice.First = _pool[candidates[0]].Id;
            choice.Second = offered > 1 ? _pool[candidates[1]].Id : null;
            choice.Third = offered > 2 ? _pool[candidates[2]].Id : null;

            _gate.Value.Await();
            _input.Value.Clear();
            _sink.Value.Show(Offer(hero, choice.First), Offer(hero, choice.Second), Offer(hero, choice.Third), offered);
        }

        private UpgradeOffer Offer(int hero, string perkId)
        {
            if (perkId == null)
                return default;

            return new UpgradeOffer(perkId, LevelOf(hero, perkId) + 1);
        }

        private int LevelOf(int hero, string perkId)
        {
            int entity = TakenPerkLookup.Find(_world.Value, _taken.Value, _takenPool.Value, hero, perkId);

            return entity < 0 ? 0 : _takenPool.Value.Get(entity).Level;
        }
    }
}

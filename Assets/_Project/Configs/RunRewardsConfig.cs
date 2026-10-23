using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "RunRewardsConfig", menuName = "Game/Meta/Run Rewards Config")]
    public sealed class RunRewardsConfig : ContentConfig
    {
        [SerializeField] private int[] _magicShardsByRarity = { 5, 15, 40, 100 };

        public int MagicShards(Rarity rarity)
        {
            int index = (int)rarity;

            if (_magicShardsByRarity == null || index < 0 || index >= _magicShardsByRarity.Length)
                return 0;

            return _magicShardsByRarity[index];
        }
    }
}

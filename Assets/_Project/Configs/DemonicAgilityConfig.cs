using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "DemonicAgilityConfig", menuName = "Game/Content/Demonic Agility Config")]
    public sealed class DemonicAgilityConfig : PerkBehaviourConfig
    {
        public override int LevelCount => 1;

        public override void Validate(PerkConfig perk)
        {
        }
    }
}

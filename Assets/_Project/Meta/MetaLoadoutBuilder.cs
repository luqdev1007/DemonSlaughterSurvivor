using Game.Configs;
using Game.Core;
using System.Collections.Generic;

namespace Game.Meta
{
    public sealed class MetaLoadoutBuilder : IRunLoadoutSource
    {
        public const string GlobalSourcePrefix = "meta.global.";
        public const string ParagonSourcePrefix = "meta.paragon.";
        public const string TreeSourcePrefix = "meta.tree.";
        public const string WeaponBaseSourcePrefix = "meta.weapon-base.";
        public const string WeaponLevelSourcePrefix = "meta.weapon-level.";
        public const string TraitSourcePrefix = "meta.trait.";
        public const string GemSourcePrefix = "meta.gem.";
        public const string RelicSourcePrefix = "meta.relic.";

        private readonly MetaProgress _progress;
        private readonly IContentRegistry _content;

        private readonly List<StatModifierSpec> _modifiers = new List<StatModifierSpec>();
        private readonly List<TuningSpec> _tuning = new List<TuningSpec>();
        private readonly List<GrantedPerkSpec> _perks = new List<GrantedPerkSpec>();

        public MetaLoadoutBuilder(MetaProgress progress, IContentRegistry content)
        {
            _progress = progress;
            _content = content;
        }

        public RunLoadout Build(string characterId)
        {
            _modifiers.Clear();
            _tuning.Clear();
            _perks.Clear();

            AddGlobal();
            AddTree(characterId);
            AddWeapon(characterId);

            return new RunLoadout(_modifiers, _tuning, _perks);
        }

        private void AddGlobal()
        {
            IReadOnlyList<GlobalPowerConfig> globals = _content.All<GlobalPowerConfig>();

            if (globals.Count == 0)
                return;

            GlobalPowerConfig global = globals[0];

            foreach (MetaPassive passive in global.Passives)
                Add(passive.Effect, _progress.GlobalRank(passive.Id), GlobalSourcePrefix + passive.Id);

            if (_progress.IsParagonUnlocked == false)
                return;

            foreach (MetaPassive passive in global.Paragon)
                Add(passive.Effect, _progress.ParagonRank(passive.Id), ParagonSourcePrefix + passive.Id);
        }

        private void AddTree(string characterId)
        {
            HeroTreeConfig tree = _progress.TreeFor(characterId);

            if (tree == null)
                return;

            foreach (HeroTreeNode node in tree.Nodes)
                Add(node.Effect, _progress.NodeRank(characterId, node.Id), TreeSourcePrefix + node.Id);
        }

        private void AddWeapon(string characterId)
        {
            string weaponId = _progress.EquippedWeapon(characterId);

            if (string.IsNullOrEmpty(weaponId) || _content.TryGet(weaponId, out UniqueWeaponConfig weapon) == false)
                return;

            int level = _progress.WeaponLevel(weaponId);

            if (level <= 0)
                return;

            string baseSource = WeaponBaseSourcePrefix + weapon.Id;

            AddMore(StatId.WeaponDamage, weapon.DamageScale - 1f, baseSource);
            AddMore(StatId.AttackSpeed, weapon.AttackSpeedScale - 1f, baseSource);

            float levelBonus = weapon.LevelDamageBonus(level);

            if (levelBonus != 0f)
                _modifiers.Add(new StatModifierSpec(StatId.WeaponDamage, StatOp.Increased, levelBonus, WeaponLevelSourcePrefix + weapon.Id));

            foreach (WeaponTrait trait in weapon.Traits)
            {
                if (level >= trait.UnlockLevel)
                    Add(trait.Effect, 1, TraitSourcePrefix + trait.Id);
            }

            foreach (WeaponSocket socket in weapon.Sockets)
            {
                if (level < socket.UnlockLevel)
                    continue;

                string content = _progress.SocketContent(weapon.Id, socket.Id);

                if (string.IsNullOrEmpty(content))
                    continue;

                if (socket.Kind == SocketKind.Relic)
                {
                    if (_content.TryGet(content, out RelicConfig relic))
                        Add(relic.Effect, _progress.RelicLevel(relic.Id), RelicSourcePrefix + relic.Id);

                    continue;
                }

                if (_content.TryGet(content, out SocketGemConfig gem))
                    Add(gem.Effect, 1, GemSourcePrefix + weapon.Id + "." + socket.Id);
            }
        }

        private void AddMore(StatId stat, float value, string source)
        {
            if (value == 0f)
                return;

            _modifiers.Add(new StatModifierSpec(stat, StatOp.More, value, source));
        }

        private void Add(MetaEffect effect, int rank, string source)
        {
            if (rank <= 0 || effect.NotImplemented)
                return;

            foreach (StatModifierEntry entry in effect.Modifiers)
            {
                if (entry != null)
                    _modifiers.Add(new StatModifierSpec(entry.Stat, entry.Op, ScaleByRank(entry.Op, entry.Value, rank), source));
            }

            foreach (TuningEntry entry in effect.Tuning)
            {
                if (entry != null)
                    _tuning.Add(new TuningSpec(entry.Key, entry.Parameter, entry.Op, ScaleByRank(entry.Op, entry.Value, rank), source));
            }

            PerkConfig perk = effect.GrantedPerk;

            if (perk != null)
                _perks.Add(new GrantedPerkSpec(perk.Id, System.Math.Min(rank, perk.MaxLevel)));
        }

        private static float ScaleByRank(StatOp op, float value, int rank)
        {
            if (op == StatOp.More)
                return (float)System.Math.Pow(1.0 + value, rank) - 1f;

            return value * rank;
        }
    }
}

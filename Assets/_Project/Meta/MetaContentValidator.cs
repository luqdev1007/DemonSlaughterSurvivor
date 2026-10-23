using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using System.Text;

namespace Game.Meta
{
    public static class MetaContentValidator
    {
        public static void Validate(IContentRegistry content)
        {
            List<string> errors = new List<string>();

            IReadOnlyList<GlobalPowerConfig> globals = content.All<GlobalPowerConfig>();

            if (globals.Count > 1)
                errors.Add($"{globals.Count} {nameof(GlobalPowerConfig)} entries, at most one is allowed");

            foreach (GlobalPowerConfig global in globals)
                global.Validate(errors);

            HashSet<string> treeOwners = new HashSet<string>();

            foreach (HeroTreeConfig tree in content.All<HeroTreeConfig>())
            {
                tree.Validate(errors);

                if (tree.Character != null && treeOwners.Add(tree.Character.Id) == false)
                    errors.Add($"{tree.Id}: a second tree for '{tree.Character.Id}'");
            }

            Dictionary<string, int> startingWeapons = new Dictionary<string, int>();

            foreach (UniqueWeaponConfig weapon in content.All<UniqueWeaponConfig>())
            {
                weapon.Validate(errors);

                if (weapon.IsStarting && weapon.Character != null)
                {
                    startingWeapons.TryGetValue(weapon.Character.Id, out int count);
                    startingWeapons[weapon.Character.Id] = count + 1;
                }
            }

            foreach (KeyValuePair<string, int> pair in startingWeapons)
            {
                if (pair.Value > 1)
                    errors.Add($"'{pair.Key}' has {pair.Value} starting weapons, at most one is allowed");
            }

            foreach (SocketGemConfig gem in content.All<SocketGemConfig>())
                gem.Validate(errors);

            HashSet<string> relicBosses = new HashSet<string>();

            foreach (RelicConfig relic in content.All<RelicConfig>())
            {
                relic.Validate(errors);

                if (relic.Boss != null && relicBosses.Add(relic.Boss.Id) == false)
                    errors.Add($"{relic.Id}: a second relic for boss '{relic.Boss.Id}'");
            }

            if (errors.Count == 0)
                return;

            StringBuilder builder = new StringBuilder();
            builder.Append("Meta content is invalid, ");
            builder.Append(errors.Count);
            builder.AppendLine(" problem(s):");

            foreach (string error in errors)
            {
                builder.Append("  ");
                builder.AppendLine(error);
            }

            throw new ContentValidationException(builder.ToString());
        }
    }
}

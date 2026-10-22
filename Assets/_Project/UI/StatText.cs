using Game.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.UI
{
    public static class StatText
    {
        private static readonly Dictionary<StatId, string> Labels = new Dictionary<StatId, string>
        {
            { StatId.MoveSpeed, "скорость бега" },
            { StatId.MaxHealth, "здоровье" },
            { StatId.ContactDamage, "контактный урон" },
            { StatId.DashCooldown, "перезарядка рывка" },
            { StatId.WeaponDamage, "урон оружия" },
            { StatId.WeaponCooldown, "перезарядка оружия" },
            { StatId.AttackSpeed, "скорость атаки" },
            { StatId.DamageTaken, "получаемый урон" },
            { StatId.PickupRadius, "радиус сбора" },
            { StatId.LifeOnKill, "здоровья за убийство" },
            { StatId.LifeOnHit, "вампиризм (здоровья за удар)" },
            { StatId.SwingReach, "дальность удара" },
            { StatId.AbilityCooldown, "перезарядка способностей" },
            { StatId.CritChance, "шанс крит. удара" },
            { StatId.Armor, "защита от физ. урона" },
            { StatId.MagicResist, "защита от маг. урона" },
            { StatId.Luck, "удача" },
            { StatId.Evasion, "уклонение" },
            { StatId.AttackPower, "сила атаки" },
            { StatId.SpellPower, "сила заклинаний" },
            { StatId.DamageDealt, "наносимый урон" },
            { StatId.LifeStealScale, "сила вампиризма" },
            { StatId.MagicDamage, "магический урон" }
        };

        private static bool IsFraction(StatId stat)
        {
            return stat == StatId.LifeOnHit || stat == StatId.LifeOnKill || stat == StatId.CritChance || stat == StatId.Evasion
                || stat == StatId.LifeStealScale || stat == StatId.MagicDamage;
        }

        public static string Label(StatId stat)
        {
            return Labels.TryGetValue(stat, out string label) ? label : stat.ToString();
        }

        public static void AppendLine(StringBuilder builder, StatId stat, StatOp op, float value)
        {
            if (builder.Length > 0)
                builder.Append('\n');

            string sign = value >= 0f ? "+" : "−";
            float magnitude = value >= 0f ? value : -value;

            if (op == StatOp.Flat && IsFraction(stat) == false)
                builder.Append(sign).Append(magnitude.ToString("0.##", CultureInfo.InvariantCulture));
            else
                builder.Append(sign).Append((magnitude * 100f).ToString("0.#", CultureInfo.InvariantCulture)).Append('%');

            builder.Append(' ').Append(Label(stat));
        }
    }
}

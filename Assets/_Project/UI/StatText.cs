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
            { StatId.LifeOnKill, "здоровье за убийство" },
            { StatId.LifeOnHit, "вампиризм" }
        };

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

            if (op == StatOp.Flat)
                builder.Append(sign).Append(magnitude.ToString("0.##", CultureInfo.InvariantCulture));
            else
                builder.Append(sign).Append((magnitude * 100f).ToString("0.#", CultureInfo.InvariantCulture)).Append('%');

            builder.Append(' ').Append(Label(stat));
        }
    }
}

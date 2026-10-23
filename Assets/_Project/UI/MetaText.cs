using Game.Core;
using System.Globalization;

namespace Game.UI
{
    public static class MetaText
    {
        public const string Soon = "скоро: эффект ещё в разработке";

        public static string Value(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string Describe(string template, float perRank, int rank)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            return template.Replace("{0}", Value(perRank * (rank < 1 ? 1 : rank)));
        }

        public static string Cost(int cost, bool memory)
        {
            string color = MetaPalette.Tag(memory ? MetaPalette.Memory : MetaPalette.Magic);

            return $"<color={color}>{UiKit.Number(cost)} ◆</color>";
        }

        public static string Reason(MetaStatus status, bool memory)
        {
            switch (status)
            {
                case MetaStatus.NotEnoughCurrency:
                    return memory ? "Не хватает осколков воспоминаний" : "Не хватает магических осколков";
                case MetaStatus.Maxed:
                    return "Максимум";
                case MetaStatus.NeedsParents:
                    return "Сначала изучи предыдущий узел";
                case MetaStatus.Excluded:
                    return "Закрыто выбором другого ключевого узла";
                case MetaStatus.GateUnmet:
                    return "Условие не выполнено";
                case MetaStatus.NotForged:
                    return "Оружие не выковано";
                case MetaStatus.NotOwned:
                    return "Нет в инвентаре";
                case MetaStatus.SocketLocked:
                    return "Гнездо ещё закрыто";
                case MetaStatus.WrongColor:
                    return "Не подходит по цвету";
                case MetaStatus.Unavailable:
                    return "Откроется позже";
                default:
                    return string.Empty;
            }
        }

        public static string Pips(int rank, int max)
        {
            string on = MetaPalette.Tag(MetaPalette.Ember);
            string off = MetaPalette.Tag(MetaPalette.Line);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int index = 0; index < max; index++)
                builder.Append(index < rank ? $"<color={on}>■</color>" : $"<color={off}>■</color>");

            return builder.ToString();
        }
    }
}

using Game.Configs;
using Game.Core;
using System;
using System.Globalization;

namespace Game.UI
{
    public sealed class RunEndPresenter : IDisposable
    {
        private static readonly string[] RarityNames = { "обычный", "редкий", "эпический", "легендарный" };

        private readonly RunEndView _view;
        private readonly RunResult _result;
        private readonly IContentRegistry _content;
        private readonly IMetaProgress _meta;

        private bool _claimed;

        public RunEndPresenter(RunEndView view, RunResult result, IContentRegistry content, IMetaProgress meta)
        {
            _view = view;
            _result = result;
            _content = content;
            _meta = meta;

            _view.Bind(Claim, () => _result.RequestExit(RunExitChoice.Again), () => _result.RequestExit(RunExitChoice.Menu), () => _result.RequestExit(RunExitChoice.Upgrade));
            _view.HideImmediate();
        }

        public void Open()
        {
            RunReport report = _result.Report;
            RunRewards rewards = _result.Rewards;

            if (report == null || rewards == null)
                return;

            _claimed = false;
            _view.Show(BuildModel(report, rewards));
            _view.SetClaimed(false, string.Empty);
        }

        public void Dispose()
        {
            _view.Unbind();
        }

        private void Claim()
        {
            if (_claimed)
                return;

            _claimed = true;
            _view.SetClaimed(true, Wallet());
        }

        private string Wallet()
        {
            return $"В копилке: <color={MetaPalette.Tag(MetaPalette.Memory)}>{UiKit.Number(_meta.MemoryShards)}</color> осколков воспоминаний · " +
                   $"<color={MetaPalette.Tag(MetaPalette.Magic)}>{UiKit.Number(_meta.MagicShards)}</color> магических · " +
                   $"<color={MetaPalette.Tag(MetaPalette.Gold)}>{_meta.RelicCount}</color> реликвий";
        }

        private RunEndModel BuildModel(RunReport report, RunRewards rewards)
        {
            RunEndModel model = new RunEndModel();
            model.Victory = report.Victory;
            model.Title = report.Victory ? "ПОБЕДА" : "ПОРАЖЕНИЕ";

            string level = _content.TryGet(report.LevelId, out LevelConfig levelConfig) ? levelConfig.DisplayName : report.LevelId;
            string hero = _content.TryGet(report.CharacterId, out CharacterConfig character) ? character.DisplayName : report.CharacterId;
            string bossName = BossName(rewards.BossId);
            string fate = report.Victory
                ? $"{bossName ?? "босс"} повержен"
                : rewards.BossId != null ? $"пал на {UiKit.Time(report.Seconds)}, {bossName} повержен" : $"пал на {UiKit.Time(report.Seconds)}";

            model.Subtitle = $"{level} · {hero} · {UiKit.Time(report.Seconds)} · {fate}";

            model.MemoryShards = rewards.MemoryShards;
            model.MemoryLines.Add(("Эффективный урон", UiKit.Number(rewards.EffectiveDamage)));
            model.MemoryLines.Add(("× коэффициент уровня", rewards.DamageCoefficient.ToString("0.###", CultureInfo.InvariantCulture)));
            model.MemoryLines.Add(("= за урон", UiKit.Number(rewards.MemoryFromDamage)));
            model.MemoryLines.Add((bossName == null ? "Босс" : $"Босс «{bossName}»", rewards.BossBonus > 0 ? "+" + UiKit.Number(rewards.BossBonus) : "—"));
            model.MemoryNote = $"Оверкилл не учтён: {UiKit.Number(report.TotalDamage - report.EffectiveDamage)}";

            model.MagicShards = rewards.MagicShards;

            foreach (MagicShardLine line in rewards.MagicLines)
            {
                string rarity = line.Rarity >= 0 && line.Rarity < RarityNames.Length ? RarityNames[line.Rarity] : line.Rarity.ToString(CultureInfo.InvariantCulture);
                model.MagicLines.Add(($"{line.Count} × {rarity} (по {line.Weight})", UiKit.Number(line.Total)));
            }

            if (rewards.MagicLines.Count == 0)
                model.MagicLines.Add(("Предметов не найдено", "0"));

            model.MagicNote = "Удача повышает редкость находок";

            FillRelic(model, rewards);
            FillStats(model, report);

            return model;
        }

        private void FillRelic(RunEndModel model, RunRewards rewards)
        {
            if (string.IsNullOrEmpty(rewards.RelicId) || _content.TryGet(rewards.RelicId, out RelicConfig relic) == false)
            {
                model.RelicActive = false;
                model.RelicEyebrow = "НЕТ РЕЛИКВИИ";
                model.RelicName = "Босс не повержен";
                model.RelicText = "Реликвия выдаётся только за победу над боссом уровня.";

                return;
            }

            model.RelicName = relic.DisplayName;

            if (rewards.RelicRepeat)
            {
                model.RelicActive = false;
                model.RelicEyebrow = "ПОВТОРНАЯ ПОБЕДА";
                model.RelicText = "Реликвия уже в коллекции. Повторно не выпадает и ничем не компенсируется.";

                return;
            }

            model.RelicActive = true;
            model.RelicEyebrow = "РЕЛИКВИЯ БОССА";
            model.RelicText = $"<color={MetaPalette.Tag(MetaPalette.Bone)}>{relic.Echo}.</color> {relic.Description}";
        }

        private static void FillStats(RunEndModel model, RunReport report)
        {
            model.Stats.Add(("Лучший DPS", UiKit.Number(report.BestDps)));
            model.Stats.Add(("Всего нанесено урона", UiKit.Number(report.TotalDamage)));
            model.Stats.Add(("Получено урона", UiKit.Number(report.DamageTaken)));
            model.Stats.Add(("Время", UiKit.Time(report.Seconds)));
            model.Stats.Add(("Убито врагов", UiKit.Number(report.EnemiesKilled)));
            model.Stats.Add(("Убито элиток", UiKit.Number(report.ElitesKilled)));
            model.Stats.Add(("Найдено предметов", UiKit.Number(report.ItemsFound)));
            model.Stats.Add(("Подобрано сфер опыта", UiKit.Number(report.GemsCollected)));
            model.Stats.Add(("Разрушено объектов", UiKit.Number(report.BreakablesDestroyed)));
        }

        private string BossName(string bossId)
        {
            if (string.IsNullOrEmpty(bossId))
                return null;

            foreach (RelicConfig relic in _content.All<RelicConfig>())
            {
                if (relic.Boss != null && relic.Boss.Id == bossId && string.IsNullOrEmpty(relic.BossName) == false)
                    return relic.BossName;
            }

            return _content.TryGet(bossId, out BossConfig boss) ? boss.DisplayName : bossId;
        }
    }
}

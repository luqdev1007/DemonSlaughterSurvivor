using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public sealed class DamageNumbers : IDamageNumberSink
    {
        private const int PendingCapacity = 128;

        private readonly List<Request> _pending = new List<Request>(PendingCapacity);
        private readonly System.Random _random = new System.Random();

        public void Show(int targetKey, Vector3 position, float amount, DamageNumberStyle style)
        {
            _pending.Add(new Request(targetKey, position, amount, style));
        }

        public void Tick(DamageNumberLayerView layer, ICameraService camera, ICombatTextSettings settings, float deltaTime)
        {
            IReadOnlyList<DamageNumberItem> items = layer.Items;

            if (settings.DamageNumbersEnabled == false)
            {
                _pending.Clear();

                for (int index = 0; index < items.Count; index++)
                {
                    if (items[index].Active)
                        items[index].Deactivate();
                }

                return;
            }

            for (int index = 0; index < _pending.Count; index++)
                Spawn(layer, _pending[index]);

            _pending.Clear();

            Rect bounds = layer.Layer.rect;
            float userScale = settings.DamageNumberScale;

            for (int index = 0; index < items.Count; index++)
            {
                DamageNumberItem item = items[index];

                if (item.Active == false)
                    continue;

                item.Age += deltaTime;
                item.PopAge += deltaTime;

                if (item.Age >= layer.LifeSeconds)
                {
                    item.Deactivate();

                    continue;
                }

                if (camera.TryWorldToViewport(item.World, out Vector3 viewport) == false || viewport.z <= 0f)
                {
                    item.Text.alpha = 0f;

                    continue;
                }

                float progress = item.Age / layer.LifeSeconds;
                float rise = layer.RisePixels * (1f - (1f - progress) * (1f - progress));

                item.Rect.anchoredPosition = new Vector2(
                    (viewport.x - 0.5f) * bounds.width + item.Jitter,
                    (viewport.y - 0.5f) * bounds.height + rise);

                float pop = item.PopAge < layer.PopSeconds ? Mathf.Lerp(layer.PopScale, 1f, item.PopAge / layer.PopSeconds) : 1f;
                item.Rect.localScale = Vector3.one * (userScale * layer.StyleScale(item.Style) * pop);

                float fadeStart = layer.LifeSeconds - layer.FadeSeconds;
                item.Text.alpha = item.Age < fadeStart ? 1f : 1f - (item.Age - fadeStart) / layer.FadeSeconds;
            }
        }

        private void Spawn(DamageNumberLayerView layer, in Request request)
        {
            IReadOnlyList<DamageNumberItem> items = layer.Items;

            for (int index = 0; index < items.Count; index++)
            {
                DamageNumberItem existing = items[index];

                if (existing.Active == false || existing.Key != request.Key || existing.Age > layer.MergeSeconds)
                    continue;

                if (existing.Style != request.Style || IsCrit(request.Style))
                    continue;

                existing.Amount += request.Amount;
                existing.World = request.Position;
                existing.PopAge = 0f;
                Write(layer, existing);

                return;
            }

            DamageNumberItem item = layer.Acquire();

            item.Active = true;
            item.Key = request.Key;
            item.World = request.Position;
            item.Amount = request.Amount;
            item.Style = request.Style;
            item.Age = 0f;
            item.PopAge = 0f;
            item.Jitter = ((float)_random.NextDouble() * 2f - 1f) * layer.JitterPixels;
            item.Text.gameObject.SetActive(true);
            item.Text.alpha = 0f;

            Write(layer, item);
        }

        private static void Write(DamageNumberLayerView layer, DamageNumberItem item)
        {
            item.Text.color = layer.StyleColor(item.Style);

            if (item.Style == DamageNumberStyle.Evade)
            {
                item.Text.SetText("Уклон");

                return;
            }

            float shown = Mathf.Max(1f, Mathf.Round(item.Amount));

            if (IsCrit(item.Style))
                item.Text.SetText("{0:0}!", shown);
            else
                item.Text.SetText("{0:0}", shown);
        }

        private static bool IsCrit(DamageNumberStyle style)
        {
            return style == DamageNumberStyle.Crit || style == DamageNumberStyle.MagicCrit;
        }

        private readonly struct Request
        {
            public Request(int key, Vector3 position, float amount, DamageNumberStyle style)
            {
                Key = key;
                Position = position;
                Amount = amount;
                Style = style;
            }

            public int Key { get; }

            public Vector3 Position { get; }

            public float Amount { get; }

            public DamageNumberStyle Style { get; }
        }
    }
}

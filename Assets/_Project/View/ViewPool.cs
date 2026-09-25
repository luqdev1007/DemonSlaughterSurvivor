using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewPool : IViewFactory, IDisposable
    {
        private sealed class Entry
        {
            public ViewInterpolator View;
            public Animator Animator;
            public GameObject Prefab;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Dictionary<GameObject, Stack<Entry>> _free = new Dictionary<GameObject, Stack<Entry>>();
        private readonly Dictionary<int, Entry> _known = new Dictionary<int, Entry>();
        private readonly HashSet<int> _issued = new HashSet<int>();
        private readonly HashSet<int> _retiring = new HashSet<int>();
        private readonly List<Entry> _all = new List<Entry>();
        private readonly Dictionary<Material, Material> _dissolveCopies = new Dictionary<Material, Material>();

        private readonly float _hitFlashSeconds;
        private readonly float _blinkSeconds;
        private readonly float _dissolveSeconds;
        private readonly Material _dissolveTemplate;
        private readonly Action<ViewInterpolator> _onRetired;
        private readonly Func<Material, Material> _dissolveMaterialFor;
        private readonly System.Random _random = new System.Random();

        private readonly float _knockbackSpeed;
        private readonly float _knockbackUpSpeed;
        private readonly float _knockbackSpeedJitter;
        private readonly float _knockbackAngleJitterDegrees;
        private readonly float _knockbackGravity;
        private readonly float _knockbackFriction;

        private bool _disposed;

        public ViewPool(LevelConfig level)
        {
            FeedbackConfig feedback = level.Feedback;

            if (feedback == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no {nameof(FeedbackConfig)} assigned.");

            if (feedback.HitFlashSeconds < 0f || feedback.InvulnerabilityBlinkSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has a negative hit flash ({feedback.HitFlashSeconds}) " +
                    $"or blink period ({feedback.InvulnerabilityBlinkSeconds}). Use 0 to switch the effect off.");

            if (feedback.DissolveSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has a negative dissolve duration ({feedback.DissolveSeconds}).");

            if (feedback.DissolveMaterial == null)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has no dissolve material assigned. " +
                    "The hero dissolves with a copy of it on death.");

            if (feedback.KnockbackSpeed < 0f || feedback.KnockbackUpSpeed < 0f || feedback.KnockbackSpeedJitter < 0f ||
                feedback.KnockbackSpeedJitter >= 1f || feedback.KnockbackAngleJitterDegrees < 0f || feedback.KnockbackFriction < 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' has an invalid death knockback: speeds, angle jitter and friction " +
                    "must not be negative, and speed jitter must be in [0, 1). Set both speeds to zero to switch the knockback off.");

            if (feedback.KnockbackUpSpeed > 0f && feedback.KnockbackGravity <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(FeedbackConfig)} '{feedback.Id}' throws corpses up at {feedback.KnockbackUpSpeed} m/s " +
                    $"with a gravity of {feedback.KnockbackGravity}. Without a positive gravity they never land.");

            _hitFlashSeconds = feedback.HitFlashSeconds;
            _blinkSeconds = feedback.InvulnerabilityBlinkSeconds;
            _dissolveSeconds = feedback.DissolveSeconds;
            _dissolveTemplate = feedback.DissolveMaterial;
            _onRetired = CompleteRetire;
            _dissolveMaterialFor = DissolveMaterialFor;
            _knockbackSpeed = feedback.KnockbackSpeed;
            _knockbackUpSpeed = feedback.KnockbackUpSpeed;
            _knockbackSpeedJitter = feedback.KnockbackSpeedJitter;
            _knockbackAngleJitterDegrees = feedback.KnockbackAngleJitterDegrees;
            _knockbackGravity = feedback.KnockbackGravity;
            _knockbackFriction = feedback.KnockbackFriction;
        }

        public IView Create(GameObject prefab, Vector3 position)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ViewPool));

            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            Entry entry = Take(prefab);

            entry.View.gameObject.SetActive(true);
            entry.View.transform.SetPositionAndRotation(position, Quaternion.identity);
            entry.View.ResetInterpolation();
            entry.View.ResetFeedback();

            if (entry.Animator != null)
            {
                entry.Animator.Rebind();
                entry.Animator.Update(0f);
            }

            _issued.Add(entry.View.GetInstanceID());

            return entry.View;
        }

        public void Release(IView view)
        {
            if (_disposed)
                return;

            Entry entry = Withdraw(view, nameof(Release));

            Return(entry);
        }

        public void Retire(IView view, float seconds, Vector3 knockbackDirection)
        {
            if (_disposed)
                return;

            Entry entry = Withdraw(view, nameof(Retire));

            if (seconds <= 0f)
            {
                Return(entry);

                return;
            }

            _retiring.Add(entry.View.GetInstanceID());

            entry.View.BeginRetire(seconds, ResolveKnockbackVelocity(knockbackDirection), _knockbackGravity, _knockbackFriction);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (int index = 0; index < _all.Count; index++)
            {
                Entry entry = _all[index];

                if (entry.View == null)
                    continue;

                UnityEngine.Object.Destroy(entry.View.gameObject);
            }

            _all.Clear();
            _known.Clear();
            _issued.Clear();
            _retiring.Clear();
            _free.Clear();

            foreach (Material copy in _dissolveCopies.Values)
            {
                if (copy != null)
                    UnityEngine.Object.Destroy(copy);
            }

            _dissolveCopies.Clear();
        }

        private void CompleteRetire(ViewInterpolator view)
        {
            if (_disposed)
                return;

            if (_retiring.Remove(view.GetInstanceID()) == false)
                return;

            Return(_known[view.GetInstanceID()]);
        }

        private Entry Withdraw(IView view, string operation)
        {
            if (view is ViewInterpolator instance == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{operation} got a view of type " +
                    $"'{(view == null ? "<null>" : view.GetType().Name)}', which this pool never created. " +
                    $"Every view in a run comes from {nameof(ViewPool)}.{nameof(Create)}, and only those may be released back.");

            int id = instance.GetInstanceID();

            if (_known.ContainsKey(id) == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{operation} got the view '{instance.name}', which was not created by this pool. " +
                    "Accepting it would put a foreign object into the free list and hand it out as ours on the next spawn.");

            if (_issued.Remove(id) == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{operation} got the view '{instance.name}' a second time. " +
                    "A twice-released view would sit in the free list twice and be handed to two entities at once.");

            return _known[id];
        }

        private void Return(Entry entry)
        {
            entry.View.ResetInterpolation();
            entry.View.ResetFeedback();
            entry.View.gameObject.SetActive(false);

            Free(entry.Prefab).Push(entry);
        }

        private Entry Take(GameObject prefab)
        {
            Stack<Entry> free = Free(prefab);

            while (free.Count > 0)
            {
                Entry pooled = free.Pop();

                if (pooled.View == null)
                    continue;

                return pooled;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);

            if (instance.TryGetComponent(out ViewInterpolator view) == false)
            {
                UnityEngine.Object.Destroy(instance);

                throw new InvalidOperationException(
                    $"Prefab '{prefab.name}' has no {nameof(ViewInterpolator)} on its root object.");
            }

            Entry entry = new Entry
            {
                View = view,
                Animator = instance.GetComponentInChildren<Animator>(true),
                Prefab = prefab
            };

            view.Configure(entry.Animator, _hitFlashSeconds, _blinkSeconds, _dissolveSeconds, _dissolveMaterialFor, _onRetired);

            _known.Add(view.GetInstanceID(), entry);
            _all.Add(entry);

            return entry;
        }

        private Vector3 ResolveKnockbackVelocity(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 1e-8f)
                return Vector3.zero;

            float angle = Spread(_knockbackAngleJitterDegrees);
            float scale = 1f + Spread(_knockbackSpeedJitter);

            Vector3 horizontal = Quaternion.AngleAxis(angle, Vector3.up) * direction.normalized * (_knockbackSpeed * scale);

            return new Vector3(horizontal.x, _knockbackUpSpeed * scale, horizontal.z);
        }

        private float Spread(float amplitude)
        {
            return ((float)_random.NextDouble() * 2f - 1f) * amplitude;
        }

        private Material DissolveMaterialFor(Material source)
        {
            if (source == null)
                return null;

            if (_dissolveCopies.TryGetValue(source, out Material copy))
                return copy;

            copy = new Material(_dissolveTemplate)
            {
                name = source.name + " (Dissolve)",
                mainTexture = source.mainTexture,
                mainTextureScale = source.mainTextureScale,
                mainTextureOffset = source.mainTextureOffset
            };

            if (source.HasProperty(BaseColorId))
                copy.SetColor(BaseColorId, source.GetColor(BaseColorId));
            else if (source.HasProperty(ColorId))
                copy.SetColor(BaseColorId, source.GetColor(ColorId));

            _dissolveCopies.Add(source, copy);

            return copy;
        }

        private Stack<Entry> Free(GameObject prefab)
        {
            Stack<Entry> free;

            if (_free.TryGetValue(prefab, out free))
                return free;

            free = new Stack<Entry>();
            _free.Add(prefab, free);

            return free;
        }
    }
}

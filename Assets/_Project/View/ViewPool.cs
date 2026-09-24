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

        private readonly Dictionary<GameObject, Stack<Entry>> _free = new Dictionary<GameObject, Stack<Entry>>();
        private readonly Dictionary<int, Entry> _known = new Dictionary<int, Entry>();
        private readonly HashSet<int> _issued = new HashSet<int>();
        private readonly List<Entry> _all = new List<Entry>();

        private bool _disposed;

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

            if (view is ViewInterpolator instance == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{nameof(Release)} got a view of type " +
                    $"'{(view == null ? "<null>" : view.GetType().Name)}', which this pool never created. " +
                    $"Every view in a run comes from {nameof(ViewPool)}.{nameof(Create)}, and only those may be released back.");

            int id = instance.GetInstanceID();

            if (_known.ContainsKey(id) == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{nameof(Release)} got the view '{instance.name}', which was not created by this pool. " +
                    "Accepting it would put a foreign object into the free list and hand it out as ours on the next spawn.");

            if (_issued.Remove(id) == false)
                throw new InvalidOperationException(
                    $"{nameof(ViewPool)}.{nameof(Release)} got the view '{instance.name}' a second time. " +
                    "A twice-released view would sit in the free list twice and be handed to two entities at once.");

            Entry entry = _known[id];

            entry.View.ResetInterpolation();
            entry.View.gameObject.SetActive(false);

            Free(entry.Prefab).Push(entry);
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
            _free.Clear();
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

            _known.Add(view.GetInstanceID(), entry);
            _all.Add(entry);

            return entry;
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

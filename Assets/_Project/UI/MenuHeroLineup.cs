using DG.Tweening;
using Game.Configs;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

namespace Game.UI
{
    public sealed class MenuHeroLineup : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [Header("Stage")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _stand;
        [SerializeField, Min(0.5f)] private float _spacing = 1.8f;
        [SerializeField, Min(0f)] private float _arcDepth = 0.35f;
        [SerializeField, Min(0f)] private float _stepForward = 0.9f;
        [SerializeField, Min(0.05f)] private float _stepSeconds = 0.4f;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _blendSeconds = 0.25f;

        [Header("Highlight")]
        [SerializeField] private Light _selectionLight;
        [SerializeField] private Vector3 _lightOffset = new Vector3(0f, 4.5f, -0.6f);
        [SerializeField, Min(0.05f)] private float _lightSeconds = 0.35f;

        [Header("Labels")]
        [SerializeField] private TMP_Text _labelTemplate;
        [SerializeField] private Vector3 _labelOffset = new Vector3(0f, -0.15f, 0f);
        [SerializeField] private Color _selectedColor = new Color(1f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color _hoverColor = Color.white;
        [SerializeField] private Color _idleColor = new Color(0.62f, 0.6f, 0.58f, 1f);

        private readonly List<Hero> _heroes = new List<Hero>();
        private readonly Vector3[] _corners = new Vector3[8];
        private RectTransform _labelRoot;
        private int _selected = -1;
        private int _hovered = -1;
        private bool _interactable = true;

        public event Action<string> Selected;

        public void Build(IReadOnlyList<CharacterConfig> heroes, string selectedId)
        {
            if (_camera == null || _stand == null)
                throw new InvalidOperationException($"{nameof(MenuHeroLineup)} needs a camera and a stand.");

            if (_labelTemplate != null)
            {
                _labelTemplate.gameObject.SetActive(false);
                _labelRoot = (RectTransform)_labelTemplate.transform.parent;
            }

            for (int index = 0; index < heroes.Count; index++)
                _heroes.Add(Spawn(heroes[index], index, heroes.Count));

            int initial = IndexOf(selectedId);

            if (initial >= 0)
                Select(initial, false);
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;

            if (interactable == false)
                _hovered = -1;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_interactable == false || eventData.button != PointerEventData.InputButton.Left)
                return;

            int index = Pick(eventData.position);

            if (index < 0 || index == _selected)
                return;

            Select(index, true);
            Selected?.Invoke(_heroes[index].Id);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            _hovered = _interactable ? Pick(eventData.position) : -1;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = -1;
        }

        private Hero Spawn(CharacterConfig config, int index, int count)
        {
            float offset = (index - (count - 1) * 0.5f) * _spacing;
            float half = Mathf.Max(1f, (count - 1) * 0.5f * _spacing);
            float depth = _arcDepth * (offset / half) * (offset / half);
            Vector3 home = new Vector3(offset, 0f, depth);

            GameObject instance = Instantiate(config.ViewPrefab, _stand);
            instance.name = "MenuHero_" + config.Id;
            instance.transform.localPosition = home;

            Vector3 toCamera = _camera.transform.position - instance.transform.position;
            toCamera.y = 0f;
            instance.transform.rotation = toCamera.sqrMagnitude > Mathf.Epsilon ? Quaternion.LookRotation(toCamera) : Quaternion.identity;

            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            Hero hero = new Hero
            {
                Id = config.Id,
                Root = instance.transform,
                Home = home,
                Renderers = instance.GetComponentsInChildren<Renderer>(),
                Label = CreateLabel(config.DisplayName)
            };

            Animator animator = instance.GetComponentInChildren<Animator>(true);

            if (animator != null && config.MenuIdleClip != null)
                BuildGraph(ref hero, animator, config);

            return hero;
        }

        private void BuildGraph(ref Hero hero, Animator animator, CharacterConfig config)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PlayableGraph graph = PlayableGraph.Create("MenuHero_" + config.Id);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            bool layered = config.MenuSelectMask != null;
            Playable mixer = layered ? (Playable)AnimationLayerMixerPlayable.Create(graph, 2) : (Playable)AnimationMixerPlayable.Create(graph, 2);
            AnimationClipPlayable idle = AnimationClipPlayable.Create(graph, config.MenuIdleClip);
            idle.SetApplyFootIK(false);
            graph.Connect(idle, 0, mixer, 0);
            mixer.SetInputWeight(0, 1f);

            if (layered)
                ((AnimationLayerMixerPlayable)mixer).SetLayerMaskFromAvatarMask(1, config.MenuSelectMask);

            if (config.MenuSelectClip != null)
            {
                AnimationClipPlayable select = AnimationClipPlayable.Create(graph, config.MenuSelectClip);
                select.SetApplyFootIK(false);
                select.SetDuration(config.MenuSelectClip.length);
                graph.Connect(select, 0, mixer, 1);
                mixer.SetInputWeight(1, 0f);
                hero.Select = select;
                hero.SelectSeconds = config.MenuSelectClip.length;
            }

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();

            hero.Graph = graph;
            hero.Mixer = mixer;
            hero.Layered = layered;
            hero.HasGraph = true;
        }

        private TMP_Text CreateLabel(string text)
        {
            if (_labelTemplate == null)
                return null;

            TMP_Text label = Instantiate(_labelTemplate, _labelRoot);
            label.gameObject.SetActive(true);
            label.text = text;

            return label;
        }

        private void Select(int index, bool animate)
        {
            if (_selected >= 0)
            {
                Hero previous = _heroes[_selected];
                previous.Root.DOKill();
                previous.Root.DOLocalMove(previous.Home, _stepSeconds).SetEase(Ease.OutCubic);
                previous.Playing = false;
                _heroes[_selected] = previous;
            }

            _selected = index;
            Hero hero = _heroes[index];
            Vector3 forward = hero.Home + _stand.InverseTransformDirection(Flat(_camera.transform.position - hero.Root.position)) * _stepForward;

            hero.Root.DOKill();

            if (animate)
                hero.Root.DOLocalMove(forward, _stepSeconds).SetEase(Ease.OutCubic);
            else
                hero.Root.localPosition = forward;

            if (animate && hero.HasGraph && hero.SelectSeconds > 0f)
            {
                hero.Select.SetTime(0d);
                hero.Select.SetDone(false);
                hero.Playing = true;
                hero.Elapsed = 0f;
            }

            _heroes[index] = hero;

            MoveLight(_stand.TransformPoint(forward), animate);
        }

        private void MoveLight(Vector3 target, bool animate)
        {
            if (_selectionLight == null)
                return;

            _selectionLight.enabled = true;
            Transform light = _selectionLight.transform;
            Vector3 position = target + _lightOffset;
            Quaternion rotation = Quaternion.LookRotation(target + Vector3.up * 0.9f - position);

            light.DOKill();

            if (animate)
            {
                light.DOMove(position, _lightSeconds).SetEase(Ease.OutCubic);
                light.DORotateQuaternion(rotation, _lightSeconds).SetEase(Ease.OutCubic);
            }
            else
            {
                light.position = position;
                light.rotation = rotation;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int index = 0; index < _heroes.Count; index++)
            {
                Hero hero = _heroes[index];

                if (hero.HasGraph == false || hero.SelectSeconds <= 0f)
                    continue;

                float target = 0f;

                if (hero.Playing)
                {
                    hero.Elapsed += deltaTime;

                    if (hero.Elapsed >= hero.SelectSeconds - _blendSeconds)
                        hero.Playing = false;
                    else
                        target = 1f;
                }

                float weight = Mathf.MoveTowards(hero.Mixer.GetInputWeight(1), target, deltaTime / _blendSeconds);
                hero.Mixer.SetInputWeight(0, hero.Layered ? 1f : 1f - weight);
                hero.Mixer.SetInputWeight(1, weight);

                _heroes[index] = hero;
            }
        }

        private void LateUpdate()
        {
            if (_labelRoot == null)
                return;

            for (int index = 0; index < _heroes.Count; index++)
            {
                Hero hero = _heroes[index];

                if (hero.Label == null)
                    continue;

                Vector3 screen = _camera.WorldToScreenPoint(hero.Root.position + _labelOffset);
                bool visible = screen.z > 0f;

                if (hero.Label.gameObject.activeSelf != visible)
                    hero.Label.gameObject.SetActive(visible);

                if (visible == false)
                    continue;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_labelRoot, screen, null, out Vector2 local);
                hero.Label.rectTransform.anchoredPosition = local;
                hero.Label.color = index == _selected ? _selectedColor : index == _hovered ? _hoverColor : _idleColor;
            }
        }

        private int Pick(Vector2 pointer)
        {
            int best = -1;
            float bestDepth = float.MaxValue;

            for (int index = 0; index < _heroes.Count; index++)
            {
                if (TryScreenRect(_heroes[index], out Rect rect, out float depth) == false || rect.Contains(pointer) == false)
                    continue;

                if (depth >= bestDepth)
                    continue;

                bestDepth = depth;
                best = index;
            }

            return best;
        }

        private bool TryScreenRect(Hero hero, out Rect rect, out float depth)
        {
            rect = default;
            depth = 0f;

            if (hero.Renderers == null || hero.Renderers.Length == 0)
                return false;

            Bounds bounds = hero.Renderers[0].bounds;

            for (int index = 1; index < hero.Renderers.Length; index++)
            {
                if (hero.Renderers[index] != null && hero.Renderers[index].enabled)
                    bounds.Encapsulate(hero.Renderers[index].bounds);
            }

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            _corners[0] = new Vector3(min.x, min.y, min.z);
            _corners[1] = new Vector3(max.x, min.y, min.z);
            _corners[2] = new Vector3(min.x, max.y, min.z);
            _corners[3] = new Vector3(max.x, max.y, min.z);
            _corners[4] = new Vector3(min.x, min.y, max.z);
            _corners[5] = new Vector3(max.x, min.y, max.z);
            _corners[6] = new Vector3(min.x, max.y, max.z);
            _corners[7] = new Vector3(max.x, max.y, max.z);

            Vector2 low = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 high = new Vector2(float.MinValue, float.MinValue);

            for (int index = 0; index < _corners.Length; index++)
            {
                Vector3 screen = _camera.WorldToScreenPoint(_corners[index]);

                if (screen.z <= 0f)
                    return false;

                low = Vector2.Min(low, screen);
                high = Vector2.Max(high, screen);
            }

            rect = Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            depth = _camera.WorldToScreenPoint(bounds.center).z;

            return true;
        }

        private int IndexOf(string id)
        {
            for (int index = 0; index < _heroes.Count; index++)
            {
                if (_heroes[index].Id == id)
                    return index;
            }

            return -1;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;

            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.back;
        }

        private void OnDestroy()
        {
            for (int index = 0; index < _heroes.Count; index++)
            {
                Hero hero = _heroes[index];

                if (hero.Root != null)
                    hero.Root.DOKill();

                if (hero.HasGraph && hero.Graph.IsValid())
                    hero.Graph.Destroy();
            }

            if (_selectionLight != null)
                _selectionLight.transform.DOKill();

            _heroes.Clear();
        }

        private struct Hero
        {
            public string Id;
            public Transform Root;
            public Vector3 Home;
            public Renderer[] Renderers;
            public TMP_Text Label;
            public PlayableGraph Graph;
            public Playable Mixer;
            public bool Layered;
            public AnimationClipPlayable Select;
            public float SelectSeconds;
            public float Elapsed;
            public bool Playing;
            public bool HasGraph;
        }
    }
}

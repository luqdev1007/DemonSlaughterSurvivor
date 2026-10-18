using UnityEngine;
using UnityEngine.Rendering;

namespace Game.View
{
    internal sealed class BloodDecals
    {
        public const int PoolVariantA = 0;
        public const int PoolVariantB = 1;
        public const int SplatterVariant = 2;
        public const int StreakVariant = 3;

        private const float BaseHeight = 0.012f;
        private const float HeightStep = 0.00015f;
        private const int HeightLayers = 40;

        private sealed class Decal
        {
            public GameObject Object;
            public Transform Transform;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public Color32[] Colors;
            public bool Active;
            public float SpawnTime;
            public float Delay;
            public float GrowSeconds;
            public float LifeSeconds;
            public float FadeSeconds;
            public float DrySeconds;
            public bool Settled;
        }

        private readonly Material _material;
        private readonly Transform _parent;
        private readonly Decal[] _decals;
        private readonly System.Random _random;
        private readonly Vector2[] _uvs = new Vector2[4];

        private int _count;
        private int _layer;

        public BloodDecals(Material material, int capacity, Transform parent, System.Random random)
        {
            _material = material;
            _parent = parent;
            _random = random;
            _decals = new Decal[Mathf.Max(0, capacity)];
        }

        public void Spawn(Vector3 position, float yawDegrees, float size, int variant, float delay, float growSeconds, float lifeSeconds, float fadeSeconds, float drySeconds)
        {
            if (_material == null || _decals.Length == 0 || size <= 0f)
                return;

            Decal decal = Take();

            decal.Active = true;
            decal.Settled = false;
            decal.SpawnTime = Time.time;
            decal.Delay = delay;
            decal.GrowSeconds = Mathf.Max(0.01f, growSeconds);
            decal.LifeSeconds = lifeSeconds;
            decal.FadeSeconds = Mathf.Max(0.01f, fadeSeconds);
            decal.DrySeconds = Mathf.Max(0.01f, drySeconds);

            _layer = (_layer + 1) % HeightLayers;

            decal.Transform.SetPositionAndRotation(
                new Vector3(position.x, BaseHeight + _layer * HeightStep, position.z),
                Quaternion.Euler(0f, yawDegrees, 0f));
            decal.Transform.localScale = new Vector3(size, 1f, size);

            SetVariant(decal.Mesh, variant);
            Write(decal, 0f, 0f, 0f);

            decal.Object.SetActive(true);
        }

        public void Tick(float now)
        {
            for (int index = 0; index < _count; index++)
            {
                Decal decal = _decals[index];

                if (decal.Active == false || decal.Settled)
                    continue;

                float age = now - decal.SpawnTime - decal.Delay;

                if (age < 0f)
                    continue;

                float grow = Mathf.Clamp01(age / decal.GrowSeconds);
                float spread = 1f - (1f - grow) * (1f - grow) * (1f - grow);
                float dry = Mathf.Clamp01(age / decal.DrySeconds);
                float alpha = 1f;

                if (age > decal.LifeSeconds)
                {
                    alpha = 1f - (age - decal.LifeSeconds) / decal.FadeSeconds;

                    if (alpha <= 0f)
                    {
                        decal.Active = false;
                        decal.Object.SetActive(false);

                        continue;
                    }
                }

                Write(decal, spread * 0.97f, dry * 0.75f, alpha);
            }
        }

        private static void Write(Decal decal, float spread, float dry, float alpha)
        {
            byte r = (byte)Mathf.RoundToInt(Mathf.Clamp01(spread) * 255f);
            byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(dry) * 255f);
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);

            Color32 color = new Color32(r, g, 0, a);

            Color32[] colors = decal.Colors;

            if (colors[0].r == r && colors[0].g == g && colors[0].a == a)
                return;

            for (int index = 0; index < colors.Length; index++)
                colors[index] = color;

            decal.Mesh.colors32 = colors;
        }

        private Decal Take()
        {
            for (int index = 0; index < _count; index++)
            {
                if (_decals[index].Active == false)
                    return _decals[index];
            }

            if (_count < _decals.Length)
            {
                Decal created = Create(_count);
                _decals[_count] = created;
                _count++;

                return created;
            }

            Decal oldest = _decals[0];

            for (int index = 1; index < _count; index++)
            {
                if (_decals[index].SpawnTime < oldest.SpawnTime)
                    oldest = _decals[index];
            }

            return oldest;
        }

        private Decal Create(int index)
        {
            GameObject created = new GameObject("BloodDecal" + index);
            created.transform.SetParent(_parent, false);
            created.SetActive(false);

            Mesh mesh = new Mesh { name = "BloodDecal" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            mesh.uv = new Vector2[4];
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
            mesh.MarkDynamic();

            MeshFilter filter = created.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = created.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            return new Decal
            {
                Object = created,
                Transform = created.transform,
                Mesh = mesh,
                Renderer = renderer,
                Colors = new Color32[4]
            };
        }

        private void SetVariant(Mesh mesh, int variant)
        {
            float u = (variant % 2) * 0.5f;
            float v = (variant / 2) * 0.5f;
            bool flip = _random.NextDouble() < 0.5;

            float left = flip ? u + 0.5f : u;
            float right = flip ? u : u + 0.5f;

            _uvs[0] = new Vector2(left, v);
            _uvs[1] = new Vector2(left, v + 0.5f);
            _uvs[2] = new Vector2(right, v + 0.5f);
            _uvs[3] = new Vector2(right, v);

            mesh.uv = _uvs;
        }

        public void Dispose()
        {
            for (int index = 0; index < _count; index++)
            {
                if (_decals[index].Mesh != null)
                    Object.Destroy(_decals[index].Mesh);
            }
        }
    }
}

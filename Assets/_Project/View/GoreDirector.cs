using Game.Configs;
using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class GoreDirector : IDisposable
    {
        private const float MinDirectionSqr = 1e-8f;
        private const float FloorThickness = 1f;
        private const float FloorSize = 400f;

        private sealed class Gib
        {
            public GameObject Object;
            public Transform Transform;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public Rigidbody Body;
            public BoxCollider Collider;
            public bool Active;
            public bool Sinking;
            public bool Landed;
            public float Age;
            public float SpawnTime;
            public float SinkFromY;
            public int Drip;
            public GibPiece[] Pieces;
            public int PieceCount;
        }

        private sealed class GibPiece
        {
            public GameObject Object;
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Mesh Baked;
        }

        private readonly GoreConfig _config;
        private readonly IDebugGoreInput _debug;
        private readonly System.Random _random = new System.Random();
        private readonly GameObject _root;
        private readonly Gib[] _gibs;
        private readonly float[] _partWeights = new float[8];
        private readonly Action<int, Collision> _onGibContact;

        private readonly BloodFxPool _hitSprays;
        private readonly BloodFxPool _hitSplats;
        private readonly BloodFxPool _deathSprays;
        private readonly BloodFxPool _deathSplats;
        private readonly BloodFxPool _fountains;
        private readonly BloodFxPool _drips;
        private readonly BloodFxPool _impactSplats;

        private int _gibCount;
        private bool _disposed;

        public GoreDirector(LevelConfig level, IDebugGoreInput debug)
        {
            FeedbackConfig feedback = level.Feedback;

            if (feedback == null || feedback.Gore == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{level.Id}' has no {nameof(GoreConfig)} in its {nameof(FeedbackConfig)}.");

            _config = feedback.Gore;
            _debug = debug;

            Validate(_config);

            _gibs = new Gib[_config.MaxActiveGibs];

            _root = new GameObject("Gore");
            _root.AddComponent<GoreRunner>().Bind(Tick);

            BoxCollider floor = _root.AddComponent<BoxCollider>();
            floor.center = new Vector3(0f, -FloorThickness * 0.5f, 0f);
            floor.size = new Vector3(FloorSize, FloorThickness, FloorSize);

            _onGibContact = OnGibContact;

            Transform parent = _root.transform;

            _hitSprays = new BloodFxPool(_config.HitSpray, _config.MaxSprays, parent, 1f);
            _deathSprays = new BloodFxPool(_config.DeathSpray, _config.MaxSprays, parent, 1f);
            _hitSplats = new BloodFxPool(_config.HitSplat, _config.MaxSplats, parent, _config.SplatScale);
            _deathSplats = new BloodFxPool(_config.DeathSplat, _config.MaxSplats, parent, _config.SplatScale);
            _impactSplats = new BloodFxPool(_config.GibImpactSplat, _config.MaxSplats, parent, _config.SplatScale);
            _fountains = new BloodFxPool(_config.StumpFountain, _config.MaxFountains, parent, 1f);
            _drips = new BloodFxPool(_config.GibDrip, _config.MaxDrips, parent, 1f);
        }

        public void PlayHit(Vector3 position, Vector3 direction)
        {
            if (_disposed)
                return;

            PlaySpray(_hitSprays, position, direction);
            PlaySplat(_hitSplats, position, direction);
        }

        public void PlayDeath(Vector3 position, Vector3 direction)
        {
            if (_disposed)
                return;

            PlaySpray(_deathSprays, position, direction);
            PlaySplat(_deathSplats, position, direction);
        }

        public bool RollDismember(float chance, in DeathBlow blow)
        {
            if (_disposed || blow.IsSlash == false)
                return false;

            if (_debug != null && _debug.ForceDismember)
                return true;

            float scaled = blow.IsEmpowered ? chance * _config.BerserkChanceMultiplier : chance;

            return _random.NextDouble() < Mathf.Clamp01(scaled);
        }

        public void Dismember(DismemberableView view, Transform body, in DeathBlow blow, Vector3 corpseVelocity)
        {
            if (_disposed || view == null)
                return;

            int index = PickPart(view, body, blow.Direction);

            if (index < 0)
                return;

            DismemberableView.Part part = view.GetPart(index);

            Gib gib = SpawnGib(part, view.ResolveGibMaterials(part), blow.Direction, corpseVelocity);

            view.Detach(part);

            Vector3 gibCenter = gib.Transform.TransformPoint(gib.Mesh.bounds.center);
            Vector3 outward = gibCenter - part.Stump.position;

            if (outward.sqrMagnitude <= MinDirectionSqr)
                outward = Vector3.up;

            Quaternion world = Quaternion.LookRotation(outward.normalized);

            _fountains.PlayAttached(part.Stump, Vector3.zero, Quaternion.Inverse(part.Stump.rotation) * world, _config.FountainSeconds);

            gib.Drip = _drips.PlayAttached(gib.Transform, gib.Mesh.bounds.center, Quaternion.identity, 0f);
        }

        private void PlaySpray(BloodFxPool pool, Vector3 position, Vector3 direction)
        {
            Vector3 aim = direction;
            aim.y = 0f;
            aim = aim.sqrMagnitude > MinDirectionSqr ? aim.normalized : Vector3.zero;
            aim.y = _config.SprayLift;

            if (aim.sqrMagnitude <= MinDirectionSqr)
                aim = Vector3.up;

            pool.Play(position + Vector3.up * _config.HitHeight, Quaternion.LookRotation(aim.normalized));
        }

        private void PlaySplat(BloodFxPool pool, Vector3 position, Vector3 direction)
        {
            direction.y = 0f;

            float yaw = direction.sqrMagnitude > MinDirectionSqr
                ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                : (float)_random.NextDouble() * 360f;

            pool.Play(new Vector3(position.x, _config.SplatHeight, position.z), Quaternion.Euler(0f, yaw, 0f));
        }

        private void OnGibContact(int index, Collision collision)
        {
            if (_disposed || index < 0 || index >= _gibCount)
                return;

            Gib gib = _gibs[index];

            if (gib.Active == false || gib.Landed || collision.relativeVelocity.magnitude < _config.GibImpactMinSpeed)
                return;

            gib.Landed = true;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : gib.Transform.position;

            PlaySplat(_impactSplats, point, gib.Body.linearVelocity);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (int index = 0; index < _gibCount; index++)
            {
                Gib gib = _gibs[index];

                if (gib.Mesh != null)
                    UnityEngine.Object.Destroy(gib.Mesh);

                if (gib.Pieces == null)
                    continue;

                for (int piece = 0; piece < gib.Pieces.Length; piece++)
                {
                    if (gib.Pieces[piece] != null && gib.Pieces[piece].Baked != null)
                        UnityEngine.Object.Destroy(gib.Pieces[piece].Baked);
                }
            }

            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        private int PickPart(DismemberableView view, Transform body, Vector3 blowDirection)
        {
            int count = Mathf.Min(view.PartCount, _partWeights.Length);

            Vector3 toSource = -blowDirection;
            toSource.y = 0f;

            bool hasSide = toSource.sqrMagnitude > MinDirectionSqr;
            float side = hasSide ? Vector3.Dot(toSource.normalized, body.right) : 0f;
            bool frontal = Mathf.Abs(side) <= _config.FrontalBlowThreshold;
            float sharedArmWeight = (_config.NearArmWeight + _config.FarArmWeight) * 0.5f;

            float total = 0f;

            for (int index = 0; index < count; index++)
            {
                DismemberableView.Part part = view.GetPart(index);
                float weight;

                if (part.Kind == DismemberPart.Head)
                {
                    weight = _config.HeadWeight;
                }
                else if (frontal)
                {
                    weight = sharedArmWeight;
                }
                else
                {
                    float armSide = Vector3.Dot(part.Stump.position - body.position, body.right);
                    weight = Mathf.Sign(armSide) == Mathf.Sign(side) ? _config.NearArmWeight : _config.FarArmWeight;
                }

                if (part.IsAttached == false)
                    weight = 0f;

                _partWeights[index] = weight;
                total += weight;
            }

            if (total <= 0f)
                return -1;

            float roll = (float)_random.NextDouble() * total;

            for (int index = 0; index < count; index++)
            {
                roll -= _partWeights[index];

                if (roll < 0f)
                    return index;
            }

            return count - 1;
        }

        private Gib SpawnGib(DismemberableView.Part part, Material[] materials, Vector3 blowDirection, Vector3 corpseVelocity)
        {
            Gib gib = TakeGib();
            SkinnedMeshRenderer source = part.Renderer;

            source.BakeMesh(gib.Mesh, false);
            gib.Mesh.RecalculateBounds();

            gib.Transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            gib.Renderer.sharedMaterials = materials;

            Bounds bounds = gib.Mesh.bounds;

            AttachPieces(gib, part.Attachments, ref bounds);

            gib.Collider.center = bounds.center;
            gib.Collider.size = bounds.size * _config.GibColliderScale;
            gib.Collider.enabled = true;

            gib.Object.SetActive(true);

            gib.Body.isKinematic = false;
            gib.Body.mass = _config.GibMass;
            gib.Body.linearVelocity = ResolveGibVelocity(blowDirection, corpseVelocity);
            gib.Body.angularVelocity = RandomUnitVector() * (_config.GibSpinDegreesPerSecond * Mathf.Deg2Rad);

            gib.Active = true;
            gib.Sinking = false;
            gib.Landed = false;
            gib.Age = 0f;
            gib.SpawnTime = Time.time;
            gib.Drip = -1;

            return gib;
        }

        private void AttachPieces(Gib gib, Renderer[] attachments, ref Bounds bounds)
        {
            HidePieces(gib);

            if (gib.Pieces == null || gib.Pieces.Length < attachments.Length)
                Array.Resize(ref gib.Pieces, attachments.Length);

            Matrix4x4 toGib = gib.Transform.worldToLocalMatrix;

            for (int index = 0; index < attachments.Length; index++)
            {
                Renderer attachment = attachments[index];

                if (attachment == null || attachment.enabled == false)
                    continue;

                GibPiece piece = gib.Pieces[gib.PieceCount];

                if (piece == null)
                {
                    piece = CreatePiece(gib.Transform);
                    gib.Pieces[gib.PieceCount] = piece;
                }

                Transform source = attachment.transform;

                if (attachment is SkinnedMeshRenderer skinned)
                {
                    skinned.BakeMesh(piece.Baked, false);
                    piece.Baked.RecalculateBounds();
                    piece.Filter.sharedMesh = piece.Baked;
                    piece.Transform.SetPositionAndRotation(source.position, source.rotation);
                    piece.Transform.localScale = Vector3.one;
                }
                else
                {
                    MeshFilter filter = attachment.GetComponent<MeshFilter>();

                    if (filter == null || filter.sharedMesh == null)
                        continue;

                    piece.Filter.sharedMesh = filter.sharedMesh;
                    piece.Transform.SetPositionAndRotation(source.position, source.rotation);
                    piece.Transform.localScale = source.lossyScale;
                }

                piece.Renderer.sharedMaterials = attachment.sharedMaterials;
                piece.Object.SetActive(true);
                gib.PieceCount++;

                EncapsulateIn(ref bounds, toGib * piece.Transform.localToWorldMatrix, piece.Filter.sharedMesh.bounds);
            }
        }

        private static void EncapsulateIn(ref Bounds bounds, Matrix4x4 matrix, Bounds local)
        {
            Vector3 min = local.min;
            Vector3 max = local.max;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);

                bounds.Encapsulate(matrix.MultiplyPoint3x4(point));
            }
        }

        private GibPiece CreatePiece(Transform parent)
        {
            GameObject gameObject = new GameObject("Piece");
            gameObject.transform.SetParent(parent, false);

            Mesh baked = new Mesh { name = "Gib Piece" };
            baked.MarkDynamic();

            return new GibPiece
            {
                Object = gameObject,
                Transform = gameObject.transform,
                Filter = gameObject.AddComponent<MeshFilter>(),
                Renderer = gameObject.AddComponent<MeshRenderer>(),
                Baked = baked
            };
        }

        private static void HidePieces(Gib gib)
        {
            for (int index = 0; index < gib.PieceCount; index++)
                gib.Pieces[index].Object.SetActive(false);

            gib.PieceCount = 0;
        }

        private Vector3 ResolveGibVelocity(Vector3 blowDirection, Vector3 corpseVelocity)
        {
            Vector3 horizontal = blowDirection;
            horizontal.y = 0f;

            if (horizontal.sqrMagnitude <= MinDirectionSqr)
            {
                Vector3 random = RandomUnitVector();
                horizontal = new Vector3(random.x, 0f, random.z);
            }

            float scale = 1f + Spread(_config.GibSpeedJitter);

            Vector3 velocity = horizontal.normalized * (_config.GibSpeed * scale);
            velocity.y = _config.GibUpSpeed * scale;

            return velocity + corpseVelocity * _config.GibCorpseVelocityShare;
        }

        private Gib TakeGib()
        {
            for (int index = 0; index < _gibCount; index++)
            {
                if (_gibs[index].Active == false)
                    return _gibs[index];
            }

            if (_gibCount < _gibs.Length)
            {
                Gib created = CreateGib(_gibCount);
                _gibs[_gibCount] = created;
                _gibCount++;

                return created;
            }

            Gib oldest = _gibs[0];

            for (int index = 1; index < _gibCount; index++)
            {
                if (_gibs[index].SpawnTime < oldest.SpawnTime)
                    oldest = _gibs[index];
            }

            Deactivate(oldest);

            return oldest;
        }

        private Gib CreateGib(int number)
        {
            GameObject gameObject = new GameObject("Gib " + number);
            gameObject.SetActive(false);
            gameObject.transform.SetParent(_root.transform, false);

            Mesh mesh = new Mesh { name = "Gib " + number };
            mesh.MarkDynamic();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            gameObject.AddComponent<GibContact>().Bind(number, _onGibContact);

            return new Gib
            {
                Object = gameObject,
                Transform = gameObject.transform,
                Mesh = mesh,
                Renderer = gameObject.AddComponent<MeshRenderer>(),
                Body = body,
                Collider = gameObject.AddComponent<BoxCollider>()
            };
        }

        private void Tick(float deltaTime)
        {
            if (_disposed)
                return;

            TickGibs(deltaTime);

            float now = Time.time;

            _hitSprays.Tick(now);
            _deathSprays.Tick(now);
            _hitSplats.Tick(now);
            _deathSplats.Tick(now);
            _impactSplats.Tick(now);
            _fountains.Tick(now);
            _drips.Tick(now);
        }

        private void TickGibs(float deltaTime)
        {
            for (int index = 0; index < _gibCount; index++)
            {
                Gib gib = _gibs[index];

                if (gib.Active == false)
                    continue;

                gib.Age += deltaTime;

                if (gib.Sinking == false)
                {
                    if (gib.Age < _config.GibLifetimeSeconds)
                        continue;

                    gib.Sinking = true;
                    gib.Body.isKinematic = true;
                    gib.Collider.enabled = false;
                    gib.SinkFromY = gib.Transform.position.y;

                    _drips.Stop(gib.Drip, gib.Transform);
                }

                float sink = _config.GibSinkSeconds > 0f ? (gib.Age - _config.GibLifetimeSeconds) / _config.GibSinkSeconds : 1f;

                if (sink >= 1f)
                {
                    Deactivate(gib);

                    continue;
                }

                Vector3 position = gib.Transform.position;
                position.y = gib.SinkFromY - _config.GibSinkDepth * sink;
                gib.Transform.position = position;
            }
        }

        private static void Deactivate(Gib gib)
        {
            gib.Active = false;
            gib.Sinking = false;
            gib.Body.isKinematic = true;
            gib.Object.SetActive(false);
        }

        private Vector3 RandomUnitVector()
        {
            float z = Spread(1f);
            float angle = (float)_random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(1f - z * z);

            return new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), z);
        }

        private float Spread(float amplitude)
        {
            return ((float)_random.NextDouble() * 2f - 1f) * amplitude;
        }

        private static void Validate(GoreConfig config)
        {
            if (config.MaxActiveGibs <= 0)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' allows {config.MaxActiveGibs} active gibs; it needs at least one.");

            if (config.BerserkChanceMultiplier < 0f || config.HeadWeight < 0f || config.NearArmWeight < 0f || config.FarArmWeight < 0f)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' has a negative berserk multiplier or part weight.");

            if (config.GibLifetimeSeconds < 0f || config.GibSinkSeconds < 0f || config.GibSinkDepth < 0f)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' has a negative gib lifetime, sink duration or sink depth.");

            if (config.GibMass <= 0f || config.GibColliderScale <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' needs a positive gib mass and collider scale.");

            if (config.MaxSprays < 0 || config.MaxSplats < 0 || config.MaxFountains < 0 || config.MaxDrips < 0)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' has a negative blood effect limit. Use 0 to switch an effect off.");

            if (config.FountainSeconds < 0f || config.GibImpactMinSpeed < 0f || config.SplatScale <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' has a negative fountain duration or gib impact speed, or a non-positive splat scale.");

            if (config.GibSpeedJitter < 0f || config.GibSpeedJitter >= 1f)
                throw new InvalidOperationException(
                    $"{nameof(GoreConfig)} '{config.name}' has gib speed jitter {config.GibSpeedJitter}; it must be in [0, 1).");
        }
    }
}

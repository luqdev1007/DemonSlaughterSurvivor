using Game.Configs;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Components
{
    public struct Weapon { public WeaponConfig Config; }

    public struct WeaponDamage { public float Value; public float Base; }

    public struct WeaponCooldown { public float Value; public float Base; }

    public struct WeaponReady { public float Remaining; }

    public struct SwingRandom { public uint State; }

    public struct Swinging { }

    public struct SwingStarted { }

    public struct Swing : IEcsAutoReset<Swing>
    {
        public const int InitialHitCapacity = 64;

        public EcsPackedEntity Weapon;
        public EcsPackedEntity Owner;
        public int Variant;
        public float ClipTime;
        public Vector3 PreviousHand;
        public Vector3 PreviousTip;
        public EcsPackedEntity[] Hits;
        public int HitCount;

        public void AutoReset(ref Swing c)
        {
            if (c.Hits == null)
                c.Hits = new EcsPackedEntity[InitialHitCapacity];

            c.Weapon = default;
            c.Owner = default;
            c.Variant = 0;
            c.ClipTime = 0f;
            c.PreviousHand = default;
            c.PreviousTip = default;
            c.HitCount = 0;
        }
    }
}

using UnityEngine;

namespace Game.Core
{
    public readonly struct DeathBlow
    {
        public readonly Vector3 Direction;
        public readonly bool IsSlash;
        public readonly bool IsEmpowered;

        public DeathBlow(Vector3 direction, bool isSlash, bool isEmpowered)
        {
            Direction = direction;
            IsSlash = isSlash;
            IsEmpowered = isEmpowered;
        }
    }
}

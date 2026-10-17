using UnityEngine;

namespace Game.Core
{
    public interface IDamageNumberSink
    {
        void Show(int targetKey, Vector3 position, float amount, DamageNumberStyle style);
    }
}

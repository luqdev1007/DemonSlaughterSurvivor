using UnityEngine;

namespace DemonSlaughter.Gameplay.Combat
{
    public sealed class EcsEntityLink : MonoBehaviour
    {
        public int Entity { get; private set; }

        public void Initialize(int entity)
        {
            Entity = entity;
        }
    }
}
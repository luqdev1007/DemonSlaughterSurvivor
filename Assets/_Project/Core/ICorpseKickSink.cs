using UnityEngine;

namespace Game.Core
{
    public interface ICorpseKickSink
    {
        void HeroMoved(Vector3 position, Vector3 velocity);

        void Strike(Vector3 origin, Vector3 forward, float reach, float halfAngleDegrees);
    }
}

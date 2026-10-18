using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Tests
{
    internal sealed class RecordingViewFactory : IViewFactory
    {
        private readonly HashSet<IView> _live = new HashSet<IView>();

        public int Created;
        public int Released;

        public IView Create(GameObject prefab, Vector3 position)
        {
            Created++;
            RecordingView view = new RecordingView();
            _live.Add(view);

            return view;
        }

        public void Release(IView view)
        {
            if (_live.Remove(view))
                Released++;
        }

        public void Retire(IView view, float seconds, DeathBlow blow)
        {
            Release(view);
        }
    }

    internal sealed class RecordingView : IView
    {
        public readonly List<string> Triggers = new List<string>();

        public float Lift;

        public Transform Transform => null;

        public void SetPosition(Vector3 position)
        {
        }

        public void SetRotation(Quaternion rotation)
        {
        }

        public void PlayWeaponHit()
        {
        }

        public void PlayHit(Vector3 direction)
        {
        }

        public void SetInvulnerable(bool value)
        {
        }

        public void SetBerserk(bool value)
        {
        }

        public void SetPowerUp(bool active, float progress)
        {
        }

        public void SetDashing(bool value)
        {
        }

        public void SetRunning(bool value)
        {
        }

        public void SetSpecialAttack(bool value)
        {
        }

        public void SetLift(float height)
        {
            Lift = height;
        }

        public void PlayPushReaction(float seconds)
        {
        }

        public void PlayAttack(string trigger, float speed)
        {
            Triggers.Add(trigger);
        }

        public void SetAction(in ActionCue cue)
        {
        }

        public void PlayDeath()
        {
        }

        public void Dissolve()
        {
        }
    }
}

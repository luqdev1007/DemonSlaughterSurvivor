using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class GameSpeedHotkeys : ITickable
    {
        private const float Step = 0.1f;

        private static readonly Key[] Digits =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        private float _speed = 1f;

        public void Tick()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                for (int index = 0; index < Digits.Length; index++)
                {
                    if (keyboard[Digits[index]].wasPressedThisFrame == false)
                        continue;

                    _speed = (index + 1) * Step;
                    Debug.Log($"Game speed {Mathf.RoundToInt(_speed * 100f)}%");
                }
            }

            if (Time.timeScale > 0f && Mathf.Approximately(Time.timeScale, _speed) == false)
                Time.timeScale = _speed;
        }
    }
}

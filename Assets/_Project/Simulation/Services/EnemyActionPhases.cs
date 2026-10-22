using Game.Simulation.Components;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class EnemyActionPhases
    {
        public static int ToTicks(float seconds, float delta)
        {
            return Mathf.Max(1, Mathf.RoundToInt(seconds / delta));
        }

        public static int ToCooldownTicks(float seconds, float delta)
        {
            return Mathf.Max(0, Mathf.RoundToInt(seconds / delta));
        }

        public static void Enter(ref EnemyAction action, EnemyActionPhase phase, float seconds, float delta)
        {
            action.Phase = phase;
            action.PhaseTicks = ToTicks(seconds, delta);
            action.PhaseTotalTicks = action.PhaseTicks;
        }

        public static void Finish(ref EnemyAction action, float globalCooldownSeconds, float delta)
        {
            action.Phase = EnemyActionPhase.None;
            action.PhaseTicks = 0;
            action.PhaseTotalTicks = 0;
            action.GlobalCooldownTicks = ToCooldownTicks(globalCooldownSeconds, delta);
        }

        public static bool IsCharge(EnemyActionPhase phase)
        {
            return phase == EnemyActionPhase.ChargeWindup
                || phase == EnemyActionPhase.ChargeLock
                || phase == EnemyActionPhase.ChargeRush
                || phase == EnemyActionPhase.ChargeRecover;
        }

        public static bool IsSpin(EnemyActionPhase phase)
        {
            return phase == EnemyActionPhase.SpinRaise
                || phase == EnemyActionPhase.Spinning
                || phase == EnemyActionPhase.SpinRecover;
        }

        public static bool IsHeavyStrike(EnemyActionPhase phase)
        {
            return phase == EnemyActionPhase.StrikeWindup
                || phase == EnemyActionPhase.StrikeLock
                || phase == EnemyActionPhase.StrikeRecover;
        }

        public static bool IsJump(EnemyActionPhase phase)
        {
            return phase == EnemyActionPhase.JumpAir
                || phase == EnemyActionPhase.JumpRecover;
        }

        public static bool HoldsPosition(EnemyActionPhase phase)
        {
            return phase != EnemyActionPhase.None && phase != EnemyActionPhase.Spinning;
        }
    }
}

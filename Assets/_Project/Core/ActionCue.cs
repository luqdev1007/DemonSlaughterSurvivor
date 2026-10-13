namespace Game.Core
{
    public enum ActionPose
    {
        None = 0,
        ChargeWindup = 1,
        ChargeRush = 2,
        SpinRaise = 3,
        Spinning = 4,
        Recover = 5,
        StrikeWindup = 6,
        StrikeLock = 7,
        StrikeRecover = 8
    }

    public enum GroundMarker
    {
        None = 0,
        ChargeAim = 1,
        ChargeLocked = 2,
        SpinWarning = 3,
        SpinActive = 4,
        StrikeAim = 5,
        StrikeLocked = 6
    }

    public struct ActionCue
    {
        public ActionPose Pose;
        public float PlaybackSpeed;
        public GroundMarker Marker;
        public float MarkerLength;
        public float MarkerWidth;
        public float MarkerRadius;
        public float MarkerOffset;
        public float TurnDegreesPerSecond;
        public float Pulse;
        public float SpinDegreesPerSecond;
        public float ArmWobbleDegrees;
    }
}

namespace Game.Core
{
    public enum ActionPose
    {
        None = 0,
        ChargeWindup = 1,
        ChargeRush = 2,
        SpinRaise = 3,
        Spinning = 4,
        Recover = 5
    }

    public enum GroundMarker
    {
        None = 0,
        ChargeAim = 1,
        ChargeLocked = 2,
        SpinWarning = 3,
        SpinActive = 4
    }

    public struct ActionCue
    {
        public ActionPose Pose;
        public float PlaybackSpeed;
        public GroundMarker Marker;
        public float MarkerLength;
        public float MarkerWidth;
        public float MarkerRadius;
        public float Pulse;
        public float SpinDegreesPerSecond;
        public float ArmWobbleDegrees;
    }
}

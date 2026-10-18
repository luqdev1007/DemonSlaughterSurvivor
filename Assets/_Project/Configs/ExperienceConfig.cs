using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ExperienceConfig", menuName = "Game/Content/Experience Config")]
    public sealed class ExperienceConfig : ContentConfig
    {
        [Header("Level curve: required(level) = base + step x level")]
        [SerializeField] private int _baseRequired = 4;
        [SerializeField] private int _stepRequired = 3;

        [Header("Gem flight")]
        [SerializeField] private float _flightSeconds = 0.5f;
        [SerializeField] private float _flightPower = 2f;
        [SerializeField, Min(0f)] private float _flightAnticipation = 1.6f;
        [SerializeField, Min(0f)] private float _flightArc = 1.1f;

        public int BaseRequired => _baseRequired;

        public int StepRequired => _stepRequired;

        public float FlightSeconds => _flightSeconds;

        public float FlightPower => _flightPower;

        public float FlightAnticipation => _flightAnticipation;

        public float FlightArc => _flightArc;

        public int Required(int level)
        {
            return _baseRequired + _stepRequired * level;
        }
    }
}

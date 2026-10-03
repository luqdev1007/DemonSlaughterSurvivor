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

        public int BaseRequired => _baseRequired;

        public int StepRequired => _stepRequired;

        public float FlightSeconds => _flightSeconds;

        public float FlightPower => _flightPower;

        public int Required(int level)
        {
            return _baseRequired + _stepRequired * level;
        }
    }
}

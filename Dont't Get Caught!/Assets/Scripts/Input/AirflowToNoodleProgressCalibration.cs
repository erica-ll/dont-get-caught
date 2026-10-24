using UnityEngine;

namespace DontGetCaught.Input
{
    /// <summary>Maps normalized airflow linearly to noodle progress per second.</summary>
    [AddComponentMenu("Don't Get Caught/Input/Airflow to Noodle Progress Calibration")]
    public sealed class AirflowToNoodleProgressCalibration : MonoBehaviour
    {
        [Header("Airflow to noodle progress calibration")]
        [SerializeField, Range(0f, 1f), Tooltip("Normalized airflow at which inhaling starts and the speed mapping begins.")]
        private float minimumAirflow = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Normalized airflow mapped to the maximum noodle progress speed.")]
        private float maximumAirflow = 1f;
        [SerializeField, Min(0f), Tooltip("Noodle progress fraction added per second at Minimum Airflow.")]
        private float minimumProgressSpeed;
        [SerializeField, Min(0f), Tooltip("Noodle progress fraction added per second at Maximum Airflow.")]
        private float maximumProgressSpeed = 0.25f;

        public float MinimumAirflow => minimumAirflow;
        public float MaximumAirflow => maximumAirflow;
        public float MinimumProgressSpeed => minimumProgressSpeed;
        public float MaximumProgressSpeed => maximumProgressSpeed;

        public float ProgressSpeedForAirflow(float airflow)
        {
            float normalizedAirflow = Mathf.InverseLerp(minimumAirflow, maximumAirflow, airflow);
            return Mathf.Lerp(minimumProgressSpeed, maximumProgressSpeed, normalizedAirflow);
        }

        private void OnValidate()
        {
            minimumAirflow = Mathf.Clamp(minimumAirflow, 0f, 0.999f);
            maximumAirflow = Mathf.Clamp(maximumAirflow, minimumAirflow + 0.001f, 1f);
            minimumProgressSpeed = Mathf.Max(0f, minimumProgressSpeed);
            maximumProgressSpeed = Mathf.Max(minimumProgressSpeed, maximumProgressSpeed);
        }
    }
}

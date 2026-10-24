using System;
using UnityEngine;

namespace DontGetCaught.Input
{
    /// <summary>
    /// Selects the keyboard test source or the auto-reconnecting serial hardware source.
    /// Change Input Mode in the Inspector; gameplay reads the same normalized sample either way.
    /// </summary>
    public sealed class InputRouter : MonoBehaviour
    {
        public enum InputMode
        {
            Keyboard,
            Hardware
        }

        [SerializeField] private InputMode inputMode = InputMode.Keyboard;
        [SerializeField] private KeyboardInputSource keyboardSource;
        [SerializeField] private SerialInputSource hardwareSource;
        [SerializeField] private AirflowToNoodleProgressCalibration calibration;

        private IGameInputSource source;
        private InputMode appliedMode;
        private bool hasAppliedMode;
        private bool hasWarnedAboutSource;

        public InputMode CurrentMode => inputMode;
        public float MinimumAirflow => calibration != null ? calibration.MinimumAirflow : 0.1f;
        public float MaximumAirflow => calibration != null ? calibration.MaximumAirflow : 1f;
        public float MinimumProgressSpeed => calibration != null ? calibration.MinimumProgressSpeed : 0f;
        public float MaximumProgressSpeed => calibration != null ? calibration.MaximumProgressSpeed : 0.25f;
        public GameInputSample CurrentSample => source != null
            ? source.CurrentSample
            : GameInputSample.Disconnected;

        public string CurrentSourceName => source != null ? source.SourceName : $"{inputMode} input unavailable";
        public string CurrentSourceStatus => source is SerialInputSource serial
            ? serial.Status
            : source != null && source.CurrentSample.IsConnected
                ? source.SourceName
                : $"{inputMode} input unavailable";
        public bool HasRawDelta => source is SerialInputSource;
        public float CurrentRawDelta => source is SerialInputSource serial ? serial.RawDelta : 0f;
        public float CurrentProgressSpeed => ProgressSpeedForAirflow(CurrentSample.Airflow);

        /// <summary>Linearly maps normalized airflow into noodle progress per second.</summary>
        public float ProgressSpeedForAirflow(float airflow)
        {
            return calibration != null
                ? calibration.ProgressSpeedForAirflow(airflow)
                : Mathf.Lerp(0f, 0.25f, Mathf.InverseLerp(0.1f, 1f, airflow));
        }

        public event Action<GameInputSample> SampleUpdated;

        private void Awake()
        {
            ResolveSourceComponents();
            ApplyInputMode();
        }

        private void Update()
        {
            if (!hasAppliedMode || appliedMode != inputMode)
            {
                ApplyInputMode();
            }

            SampleUpdated?.Invoke(CurrentSample);
        }

        /// <summary>Supports UI buttons or other runtime controls in addition to the Inspector enum.</summary>
        public void SetInputMode(InputMode mode)
        {
            inputMode = mode;
            ApplyInputMode();
        }

        private void ResolveSourceComponents()
        {
            if (calibration == null) calibration = GetComponent<AirflowToNoodleProgressCalibration>();
            if (calibration == null) calibration = FindFirstObjectByType<AirflowToNoodleProgressCalibration>();
            if (keyboardSource == null) keyboardSource = FindFirstObjectByType<KeyboardInputSource>();
            if (hardwareSource == null) hardwareSource = FindFirstObjectByType<SerialInputSource>();
        }

        private void ApplyInputMode()
        {
            ResolveSourceComponents();
            appliedMode = inputMode;
            hasAppliedMode = true;

            bool useKeyboard = inputMode == InputMode.Keyboard;
            if (keyboardSource != null) keyboardSource.enabled = useKeyboard;
            if (hardwareSource != null) hardwareSource.enabled = !useKeyboard;

            source = useKeyboard ? keyboardSource as IGameInputSource : hardwareSource as IGameInputSource;
            if (source == null && !hasWarnedAboutSource)
            {
                Debug.LogWarning($"Input Mode is {inputMode}, but its input source is not assigned or found.", this);
                hasWarnedAboutSource = true;
            }
            else if (source != null)
            {
                hasWarnedAboutSource = false;
            }
        }
    }
}

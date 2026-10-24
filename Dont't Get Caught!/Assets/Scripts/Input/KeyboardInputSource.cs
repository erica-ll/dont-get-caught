using DontGetCaught.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontGetCaught.Input
{
    /// <summary>
    /// Keyboard fallback input for gameplay before physical hardware is ready.
    /// Hold Space to simulate airflow; F toggles the fork return state.
    /// </summary>
    public sealed class KeyboardInputSource : MonoBehaviour, IGameInputSource
    {
        [Header("Airflow")]
        [SerializeField] private Key inhaleKey = Key.Space;
        [SerializeField, Range(0f, 1f)] private float airflowStrength = 0.75f;
        [SerializeField, Range(0.05f, 1f)] private float airflowAdjustmentPerSecond = 0.5f;

        [Header("Fork Return Simulation")]
        [SerializeField] private Key forkToggleKey = Key.F;
        [SerializeField] private bool forkReturned = true;

        private Keyboard keyboard;
        private float currentAirflowStrength;

        public GameInputSample CurrentSample { get; private set; } = GameInputSample.Disconnected;
        public string SourceName => "Keyboard";

        private void Awake()
        {
            currentAirflowStrength = airflowStrength;
            keyboard = Keyboard.current;
        }

        private void Update()
        {
            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                CurrentSample = GameInputSample.Disconnected;
                return;
            }

            AdjustAirflowStrength();
            ToggleForkReturnState();

            float airflow = keyboard[inhaleKey].isPressed ? currentAirflowStrength : 0f;
            CurrentSample = new GameInputSample(airflow, forkReturned, true);
        }

        private void OnDisable()
        {
            CurrentSample = GameInputSample.Disconnected;
        }

        private void AdjustAirflowStrength()
        {
            float adjustment = 0f;
            if (keyboard.upArrowKey.isPressed || keyboard.equalsKey.isPressed)
            {
                adjustment += 1f;
            }

            if (keyboard.downArrowKey.isPressed || keyboard.minusKey.isPressed)
            {
                adjustment -= 1f;
            }

            currentAirflowStrength = Mathf.Clamp01(
                currentAirflowStrength + adjustment * airflowAdjustmentPerSecond * Time.deltaTime);
        }

        private void ToggleForkReturnState()
        {
            if (keyboard[forkToggleKey].wasPressedThisFrame)
            {
                forkReturned = !forkReturned;
            }

        }
    }
}

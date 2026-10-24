using System;
using DontGetCaught.Core;
using DontGetCaught.Input;
using UnityEngine;

namespace DontGetCaught.Noodle
{
    /// <summary>
    /// Converts airflow input into one noodle's progress.
    /// Progress resets after airflow remains below the threshold for the configured grace period.
    /// </summary>
    public sealed class NoodleEatingController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputRouter inputRouter;

        private float belowThresholdSeconds;
        private bool completionRaised;

        public float Progress { get; private set; }
        public bool IsEating { get; private set; }

        public event Action<float> ProgressChanged;
        public event Action NoodleCompleted;
        public event Action<bool> EatingChanged;

        private void Update()
        {
            if (gameManager == null || inputRouter == null)
            {
                SetEating(false);
                ResetProgress();
                return;
            }

            if (!CanEatInCurrentState())
            {
                SetEating(false);
                ResetProgress();
                return;
            }

            GameInputSample sample = inputRouter.CurrentSample;
            float airflow = sample.Airflow;
            float minimumAirflow = inputRouter.MinimumAirflow;

            if (!sample.IsConnected || sample.ForkReturned || airflow <= minimumAirflow)
            {
                SetEating(false);
                belowThresholdSeconds += Time.deltaTime;
                if (belowThresholdSeconds >= gameManager.Config.airflowSmoothingGraceSeconds)
                {
                    ResetProgress();
                }
                return;
            }

            belowThresholdSeconds = 0f;
            SetEating(true);

            float rate = inputRouter.ProgressSpeedForAirflow(airflow);
            SetProgress(Progress + rate * Time.deltaTime);

            if (Progress >= 1f && !completionRaised)
            {
                completionRaised = true;
                SetEating(false);
                NoodleCompleted?.Invoke();
            }
        }

        private bool CanEatInCurrentState()
        {
            return gameManager.CurrentState == GameState.Playing;
        }

        private void ResetProgress()
        {
            if (Progress > 0f)
            {
                SetProgress(0f);
            }

            completionRaised = false;
        }

        private void SetProgress(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(Progress, clamped))
            {
                return;
            }

            Progress = clamped;
            ProgressChanged?.Invoke(Progress);
        }

        private void SetEating(bool value)
        {
            if (IsEating == value)
            {
                return;
            }

            IsEating = value;
            EatingChanged?.Invoke(IsEating);
        }
    }
}





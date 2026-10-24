using DontGetCaught.Core;
using DontGetCaught.Input;
using DontGetCaught.Scoring;
using TMPro;
using UnityEngine;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Displays round stats, current state, and each prop return-point sensor separately.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text forkStationText;
        [SerializeField] private TMP_Text cupStationText;

        private int lastDisplayedSecond = -1;
        private string lastCaughtReason;
        private GameInputSample lastInputSample;
        private GameState displayedState = GameState.Ready;

        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<InputRouter>();
            if (gameManager != null)
            {
                gameManager.TimerChanged += HandleTimerChanged;
                gameManager.LivesChanged += HandleLivesChanged;
                gameManager.StateChanged += HandleStateChanged;
                gameManager.PlayerCaught += HandlePlayerCaught;
                HandleTimerChanged(gameManager.RemainingSeconds);
                HandleLivesChanged(gameManager.RemainingLives);
                displayedState = gameManager.CurrentState;
                RenderStatus();
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += HandleScoreChanged;
                HandleScoreChanged(scoreManager.TotalScore);
            }

            if (inputRouter != null)
            {
                inputRouter.SampleUpdated += HandleSampleUpdated;
                HandleSampleUpdated(inputRouter.CurrentSample);
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.TimerChanged -= HandleTimerChanged;
                gameManager.LivesChanged -= HandleLivesChanged;
                gameManager.StateChanged -= HandleStateChanged;
                gameManager.PlayerCaught -= HandlePlayerCaught;
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= HandleScoreChanged;
            }

            if (inputRouter != null)
            {
                inputRouter.SampleUpdated -= HandleSampleUpdated;
            }
        }

        private void HandleTimerChanged(float secondsRemaining)
        {
            int totalSeconds = Mathf.CeilToInt(secondsRemaining);
            if (totalSeconds == lastDisplayedSecond)
            {
                return;
            }

            lastDisplayedSecond = totalSeconds;
            if (timerText != null)
            {
                timerText.text = System.TimeSpan.FromSeconds(totalSeconds).ToString(@"mm\:ss");
            }
        }

        private void HandleLivesChanged(int remainingLives)
        {
            if (livesText != null)
            {
                livesText.text = $"Lives: {remainingLives}";
            }
        }

        private void HandleScoreChanged(int totalScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {totalScore}";
            }
        }

        private void HandleStateChanged(GameState previous, GameState current)
        {
            displayedState = current;
            RenderStatus();
        }

        private void HandlePlayerCaught(string reason, int remainingLives)
        {
            lastCaughtReason = reason;
            RenderStatus();
        }

        private void HandleSampleUpdated(GameInputSample sample)
        {
            lastInputSample = sample;
            string disconnected = sample.IsConnected ? string.Empty : " (no input)";
            if (forkStationText != null)
                forkStationText.text = $"F - Fork station: {(sample.ForkReturned ? "AT STATION" : "PICKED UP")}{disconnected}";
            if (cupStationText != null)
                cupStationText.text = $"C - Cup station: {(sample.CupReturned ? "AT STATION" : "PICKED UP")}{disconnected}";
            RenderStatus();
        }

        private void RenderStatus()
        {
            if (statusText == null) return;
            string message = displayedState switch
            {
                GameState.Ready => "Ready",
                GameState.Playing => "Playing - both props picked up",
                GameState.TeacherWarning => "Warning - stop inhaling and return both props",
                GameState.TeacherChecking => "Teacher checking - keep still and both props at their stations",
                GameState.CaughtRecovery => BuildCaughtStatus(),
                GameState.WaitingForPickup => "Teacher is away - pick up both props to resume",
                GameState.Won => "Round complete",
                GameState.Lost => "Game over",
                _ => displayedState.ToString()
            };
            if (forkStationText == null || cupStationText == null)
            {
                string disconnected = lastInputSample.IsConnected ? string.Empty : " (no input)";
                message += $"\nF - Fork station: {(lastInputSample.ForkReturned ? "AT STATION" : "PICKED UP")}{disconnected}";
                message += $"\nC - Cup station: {(lastInputSample.CupReturned ? "AT STATION" : "PICKED UP")}{disconnected}";
            }
            if (inputRouter != null)
            {
                message += $"\nInput: {inputRouter.CurrentSourceStatus}";
                if (inputRouter.HasRawDelta)
                {
                    message += $" | Delta: {inputRouter.CurrentRawDelta:0.0}";
                }
                message += $" | Airflow: {lastInputSample.Airflow:0.00}";
            }
            statusText.text = message;
        }

        private string BuildCaughtStatus()
        {
            return string.IsNullOrEmpty(lastCaughtReason)
                ? "Caught - stop inhaling and return both props"
                : $"Caught: {lastCaughtReason} Return both props, then pick them up to resume";
        }

    }
}

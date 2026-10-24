using DontGetCaught.Core;
using DontGetCaught.Input;
using DontGetCaught.Noodle;
using DontGetCaught.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Connects scene-authored HUD objects to the existing game events.
    /// Build and position the visual hierarchy in the Unity Editor; this component
    /// only updates text and progress values while the game runs.
    /// </summary>
    public sealed class ArtDirectedGameHUD : MonoBehaviour
    {
        [Header("Scene UI references")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text thirtySecondTimerText;
        [SerializeField] private TMP_Text tenSecondTimerText;
        [SerializeField] private GameObject regularTimerPanel;
        [SerializeField] private GameObject thirtySecondTimerPanel;
        [SerializeField] private GameObject tenSecondTimerPanel;
        [SerializeField] private TMP_Text roundStatusText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image[] player1LifeIcons;

        [Header("Temporary logic states (Canvas-root, unskinned)")]
        [SerializeField] private TMP_Text forkStatusDebugText;
        [SerializeField] private TMP_Text inputStatusDebugText;
        [SerializeField] private TMP_Text airflowStatusDebugText;
        [SerializeField] private TMP_Text teacherStatusDebugText;
        [SerializeField] private Image noodleProgressFill;

        [Header("Game systems (auto-found when left empty)")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private NoodleEatingController noodleController;

        private GameState currentState = GameState.Ready;
        private string lastCaughtReason;
        private int lastDisplayedSecond = -1;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<InputRouter>();
            if (noodleController == null) noodleController = FindFirstObjectByType<NoodleEatingController>();

            WarnIfRequiredReferencesAreMissing();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.TimerChanged += HandleTimerChanged;
                gameManager.LivesChanged += HandleLivesChanged;
                gameManager.StateChanged += HandleStateChanged;
                gameManager.PlayerCaught += HandlePlayerCaught;
                currentState = gameManager.CurrentState;
                HandleTimerChanged(gameManager.RemainingSeconds);
                HandleLivesChanged(gameManager.RemainingLives);
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

            if (noodleController != null)
            {
                noodleController.ProgressChanged += HandleProgressChanged;
                HandleProgressChanged(noodleController.Progress);
            }

            RenderState();
            RenderTeacherStatus();
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

            if (scoreManager != null) scoreManager.ScoreChanged -= HandleScoreChanged;
            if (inputRouter != null) inputRouter.SampleUpdated -= HandleSampleUpdated;
            if (noodleController != null) noodleController.ProgressChanged -= HandleProgressChanged;
        }

        private void HandleTimerChanged(float secondsRemaining)
        {
            int totalSeconds = Mathf.CeilToInt(secondsRemaining);
            UpdateTimerPanelVisibility(totalSeconds);
            if (totalSeconds == lastDisplayedSecond) return;
            lastDisplayedSecond = totalSeconds;

            string display = System.TimeSpan.FromSeconds(totalSeconds).ToString(@"mm\:ss");
            if (timerText != null) timerText.text = display;
            if (thirtySecondTimerText != null) thirtySecondTimerText.text = display;
            if (tenSecondTimerText != null) tenSecondTimerText.text = display;
        }

        private void UpdateTimerPanelVisibility(int secondsRemaining)
        {
            bool showTenSecond = secondsRemaining <= 10;
            bool showThirtySecond = secondsRemaining <= 30 && !showTenSecond;
            bool showRegular = !showTenSecond && !showThirtySecond;

            if (regularTimerPanel != null) regularTimerPanel.SetActive(showRegular);
            if (thirtySecondTimerPanel != null) thirtySecondTimerPanel.SetActive(showThirtySecond);
            if (tenSecondTimerPanel != null) tenSecondTimerPanel.SetActive(showTenSecond);
        }

        private void HandleLivesChanged(int lives)
        {
            if (player1LifeIcons == null) return;
            for (int i = 0; i < player1LifeIcons.Length; i++)
            {
                if (player1LifeIcons[i] != null)
                {
                    player1LifeIcons[i].gameObject.SetActive(i < lives);
                }
            }
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        private void HandleProgressChanged(float progress)
        {
            if (noodleProgressFill != null) noodleProgressFill.fillAmount = Mathf.Clamp01(progress);
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            currentState = next;
            RenderState();
            RenderTeacherStatus();
        }

        private void HandlePlayerCaught(string reason, int remainingLives)
        {
            lastCaughtReason = reason;
            RenderState();
            RenderTeacherStatus();
        }

        private void RenderTeacherStatus()
        {
            if (teacherStatusDebugText == null) return;

            teacherStatusDebugText.text = currentState switch
            {
                GameState.TeacherWarning => "[LOGIC TEST / UNSKINNED] TEACHER: WARNING / APPROACHING",
                GameState.TeacherChecking => "[LOGIC TEST / UNSKINNED] TEACHER: CHECKING",
                GameState.CaughtRecovery => string.IsNullOrEmpty(lastCaughtReason)
                    ? "[LOGIC TEST / UNSKINNED] TEACHER: CAUGHT / RECOVERY"
                    : $"[LOGIC TEST / UNSKINNED] TEACHER: CAUGHT / {lastCaughtReason.ToUpperInvariant()}",
                GameState.WaitingForPickup => "[LOGIC TEST / UNSKINNED] TEACHER: AWAY / WAITING FOR FORK PICKUP",
                _ => "[LOGIC TEST / UNSKINNED] TEACHER: INACTIVE"
            };
        }

        private void HandleSampleUpdated(GameInputSample sample)
        {
            if (forkStatusDebugText != null)
            {
                forkStatusDebugText.text = sample.ForkReturned
                    ? "[LOGIC TEST / UNSKINNED] FORK: AT STATION"
                    : "[LOGIC TEST / UNSKINNED] FORK: PICKED UP";
            }
            if (inputStatusDebugText != null)
            {
                string sourceStatus = inputRouter != null ? inputRouter.CurrentSourceStatus : "Input unavailable";
                inputStatusDebugText.text = sample.IsConnected
                    ? $"[LOGIC TEST / UNSKINNED] INPUT: {sourceStatus}"
                    : "[LOGIC TEST / UNSKINNED] INPUT: DISCONNECTED";
                inputStatusDebugText.color = sample.IsConnected
                    ? new Color(0.18f, 0.09f, 0.05f, 1f)
                    : new Color(0.7f, 0.08f, 0.05f, 1f);
            }
            if (airflowStatusDebugText != null)
            {
                float progressSpeed = inputRouter != null
                    ? inputRouter.ProgressSpeedForAirflow(sample.Airflow)
                    : 0f;
                airflowStatusDebugText.text =
                    $"[LOGIC TEST / UNSKINNED] AIRFLOW: {sample.Airflow:0.00} | NOODLE SPEED: {progressSpeed:0.000}/s";
            }
        }

        private void RenderState()
        {
            if (roundStatusText == null) return;

            Color normal = new Color(0.18f, 0.09f, 0.05f, 1f);
            Color warning = new Color(0.76f, 0.12f, 0.05f, 1f);
            switch (currentState)
            {
                case GameState.Ready:
                    roundStatusText.text = "PICK UP THE FORK TO PLAY";
                    roundStatusText.color = normal;
                    break;
                case GameState.Playing:
                    roundStatusText.text = "KEEP EATING BEFORE THE TEACHER LOOKS";
                    roundStatusText.color = normal;
                    break;
                case GameState.TeacherWarning:
                    roundStatusText.text = "TEACHER IS COMING! STOP INHALING AND HIDE THE FORK";
                    roundStatusText.color = warning;
                    break;
                case GameState.TeacherChecking:
                    roundStatusText.text = "STAY STILL — FORK RETURNED";
                    roundStatusText.color = warning;
                    break;
                case GameState.CaughtRecovery:
                    roundStatusText.text = string.IsNullOrEmpty(lastCaughtReason)
                        ? "CAUGHT! RETURN THE FORK"
                        : $"CAUGHT: {lastCaughtReason.ToUpperInvariant()}";
                    roundStatusText.color = warning;
                    break;
                case GameState.WaitingForPickup:
                    roundStatusText.text = "TEACHER AWAY — PICK UP THE FORK";
                    roundStatusText.color = normal;
                    break;
                case GameState.Won:
                    roundStatusText.text = "ROUND COMPLETE — YOU WIN!";
                    roundStatusText.color = new Color(0.1f, 0.42f, 0.16f, 1f);
                    break;
                case GameState.Lost:
                    roundStatusText.text = "GAME OVER";
                    roundStatusText.color = warning;
                    break;
            }
        }

        private void WarnIfRequiredReferencesAreMissing()
        {
            if (timerText == null || roundStatusText == null || scoreText == null
                || noodleProgressFill == null || player1LifeIcons == null || player1LifeIcons.Length == 0)
            {
                Debug.LogWarning(
                    "ArtDirectedGameHUD needs Timer, Round Status, Score, Noodle Progress, and Player 1 life icon " +
                    "references assigned in the Inspector. Canvas-root fork/input debug text fields are optional.", this);
            }
        }
    }
}

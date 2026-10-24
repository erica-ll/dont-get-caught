using System;
using DontGetCaught.Input;
using UnityEngine;

namespace DontGetCaught.Core
{
    /// <summary>
    /// Owns round state, timer, lives, teacher-check results, and fork pickup gating.
    /// Other systems request transitions through this component instead of setting state directly.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private bool startRoundOnPlay = true;

        private float caughtRecoveryElapsed;
        private bool recoveryTimerResumed;
        private bool hasWarnedAboutMissingInput;

        public GameState CurrentState { get; private set; } = GameState.Ready;
        public float RemainingSeconds { get; private set; }
        public int RemainingLives { get; private set; }
        public GameConfig Config => config;
        public bool IsRoundActive => CurrentState == GameState.Playing
                                     || CurrentState == GameState.TeacherWarning
                                     || CurrentState == GameState.TeacherChecking
                                     || CurrentState == GameState.CaughtRecovery
                                     || CurrentState == GameState.WaitingForPickup;

        public event Action<GameState, GameState> StateChanged;
        public event Action<float> TimerChanged;
        public event Action<int> LivesChanged;
        public event Action<string, int> PlayerCaught;
        public event Action<GameState> RoundEnded;

        private void Awake()
        {
            if (inputRouter == null)
            {
                inputRouter = FindFirstObjectByType<InputRouter>();
            }

            RemainingSeconds = Mathf.Max(1f, config.roundDurationSeconds);
            RemainingLives = Mathf.Max(1, config.startingLives);
        }

        private void Start()
        {
            if (startRoundOnPlay)
            {
                StartRound();
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.CaughtRecovery)
            {
                caughtRecoveryElapsed += Time.deltaTime;
                if (!recoveryTimerResumed
                    && caughtRecoveryElapsed >= config.caughtRecoveryTimeoutSeconds)
                {
                    recoveryTimerResumed = true;
                }

                TryCompleteCaughtRecovery();
            }
            else if (CurrentState == GameState.WaitingForPickup)
            {
                TryCompletePickup();
            }

            if (!ShouldAdvanceRoundTimer())
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            TimerChanged?.Invoke(RemainingSeconds);

            if (RemainingSeconds <= 0f)
            {
                FinishRound(GameState.Won);
            }
        }

        /// <summary>Starts or restarts a round and resets its timer, lives, and recovery timer.</summary>
        public void StartRound()
        {
            if (IsRoundActive)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(1f, config.roundDurationSeconds);
            RemainingLives = Mathf.Max(1, config.startingLives);
            caughtRecoveryElapsed = 0f;
            recoveryTimerResumed = false;
            TimerChanged?.Invoke(RemainingSeconds);
            LivesChanged?.Invoke(RemainingLives);
            GameInputSample sample = inputRouter != null ? inputRouter.CurrentSample : GameInputSample.Disconnected;
            ChangeState(sample.IsConnected && !sample.ForkReturned
                ? GameState.Playing
                : GameState.WaitingForPickup);
        }

        /// <summary>Enters the warning phase while the round is active.</summary>
        public void BeginTeacherWarning()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.TeacherWarning);
            }
        }

        /// <summary>Begins a teacher check. Teacher AI or the temporary test input can call this.</summary>
        public void BeginTeacherChecking()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.TeacherWarning)
            {
                ChangeState(GameState.TeacherChecking);
            }
        }

        /// <summary>
        /// Resolves a teacher check using the latest normalized input sample.
        /// A successful check leads to WaitingForPickup, never directly to Playing.
        /// </summary>
        public void ResolveTeacherCheck()
        {
            if (CurrentState != GameState.TeacherChecking)
            {
                return;
            }

            if (!TryGetConnectedSample(out GameInputSample sample))
            {
                return;
            }

            if (sample.Airflow > MinimumAirflow)
            {
                ReportCaught("Still inhaling!");
                return;
            }

            if (!sample.ForkReturned)
            {
                ReportCaught("Fork is not at its return point!");
                return;
            }

            ChangeState(GameState.WaitingForPickup);
        }

        /// <summary>Reports a failed teacher check and enters recovery or loss.</summary>
        public void ReportCaught(string reason)
        {
            if (CurrentState != GameState.TeacherChecking)
            {
                Debug.LogWarning("A catch can only be reported while the teacher is checking.", this);
                return;
            }

            RemainingLives = Mathf.Max(0, RemainingLives - 1);
            LivesChanged?.Invoke(RemainingLives);
            PlayerCaught?.Invoke(reason, RemainingLives);

            if (RemainingLives <= 0)
            {
                FinishRound(GameState.Lost);
                return;
            }

            caughtRecoveryElapsed = 0f;
            recoveryTimerResumed = false;
            ChangeState(GameState.CaughtRecovery);
        }

        /// <summary>Attempts to complete recovery; the fork must be returned and airflow stopped.</summary>
        public void CompleteRecovery()
        {
            if (CurrentState == GameState.CaughtRecovery)
            {
                TryCompleteCaughtRecovery();
            }
        }

        /// <summary>Ends a round with a win or loss result.</summary>
        public void FinishRound(GameState result)
        {
            if (result != GameState.Won && result != GameState.Lost)
            {
                Debug.LogWarning($"Rejected round result: {result}.", this);
                return;
            }

            if (!IsRoundActive)
            {
                return;
            }

            ChangeState(result);
            RoundEnded?.Invoke(result);
        }

        private bool ShouldAdvanceRoundTimer()
        {
            return CurrentState == GameState.Playing
                   || CurrentState == GameState.TeacherWarning
                   || CurrentState == GameState.TeacherChecking
                   || CurrentState == GameState.WaitingForPickup
                   || (CurrentState == GameState.CaughtRecovery && recoveryTimerResumed);
        }

        private void TryCompleteCaughtRecovery()
        {
            if (!TryGetConnectedSample(out GameInputSample sample))
            {
                return;
            }

            bool airflowStopped = sample.Airflow <= MinimumAirflow;
            if (airflowStopped && sample.ForkReturned)
            {
                caughtRecoveryElapsed = 0f;
                recoveryTimerResumed = false;
                ChangeState(GameState.WaitingForPickup);
            }
        }

        private void TryCompletePickup()
        {
            if (!TryGetConnectedSample(out GameInputSample sample))
            {
                return;
            }

            if (!sample.ForkReturned)
            {
                ChangeState(GameState.Playing);
            }
        }

        private bool TryGetConnectedSample(out GameInputSample sample)
        {
            sample = inputRouter != null ? inputRouter.CurrentSample : GameInputSample.Disconnected;
            if (sample.IsConnected)
            {
                hasWarnedAboutMissingInput = false;
                return true;
            }

            if (!hasWarnedAboutMissingInput)
            {
                Debug.LogWarning("Cannot resolve the teacher check until the input source is connected.", this);
                hasWarnedAboutMissingInput = true;
            }

            return false;
        }

        private float MinimumAirflow => inputRouter != null ? inputRouter.MinimumAirflow : 0.1f;

        private void ChangeState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            GameState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}

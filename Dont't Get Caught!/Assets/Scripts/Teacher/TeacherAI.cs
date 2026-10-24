using System;
using System.Collections;
using DontGetCaught.Core;
using DontGetCaught.Input;
using UnityEngine;
namespace DontGetCaught.Teacher
{
    /// <summary>Runs the teacher's scheduled warning and face-to-player check cycle.</summary>
    public sealed class TeacherAI : MonoBehaviour
    {
        [Serializable]
        private struct DifficultyStage
        {
            [Min(0f)] public float startElapsed;
            [Min(0f)] public float endElapsed;
            [Min(0.1f)] public float minInterval;
            [Min(0.1f)] public float maxInterval;
            [Min(0.1f)] public float warningSeconds;
        }
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private DifficultyStage[] stages =
        {
            new DifficultyStage { startElapsed=0f, endElapsed=60f, minInterval=15f, maxInterval=22f, warningSeconds=2.5f },
            new DifficultyStage { startElapsed=60f, endElapsed=120f, minInterval=11f, maxInterval=17f, warningSeconds=1.8f },
            new DifficultyStage { startElapsed=120f, endElapsed=180f, minInterval=7f, maxInterval=12f, warningSeconds=1.2f }
        };
        private Coroutine routine;
        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<InputRouter>();
            routine = StartCoroutine(TeacherLoop());
        }
        private void OnDisable()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
        }
        private IEnumerator TeacherLoop()
        {
            if (gameManager == null || inputRouter == null)
            {
                Debug.LogError("TeacherAI requires GameManager and InputRouter references.", this);
                yield break;
            }
            while (gameManager.CurrentState != GameState.Won && gameManager.CurrentState != GameState.Lost)
            {
                yield return new WaitUntil(() => gameManager.CurrentState == GameState.Playing
                    || gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost);
                if (gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost) yield break;
                DifficultyStage stage = StageAtCurrentTime();
                float min = Mathf.Max(0.1f, stage.minInterval);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, Mathf.Max(min, stage.maxInterval)));
                if (gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost) yield break;
                if (gameManager.CurrentState != GameState.Playing) continue;
                gameManager.BeginTeacherWarning();
                yield return new WaitForSeconds(Mathf.Max(0.1f, stage.warningSeconds));
                if (gameManager.CurrentState != GameState.TeacherWarning) continue;
                gameManager.BeginTeacherChecking();
                float elapsed = 0f;
                while (gameManager.CurrentState == GameState.TeacherChecking)
                {
                    GameInputSample sample = inputRouter.CurrentSample;
                    if (sample.IsConnected && (sample.Airflow > inputRouter.MinimumAirflow
                        || !sample.ForkReturned))
                    {
                        string reason = sample.Airflow > inputRouter.MinimumAirflow
                            ? "Still inhaling!" : "Return the fork to its station!";
                        gameManager.ReportCaught(reason);
                        break;
                    }
                    elapsed += Time.deltaTime;
                    if (elapsed >= gameManager.Config.teacherFacingDurationSeconds) gameManager.ResolveTeacherCheck();
                    if (gameManager.CurrentState == GameState.TeacherChecking) yield return null;
                }
                if (gameManager.CurrentState == GameState.CaughtRecovery)
                    yield return new WaitUntil(() => gameManager.CurrentState == GameState.WaitingForPickup
                        || gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost);
                if (gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost) yield break;
                yield return new WaitUntil(() => gameManager.CurrentState == GameState.Playing
                    || gameManager.CurrentState == GameState.Won || gameManager.CurrentState == GameState.Lost);
            }
        }
        private DifficultyStage StageAtCurrentTime()
        {
            float elapsed = gameManager.Config.roundDurationSeconds - gameManager.RemainingSeconds;
            if (stages == null || stages.Length == 0)
                return new DifficultyStage { startElapsed=0f, endElapsed=float.MaxValue, minInterval=15f, maxInterval=22f, warningSeconds=2.5f };
            for (int i=0; i<stages.Length; i++)
                if (elapsed >= stages[i].startElapsed && elapsed < stages[i].endElapsed) return stages[i];
            return elapsed < stages[0].startElapsed ? stages[0] : stages[stages.Length-1];
        }
    }
}

using DontGetCaught.Scoring;
using UnityEngine;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Reveals a little more of the score bar each time a noodle is completed.
    /// Place this component on the score bar's root object and assign its mask
    /// and full-width bar graphic in the Inspector.
    /// </summary>
    public sealed class ScoreProgressBar : MonoBehaviour
    {
        [Header("Score Source")]
        [SerializeField] private ScoreManager scoreManager;

        [Header("Bar Layout")]
        [SerializeField] private RectTransform fillMask;
        [SerializeField] private RectTransform fullBar;

        [Header("Progress")]
        [SerializeField, Range(0f, 1f)] private float ratioPerCompletedNoodle = 0.05f;

        private int lastScore;
        private int completedNoodles;

        private void OnEnable()
        {
            if (scoreManager == null)
            {
                scoreManager = FindFirstObjectByType<ScoreManager>();
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += HandleScoreChanged;
                lastScore = scoreManager.TotalScore;
            }

            completedNoodles = 0;
            ApplyProgress();
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= HandleScoreChanged;
            }
        }

        private void OnValidate()
        {
            ratioPerCompletedNoodle = Mathf.Clamp01(ratioPerCompletedNoodle);
            ApplyProgress();
        }

        private void HandleScoreChanged(int newScore)
        {
            if (newScore <= 0 || newScore < lastScore)
            {
                completedNoodles = 0;
            }
            else if (newScore > lastScore)
            {
                // ScoreManager raises one event per completed noodle. The score
                // amount itself may be much larger than one, so count events.
                completedNoodles++;
            }

            lastScore = newScore;
            ApplyProgress();
        }

        private void ApplyProgress()
        {
            if (fillMask == null || fullBar == null)
            {
                return;
            }

            float fullWidth = fullBar.rect.width;
            if (fullWidth <= 0f)
            {
                return;
            }

            float visibleWidth = fullWidth * Mathf.Clamp01(completedNoodles * ratioPerCompletedNoodle);
            Vector2 anchorMin = fillMask.anchorMin;
            Vector2 anchorMax = fillMask.anchorMax;
            anchorMin.x = 0f;
            anchorMax.x = 0f;
            fillMask.anchorMin = anchorMin;
            fillMask.anchorMax = anchorMax;

            Vector2 pivot = fillMask.pivot;
            pivot.x = 0f;
            fillMask.pivot = pivot;

            Vector2 size = fillMask.sizeDelta;
            size.x = visibleWidth;
            fillMask.sizeDelta = size;

            // Keep the clipping window attached to the left edge as it grows.
            Vector2 position = fillMask.anchoredPosition;
            position.x = 0f;
            fillMask.anchoredPosition = position;
        }
    }
}

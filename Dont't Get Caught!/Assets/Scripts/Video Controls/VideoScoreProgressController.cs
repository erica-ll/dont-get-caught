using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace DontGetCaught.VideoControls
{
    /// <summary>
    /// Drives both player scores and progress bars from elapsed video playback
    /// time. All changes are local to the Video scene.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class VideoScoreProgressController : MonoBehaviour
    {
        [Header("Video Source")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Score Growth")]
        [SerializeField] private TMP_Text player1ScoreText;
        [SerializeField] private TMP_Text player2ScoreText;
        [SerializeField, Min(0)] private int player1BaseScore;
        [SerializeField, Min(0)] private int player2BaseScore;
        [SerializeField, Min(0f)] private float scorePointsPerSecond = 5f;
        [SerializeField, Min(1)] private int pointsToFillEntireBar = 1000;

        [Header("Score Timer")]
        [SerializeField, Min(0f)] private float scoreStartDelaySeconds;
        [SerializeField, Min(0f)] private float scoreGrowthDurationSeconds = 60f;

        [Header("Player 1 Progress Bar (left to right)")]
        [SerializeField] private RectTransform player1FillMask;
        [SerializeField] private RectTransform player1FullBar;

        [Header("Player 2 Progress Bar (right to left)")]
        [SerializeField] private RectTransform player2FillMask;
        [SerializeField] private RectTransform player2FullBar;
        [SerializeField] private bool player2GrowsFromRight = true;

        private float player1BarWidth;
        private float player2BarWidth;
        private float elapsedPlaybackSeconds;
        private bool videoHasStarted;
        private bool barsConfigured;

        private void Awake()
        {
            if (videoPlayer == null) videoPlayer = FindFirstObjectByType<VideoPlayer>();
            ConfigureBars();
        }

        private void OnEnable()
        {
            if (videoPlayer == null) videoPlayer = FindFirstObjectByType<VideoPlayer>();

            if (videoPlayer != null)
            {
                videoPlayer.started += HandleVideoStarted;
                videoHasStarted = videoPlayer.isPlaying;
            }

            RenderAtCurrentTime();
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.started -= HandleVideoStarted;
            }
        }

        private void OnValidate()
        {
            scorePointsPerSecond = Mathf.Max(0f, scorePointsPerSecond);
            pointsToFillEntireBar = Mathf.Max(1, pointsToFillEntireBar);
            scoreStartDelaySeconds = Mathf.Max(0f, scoreStartDelaySeconds);
            scoreGrowthDurationSeconds = Mathf.Max(0f, scoreGrowthDurationSeconds);
        }

        private void LateUpdate()
        {
            if (!barsConfigured)
            {
                ConfigureBars();
            }

            if (videoPlayer == null || !videoPlayer.isPlaying)
            {
                return;
            }

            if (!videoHasStarted)
            {
                videoHasStarted = true;
                elapsedPlaybackSeconds = 0f;
            }

            elapsedPlaybackSeconds += Time.unscaledDeltaTime * Mathf.Max(0f, videoPlayer.playbackSpeed);
            RenderAtCurrentTime();
        }

        private void HandleVideoStarted(VideoPlayer source)
        {
            elapsedPlaybackSeconds = 0f;
            videoHasStarted = true;
            RenderAtCurrentTime();
        }

        private void ConfigureBars()
        {
            if (barsConfigured)
            {
                return;
            }

            ConfigureBar(player1FillMask, player1FullBar, false, out player1BarWidth);
            ConfigureBar(player2FillMask, player2FullBar, player2GrowsFromRight, out player2BarWidth);
            barsConfigured = player1FillMask != null && player1FullBar != null
                             && player2FillMask != null && player2FullBar != null
                             && player1BarWidth > 0f && player2BarWidth > 0f;
        }

        private static void ConfigureBar(
            RectTransform mask,
            RectTransform fullBar,
            bool growFromRight,
            out float fullWidth)
        {
            fullWidth = 0f;
            if (mask == null || fullBar == null)
            {
                return;
            }

            fullWidth = fullBar.rect.width;
            if (fullWidth <= 0f)
            {
                return;
            }

            // Preserve the bar art's current visible start/end location before
            // switching its anchors to the direction in which it should grow.
            float oldMaskPosition = mask.anchoredPosition.x;
            float oldBarPosition = fullBar.anchoredPosition.x;
            float leftEdge = oldMaskPosition + oldBarPosition;
            float rightEdge = leftEdge + fullWidth;

            SetHorizontalAnchor(mask, 0f, growFromRight ? 1f : 0f);
            SetHorizontalPosition(mask, growFromRight ? rightEdge : leftEdge);

            SetHorizontalAnchor(fullBar, growFromRight ? 1f : 0f, growFromRight ? 1f : 0f);
            SetHorizontalPosition(fullBar, 0f);
            Vector2 barSize = fullBar.sizeDelta;
            barSize.x = fullWidth;
            fullBar.sizeDelta = barSize;

            Vector2 maskSize = mask.sizeDelta;
            maskSize.x = 0f;
            mask.sizeDelta = maskSize;
        }

        private static void SetHorizontalAnchor(RectTransform rect, float anchorX, float pivotX)
        {
            Vector2 min = rect.anchorMin;
            Vector2 max = rect.anchorMax;
            min.x = anchorX;
            max.x = anchorX;
            rect.anchorMin = min;
            rect.anchorMax = max;

            Vector2 pivot = rect.pivot;
            pivot.x = pivotX;
            rect.pivot = pivot;
        }

        private static void SetHorizontalPosition(RectTransform rect, float x)
        {
            Vector2 position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
        }

        private void RenderAtCurrentTime()
        {
            float growthElapsed = Mathf.Clamp(
                elapsedPlaybackSeconds - scoreStartDelaySeconds,
                0f,
                scoreGrowthDurationSeconds);
            float elapsedPoints = growthElapsed * scorePointsPerSecond;
            float player1Score = player1BaseScore + elapsedPoints;
            float player2Score = player2BaseScore + elapsedPoints;
            if (player1ScoreText != null)
            {
                player1ScoreText.text = Mathf.FloorToInt(player1Score).ToString();
            }

            if (player2ScoreText != null)
            {
                player2ScoreText.text = Mathf.FloorToInt(player2Score).ToString();
            }

            float scoreThreshold = Mathf.Max(1, pointsToFillEntireBar);
            SetMaskProgress(player1FillMask, player1BarWidth, player1Score / scoreThreshold);
            SetMaskProgress(player2FillMask, player2BarWidth, player2Score / scoreThreshold);
        }

        private static void SetMaskProgress(RectTransform mask, float fullWidth, float progress)
        {
            if (mask == null || fullWidth <= 0f)
            {
                return;
            }

            Vector2 size = mask.sizeDelta;
            size.x = fullWidth * Mathf.Clamp01(progress);
            mask.sizeDelta = size;
        }
    }
}

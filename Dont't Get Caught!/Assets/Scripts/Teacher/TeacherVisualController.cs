using DontGetCaught.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DontGetCaught.Teacher
{
    /// <summary>Plays the art team's teacher sprite sequences from round-state changes.</summary>
    public sealed class TeacherVisualController : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private Image teacherImage;
        [SerializeField] private TeacherAnimationSet animationSet;
        [SerializeField] private GameManager gameManager;

        [Header("Playback")]
        [SerializeField, Min(1f)] private float walkFramesPerSecond = 12f;
        [SerializeField, Min(1f)] private float writingFramesPerSecond = 4f;
        [SerializeField, Min(1f)] private float alertFramesPerSecond = 24f;
        [SerializeField, Min(1f)] private float angryFramesPerSecond = 8f;
        [SerializeField, Min(1f)] private float turningFramesPerSecond = 12f;

        [Header("Visual size comparison")]
        [SerializeField, Min(0.1f)] private float walkingSizeScale = 1f;
        [SerializeField, Min(0.1f)] private float writingSizeScale = 1f;
        [SerializeField, Min(0.1f)] private float alertSizeScale = 1f;
        [SerializeField, Min(0.1f)] private float angrySizeScale = 1f;
        [SerializeField, Min(0.1f)] private float turningSizeScale = 1f;

        [Header("Walking circulation start/stop (0-based frame indices)")]
        [SerializeField, Min(0)] private int walkingStartFrame = 23;
        [SerializeField, Min(0)] private int walkingStopFrame = 22;

        [Header("Source artwork canvas")]
        [SerializeField] private Vector2 sourceCanvasSize = new Vector2(1920f, 1080f);

        private Sprite[] currentFrames;
        private float currentFramesPerSecond;
        private float frameTimer;
        private float calmTimer;
        private int frameIndex;
        private float currentSizeScale = 1f;
        private float lastAppliedSizeScale = -1f;
        private bool loopCurrentSequence;
        private bool transitioningToAngryLoop;
        private bool returningToWriting;
        private CalmPhase calmPhase;

        private enum CalmPhase
        {
            None,
            Walking,
            Writing
        }

        private void Awake()
        {
            if (teacherImage == null) teacherImage = GetComponent<Image>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        }

        private void OnEnable()
        {
            if (gameManager == null) return;
            gameManager.StateChanged += HandleStateChanged;
            SetState(gameManager.CurrentState, gameManager.CurrentState);
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (currentFrames == null || currentFrames.Length == 0) return;

            if (!Mathf.Approximately(currentSizeScale, lastAppliedSizeScale)) ApplyFrame(frameIndex);

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, currentFramesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                if (frameIndex + 1 < currentFrames.Length)
                {
                    ApplyFrame(++frameIndex);
                    continue;
                }

                if (loopCurrentSequence)
                {
                    ApplyFrame(frameIndex = 0);
                    continue;
                }

                frameTimer = 0f;
                if (transitioningToAngryLoop)
                {
                    transitioningToAngryLoop = false;
                    Play(animationSet != null ? animationSet.AngryCirculation : null, angryFramesPerSecond, true, angrySizeScale);
                }
                else if (returningToWriting)
                {
                    returningToWriting = false;
                    StartCalmWriting();
                }
                else if (calmPhase == CalmPhase.Walking)
                {
                    StartCalmWriting();
                }
                break;
            }

            if (calmPhase == CalmPhase.Writing)
            {
                calmTimer += Time.deltaTime;
                if (calmTimer >= CalmCycleDurationSeconds)
                {
                    StartCalmWalk();
                }
            }
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            SetState(previous, next);
        }

        private void SetState(GameState previous, GameState next)
        {
            if (next == GameState.Playing && previous == GameState.WaitingForPickup)
            {
                // Picking up the fork resumes gameplay without restarting the teacher's
                // current calm animation or resetting its walk/write cycle timer.
                return;
            }

            calmPhase = CalmPhase.None;
            transitioningToAngryLoop = false;
            returningToWriting = false;

            switch (next)
            {
                case GameState.TeacherWarning:
                    Play(animationSet != null ? animationSet.Alert : null, alertFramesPerSecond, false, alertSizeScale);
                    break;
                case GameState.TeacherChecking:
                    Play(animationSet != null ? animationSet.TurningAround : null, turningFramesPerSecond, false, turningSizeScale);
                    break;
                case GameState.CaughtRecovery:
                    StartAngryTurningAround();
                    break;
                case GameState.Lost when previous == GameState.TeacherChecking:
                    StartAngryTurningAround();
                    break;
                case GameState.WaitingForPickup when previous == GameState.TeacherChecking
                    || previous == GameState.CaughtRecovery:
                    returningToWriting = true;
                    Play(animationSet != null ? animationSet.TurningAroundToWrite : null,
                        turningFramesPerSecond, false, turningSizeScale);
                    break;
                case GameState.WaitingForPickup:
                    StartCalmWriting();
                    break;
                case GameState.Ready:
                    calmTimer = 0f;
                    StartCalmWalk();
                    break;
                case GameState.Playing:
                    calmTimer = 0f;
                    StartCalmWalk();
                    break;
                case GameState.Won:
                    calmPhase = CalmPhase.None;
                    break;
            }
        }

        private float CalmCycleDurationSeconds
        {
            get
            {
                Sprite[] walk = animationSet != null ? animationSet.WalkCirculation : null;
                if (walk == null || walk.Length == 0) return 9.6f;
                int start = Mathf.Clamp(walkingStartFrame, 0, walk.Length - 1);
                int stop = Mathf.Clamp(walkingStopFrame, 0, walk.Length - 1);
                int frameCount = stop >= start ? stop - start + 1 : walk.Length - start + stop + 1;
                return frameCount / Mathf.Max(1f, walkFramesPerSecond);
            }
        }

        private void StartCalmWalk()
        {
            calmPhase = CalmPhase.Walking;
            calmTimer = 0f;
            Sprite[] frames = animationSet != null ? animationSet.WalkCirculation : null;
            PlayWrappedRange(frames, walkingStartFrame, walkingStopFrame, walkFramesPerSecond, walkingSizeScale);
        }

        private void StartCalmWriting()
        {
            calmPhase = CalmPhase.Writing;
            calmTimer = 0f;
            Play(animationSet != null ? animationSet.WritingCirculation : null, writingFramesPerSecond, true, writingSizeScale);
        }

        private void StartAngryTurningAround()
        {
            Sprite[] frames = animationSet != null ? animationSet.AngryTurningAround : null;
            if (frames != null && frames.Length > 0)
            {
                transitioningToAngryLoop = true;
                Play(frames, turningFramesPerSecond, false, angrySizeScale);
            }
            else
            {
                Play(animationSet != null ? animationSet.AngryCirculation : null, angryFramesPerSecond, true, angrySizeScale);
            }
        }

        private void PlayWrappedRange(Sprite[] frames, int first, int last, float framesPerSecond, float sizeScale)
        {
            if (frames == null || frames.Length == 0) return;
            first = Mathf.Clamp(first, 0, frames.Length - 1);
            last = Mathf.Clamp(last, 0, frames.Length - 1);
            int frameCount = last >= first ? last - first + 1 : frames.Length - first + last + 1;
            Sprite[] segment = new Sprite[frameCount];
            for (int i = 0; i < segment.Length; i++) segment[i] = frames[(first + i) % frames.Length];
            Play(segment, framesPerSecond, false, sizeScale);
        }

        private void Play(Sprite[] frames, float framesPerSecond, bool loop, float sizeScale)
        {
            if (frames == null || frames.Length == 0 || teacherImage == null) return;
            currentFrames = frames;
            currentFramesPerSecond = framesPerSecond;
            loopCurrentSequence = loop;
            currentSizeScale = sizeScale;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame(0);
        }

        private void ApplyFrame(int index)
        {
            if (teacherImage == null || currentFrames == null || index >= currentFrames.Length) return;
            Sprite sprite = currentFrames[index];
            if (sprite == null) return;

            teacherImage.sprite = sprite;
            RectTransform imageRect = teacherImage.rectTransform;
            RectTransform artboardRect = imageRect.parent as RectTransform;
            if (artboardRect == null) return;

            float scaleX = artboardRect.rect.width / Mathf.Max(1f, sourceCanvasSize.x);
            float scaleY = artboardRect.rect.height / Mathf.Max(1f, sourceCanvasSize.y);
            Rect spriteRect = sprite.rect;
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(spriteRect.width * scaleX, spriteRect.height * scaleY) * currentSizeScale;
            imageRect.anchoredPosition = new Vector2(
                (spriteRect.center.x - sourceCanvasSize.x * 0.5f) * scaleX,
                (spriteRect.center.y - sourceCanvasSize.y * 0.5f) * scaleY);
            lastAppliedSizeScale = currentSizeScale;
        }
    }
}

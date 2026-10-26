using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Plays a VideoPlayer into a RawImage so the video can sit behind the
    /// scene's Screen Space Overlay HUD without changing gameplay systems.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class VideoSceneBackground : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;

        private RenderTexture videoTexture;
        private Vector2 lastViewSize;

        private void Awake()
        {
            if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        }

        private void OnEnable()
        {
            if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
            if (videoImage == null) videoImage = FindFirstObjectByType<RawImage>();

            if (videoPlayer == null || videoImage == null)
            {
                Debug.LogError("VideoSceneBackground needs a VideoPlayer and a RawImage reference.", this);
                return;
            }

            if (videoPlayer.clip == null)
            {
                Debug.LogError("Assign a Video Clip to the VideoPlayer in the Inspector.", this);
                return;
            }

            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.Prepare();
        }

        private void LateUpdate()
        {
            UpdateAspectFill();
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= HandlePrepared;
                videoPlayer.errorReceived -= HandleVideoError;
                videoPlayer.Stop();
                videoPlayer.targetTexture = null;
            }

            if (videoImage != null && videoImage.texture == videoTexture)
            {
                videoImage.texture = null;
            }

            ReleaseVideoTexture();
        }

        private void HandlePrepared(VideoPlayer source)
        {
            ReleaseVideoTexture();

            int width = Mathf.Max(1, (int)source.width);
            int height = Mathf.Max(1, (int)source.height);
            videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "Video Scene Runtime Texture"
            };
            videoTexture.Create();

            source.targetTexture = videoTexture;
            videoImage.texture = videoTexture;
            videoImage.raycastTarget = false;
            lastViewSize = Vector2.zero;
            UpdateAspectFill();
            source.Play();
        }

        private void HandleVideoError(VideoPlayer source, string message)
        {
            Debug.LogError($"Video playback failed: {message}", this);
        }

        private void UpdateAspectFill()
        {
            if (videoImage == null || videoTexture == null)
            {
                return;
            }

            RectTransform imageRect = videoImage.rectTransform;
            Vector2 viewSize = imageRect.rect.size;
            if (viewSize.x <= 0f || viewSize.y <= 0f || viewSize == lastViewSize)
            {
                return;
            }

            lastViewSize = viewSize;
            float viewAspect = viewSize.x / viewSize.y;
            float videoAspect = (float)videoTexture.width / videoTexture.height;
            Rect uv = new Rect(0f, 0f, 1f, 1f);

            if (viewAspect > videoAspect)
            {
                uv.height = videoAspect / viewAspect;
                uv.y = (1f - uv.height) * 0.5f;
            }
            else
            {
                uv.width = viewAspect / videoAspect;
                uv.x = (1f - uv.width) * 0.5f;
            }

            videoImage.uvRect = uv;
        }

        private void ReleaseVideoTexture()
        {
            if (videoTexture == null)
            {
                return;
            }

            videoTexture.Release();
            Destroy(videoTexture);
            videoTexture = null;
        }
    }
}

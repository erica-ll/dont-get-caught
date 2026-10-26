using UnityEngine;

namespace DontGetCaught.UI
{
    /// <summary>Ordered full-to-empty sprite frames for the ramen cup UI.</summary>
    [CreateAssetMenu(menuName = "Don't Get Caught/UI/Ramen Cup Frame Sequence")]
    public sealed class RamenCupFrameSequence : ScriptableObject
    {
        [SerializeField] private Sprite[] frames;

        public int FrameCount => frames != null ? frames.Length : 0;

        public Sprite GetFrameAtProgress(float progress)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int frameIndex = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Clamp01(progress) * (frames.Length - 1)),
                0,
                frames.Length - 1);
            return frames[frameIndex];
        }
    }
}

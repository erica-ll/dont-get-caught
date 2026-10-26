using DontGetCaught.Noodle;
using UnityEngine;
using UnityEngine.UI;

namespace DontGetCaught.UI
{
    /// <summary>
    /// Maps noodle progress to the ramen cup sequence: full at zero progress,
    /// empty at completed progress, and full again after the noodle resets.
    /// </summary>
    public sealed class NoodleEatingRamenSequenceView : MonoBehaviour
    {
        [Header("Noodle Progress")]
        [SerializeField] private NoodleEatingController noodleController;

        [Header("Ramen Cup Display")]
        [SerializeField] private Image ramenCupImage;
        [SerializeField] private RamenCupFrameSequence frameSequence;

        private void OnEnable()
        {
            if (noodleController == null)
            {
                noodleController = FindFirstObjectByType<NoodleEatingController>();
            }

            if (noodleController == null)
            {
                Debug.LogError("NoodleEatingRamenSequenceView needs a NoodleEatingController reference.", this);
                return;
            }

            noodleController.ProgressChanged += HandleProgressChanged;
            HandleProgressChanged(noodleController.Progress);
        }

        private void OnDisable()
        {
            if (noodleController != null)
            {
                noodleController.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleProgressChanged(float progress)
        {
            if (ramenCupImage == null || frameSequence == null || frameSequence.FrameCount == 0)
            {
                return;
            }

            Sprite frame = frameSequence.GetFrameAtProgress(progress);
            if (frame != null)
            {
                ramenCupImage.sprite = frame;
            }
        }
    }
}

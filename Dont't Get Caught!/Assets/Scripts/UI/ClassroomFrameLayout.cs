using UnityEngine;
using UnityEngine.UI;

namespace DontGetCaught.UI
{
    /// <summary>Fits the classroom artwork inside an adjustable, four-sided frame.</summary>
    [ExecuteAlways]
    public sealed class ClassroomFrameLayout : MonoBehaviour
    {
        [Header("Classroom content")]
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private RectTransform artworkRoot;
        [SerializeField] private Vector2 artworkCanvasSize = new Vector2(1920f, 1080f);

        [Header("Frame widths")]
        [SerializeField, Min(0f)] private float leftWidth = 48f;
        [SerializeField, Min(0f)] private float rightWidth = 48f;
        [SerializeField, Min(0f)] private float topWidth = 48f;
        [SerializeField, Min(0f)] private float bottomWidth = 48f;

        [Header("Frame appearance")]
        [SerializeField] private Color frameColor = Color.black;
        [SerializeField] private Image frameBackground;
        [SerializeField] private Image topBorder;
        [SerializeField] private Image rightBorder;
        [SerializeField] private Image bottomBorder;
        [SerializeField] private Image leftBorder;

        private bool applyingLayout;

        private void OnEnable() => ApplyLayout();

        private void OnValidate() => ApplyLayout();

        private void OnRectTransformDimensionsChange() => ApplyLayout();

        private void ApplyLayout()
        {
            if (applyingLayout) return;
            RectTransform frameRect = transform as RectTransform;
            if (frameRect == null || contentRect == null || artworkRoot == null) return;

            applyingLayout = true;
            leftWidth = Mathf.Max(0f, leftWidth);
            rightWidth = Mathf.Max(0f, rightWidth);
            topWidth = Mathf.Max(0f, topWidth);
            bottomWidth = Mathf.Max(0f, bottomWidth);

            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(leftWidth, bottomWidth);
            contentRect.offsetMax = new Vector2(-rightWidth, -topWidth);

            SetHorizontalBorder(topBorder != null ? topBorder.rectTransform : null, topWidth, true);
            SetHorizontalBorder(bottomBorder != null ? bottomBorder.rectTransform : null, bottomWidth, false);
            SetVerticalBorder(leftBorder != null ? leftBorder.rectTransform : null, leftWidth, true);
            SetVerticalBorder(rightBorder != null ? rightBorder.rectTransform : null, rightWidth, false);
            SetBorderColor(topBorder);
            SetBorderColor(rightBorder);
            SetBorderColor(bottomBorder);
            SetBorderColor(leftBorder);
            SetBorderColor(frameBackground);

            artworkRoot.anchorMin = artworkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            artworkRoot.pivot = new Vector2(0.5f, 0.5f);
            artworkRoot.anchoredPosition = Vector2.zero;
            artworkRoot.sizeDelta = artworkCanvasSize;

            float availableWidth = Mathf.Max(0f, contentRect.rect.width);
            float availableHeight = Mathf.Max(0f, contentRect.rect.height);
            float fitScale = Mathf.Min(
                availableWidth / Mathf.Max(1f, artworkCanvasSize.x),
                availableHeight / Mathf.Max(1f, artworkCanvasSize.y));
            artworkRoot.localScale = new Vector3(fitScale, fitScale, 1f);
            applyingLayout = false;
        }

        private void SetHorizontalBorder(RectTransform border, float width, bool atTop)
        {
            if (border == null) return;
            border.anchorMin = atTop ? new Vector2(0f, 1f) : Vector2.zero;
            border.anchorMax = atTop ? Vector2.one : new Vector2(1f, 0f);
            border.pivot = atTop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            border.anchoredPosition = Vector2.zero;
            border.sizeDelta = new Vector2(0f, width);
        }

        private void SetVerticalBorder(RectTransform border, float width, bool atLeft)
        {
            if (border == null) return;
            border.anchorMin = atLeft ? Vector2.zero : new Vector2(1f, 0f);
            border.anchorMax = atLeft ? new Vector2(0f, 1f) : Vector2.one;
            border.pivot = atLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            border.offsetMin = new Vector2(atLeft ? 0f : -width, bottomWidth);
            border.offsetMax = new Vector2(atLeft ? width : 0f, -topWidth);
        }

        private void SetBorderColor(Image border)
        {
            if (border != null) border.color = frameColor;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class ScreenHeatEffect : MonoBehaviour
{
    [SerializeField] private Image heatOverlayImage; // Kéo UI Image màu đỏ vào đây
    [SerializeField] private float fadeSpeed = 2f;

    private float targetAlpha = 0f;

    void Start()
    {
        if (heatOverlayImage != null)
        {
            Color c = heatOverlayImage.color;
            c.a = 0f;
            heatOverlayImage.color = c;
        }
    }

    void Update()
    {
        if (heatOverlayImage == null) return;

        // Lerp màu để chuyển đổi mượt mà giữa nóng và nguội
        Color currentColor = heatOverlayImage.color;
        float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);

        heatOverlayImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
    }

    public void SetHeat(bool isHot)
    {
        // Nếu nóng thì Alpha = 0.6 (khá đậm), nguội thì Alpha = 0
        targetAlpha = isHot ? 0.6f : 0f;
    }
}
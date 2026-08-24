using TMPro;
using UnityEngine;

public sealed class FpsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;

    private float smoothedDeltaTime;

    private void Update()
    {
        smoothedDeltaTime = Mathf.Lerp(
            smoothedDeltaTime,
            Time.unscaledDeltaTime,
            0.1f
        );

        if (fpsText == null || smoothedDeltaTime <= Mathf.Epsilon)
        {
            return;
        }

        float fps = 1f / smoothedDeltaTime;
        fpsText.text = $"FPS: {fps:0}";
    }
}

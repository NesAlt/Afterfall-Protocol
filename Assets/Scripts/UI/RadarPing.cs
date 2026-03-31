using UnityEngine;

public class RadarPing : MonoBehaviour
{
    public float maxRadius = 80f;
    public float speed = 100f;
    private RectTransform rect;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        rect.sizeDelta = new Vector2(10f, 10f); // start visible
        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        float newSize = rect.sizeDelta.x + speed * Time.deltaTime;
        rect.sizeDelta = new Vector2(newSize, newSize);

        float progress = newSize / maxRadius;

        if (progress < 0.5f)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            float fadeProgress = (progress - 0.5f) * 2f;
            canvasGroup.alpha = 1f - fadeProgress;
        }

        if (newSize >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class CreditsFade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeSpeed = 1.5f;

    void Start()
    {
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
        }
    }
}
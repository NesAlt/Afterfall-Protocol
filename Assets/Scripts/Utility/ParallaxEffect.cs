using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    private float length, startPos;
    public GameObject cam;

    [Range(0f, 1f)]
    public float parallaxEffect;

    [Header("Clamp Settings")]
    public bool infiniteHorizontal = true;   // Turn OFF for sky/distant layers
    public bool stretchToFillCamera = true;   // Auto-scales sprite to cover viewport

    private Camera mainCam;
    private SpriteRenderer sr;

    void Start()
    {
        mainCam = cam.GetComponent<Camera>();
        sr = GetComponent<SpriteRenderer>();

        startPos = transform.position.x;
        length = sr.bounds.size.x;

        if (stretchToFillCamera)
            AutoScaleToFillCamera();
    }

    void AutoScaleToFillCamera()
    {
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth  = camHeight * mainCam.aspect;

        float spriteHeight = sr.sprite.bounds.size.y * transform.localScale.y;
        float spriteWidth  = sr.sprite.bounds.size.x * transform.localScale.x;

        if (spriteHeight < camHeight)
        {
            float scaleY = camHeight / sr.sprite.bounds.size.y;
            transform.localScale = new Vector3(scaleY, scaleY, 1f); // uniform scale
        }

        float requiredWidth = camWidth * 3f;
        length = sr.bounds.size.x;
        if (length < requiredWidth)
        {
            float scaleX = requiredWidth / sr.sprite.bounds.size.x;
            transform.localScale = new Vector3(
                Mathf.Max(transform.localScale.x, scaleX),
                transform.localScale.y,
                1f
            );
            length = sr.bounds.size.x;
        }
    }

    void LateUpdate()
    {
        float camX = cam.transform.position.x;

        float dist = camX * parallaxEffect;
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        if (infiniteHorizontal)
        {
            float temp = camX * (1f - parallaxEffect);
            if      (temp > startPos + length) startPos += length;
            else if (temp < startPos - length) startPos -= length;
        }
    }
}
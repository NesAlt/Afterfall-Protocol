using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    private float length, startPos;
    public GameObject cam;
    public float parallaxEffect; // The multiplier (e.g., 0.5f)

    void Start()
    {
        startPos = transform.position.x;
        // This calculates the width of your sprite so it can loop
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float dist = (cam.transform.position.x * parallaxEffect);
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        // Infinite Looping Logic (Optional but recommended)
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        if (temp > startPos + length) startPos += length;
        else if (temp < startPos - length) startPos -= length;
    }
}
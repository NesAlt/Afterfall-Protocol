using UnityEngine;

public class BobbingEffect : MonoBehaviour
{
    public float amplitude = 0.15f; 
    public float frequency = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = startPosition + new Vector3(0, offset, 0);

        transform.Rotate(0, 0, 30f * Time.deltaTime);
    }
}

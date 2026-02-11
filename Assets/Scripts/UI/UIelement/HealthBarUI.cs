using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public float smoothSpeed = 5f;
    private float targetFill;
    public Image fillImage;
    private Health playerHealth;

    void Start()
    {
        playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();

        playerHealth.OnHealthChanged += UpdateBarInstant;
        UpdateBarInstant();
    }


    void Update()
    {
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
    }

    public void UpdateBarInstant()
    {
        Debug.Log("Updating bar");
        targetFill = (float)playerHealth.currentHealth / playerHealth.maximumHealth;
    }
}

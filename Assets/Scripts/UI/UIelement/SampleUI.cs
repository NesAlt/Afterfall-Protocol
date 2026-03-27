using TMPro;
using UnityEngine;

public class SampleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sampleText;

    public void UpdateUI(int current, int required)
    {
        sampleText.text = $"Samples: {current} / {required}";
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMouseSensitivity : MonoBehaviour
{
    public Slider sensitivitySlider;
    public TMP_Text valueText;

    private const string PREF_KEY = "MouseSensitivity";
    private const float DEFAULT_VALUE = 2f;

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(PREF_KEY, DEFAULT_VALUE);

        sensitivitySlider.minValue = 0.5f;
        sensitivitySlider.maxValue = 5f;
        sensitivitySlider.value = saved;

        UpdateText(saved);

        sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_KEY, value);
        PlayerPrefs.Save();

        UpdateText(value);
    }

    private void UpdateText(float value)
    {
        if (valueText != null)
            valueText.text = "Sensitivity: " + value.ToString("0.00");
    }
}

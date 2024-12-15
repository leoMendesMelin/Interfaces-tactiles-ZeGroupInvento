using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaiterUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText; // Changé pour TMP

    private WaiterData waiterData;
    private readonly Color connectedColor = new Color(0.18f, 0.8f, 0.44f); // Vert #2ECC71
    private readonly Color disconnectedColor = new Color(0.7f, 0.7f, 0.7f); // Gris

    public void Initialize(WaiterData data)
    {
        waiterData = data;
        UpdateUI();
    }

    public string GetWaiterId()
    {
        return waiterData?.id;
    }

    private void UpdateUI()
    {
        if (waiterData != null && nameText != null)
        {
            nameText.text = waiterData.name;
            background.color = waiterData.status == "connected" ? connectedColor : disconnectedColor;
        }
    }

    public void UpdateStatus(string newStatus)
    {
        if (waiterData != null)
        {
            waiterData.status = newStatus;
            UpdateUI();
        }
    }

    // Méthode pour débugger et vérifier les références
    private void OnValidate()
    {
        if (nameText == null)
        {
            nameText = GetComponentInChildren<TextMeshProUGUI>();
            if (nameText == null)
            {
                Debug.LogWarning("TextMeshProUGUI component not found in WaiterUI or its children");
            }
        }

        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                Debug.LogWarning("Image component not found in WaiterUI");
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaiterUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    private WaiterData waiterData;
    private readonly Color connectedColor = new Color(0.18f, 0.8f, 0.44f); // Vert #2ECC71
    private readonly Color disconnectedColor = new Color(0.7f, 0.7f, 0.7f); // Gris
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // S'assurer que nous avons un CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Configurer le CanvasGroup pour être toujours interactif
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    public void Initialize(WaiterData data)
    {
        waiterData = data;
        UpdateUI();

        // S'assurer que le waiter reste interactif après l'initialisation
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public WaiterData GetWaiterData()
    {
        return waiterData;
    }

    private void UpdateUI()
    {
        if (waiterData != null && nameText != null && background != null)
        {
            nameText.text = waiterData.name;
            background.color = waiterData.status == "connected" ? connectedColor : disconnectedColor;

            // Même quand déconnecté, garder le waiter interactif
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
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

    private void OnEnable()
    {
        // S'assurer que l'image de fond peut recevoir les raycasts
        if (background != null)
        {
            background.raycastTarget = true;
        }

        // Optionnel : désactiver le raycast sur le texte pour éviter la redondance
        if (nameText != null)
        {
            nameText.raycastTarget = false;
        }
    }

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
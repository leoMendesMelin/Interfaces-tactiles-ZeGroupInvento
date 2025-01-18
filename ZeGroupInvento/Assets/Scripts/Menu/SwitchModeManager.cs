using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class SwitchModeManager : MonoBehaviour
{
    [Header("Menu Groups")]
    public GameObject elementPanelSouth;
    public GameObject elementPanelNorth;
    public GameObject zoneMenuPanelSouth;
    public GameObject zoneMenuPanelNorth;

    [Header("South Buttons")]
    public Button elementButtonSouth;
    public Button zoneButtonSouth;

    [Header("North Buttons")]
    public Button elementButtonNorth;
    public Button zoneButtonNorth;

    private bool isElementSouthSelected = true;
    private bool isElementNorthSelected = true;
    private bool isZoneSouthSelected = false;
    private bool isZoneNorthSelected = false;

    private void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        // Attendre que les données soient chargées
        yield return new WaitForSeconds(2f);

        // Configuration des boutons
        elementButtonSouth.onClick.AddListener(() => OnElementButtonClick(true));
        elementButtonNorth.onClick.AddListener(() => OnElementButtonClick(false));
        zoneButtonSouth.onClick.AddListener(() => OnZoneButtonClick(true));
        zoneButtonNorth.onClick.AddListener(() => OnZoneButtonClick(false));

        // État initial
        isElementSouthSelected = true;
        isElementNorthSelected = true;
        isZoneSouthSelected = false;
        isZoneNorthSelected = false;

        UpdateMenuVisibility();
        UpdateButtonVisuals();
    }

    private void UpdateScripts(bool showElementPanels, bool showZonePanels)
    {
        Debug.Log($"UpdateScripts called with elements:{showElementPanels}, zones:{showZonePanels}");

        var tables = FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("TABLE_RECT_2") || go.name.Contains("TABLE_RECT_4"))
            .ToList();
        var zones = FindObjectsOfType<ZoneControllerPrefab>();

        Debug.Log($"Found {tables.Count} tables and {zones.Length} zones");

        foreach (var table in tables)
        {
            if (table.TryGetComponent<ElementDragHandler>(out var dragHandler))
                dragHandler.enabled = showElementPanels;

            if (table.TryGetComponent<TableSliceHandler>(out var sliceHandler))
                sliceHandler.enabled = showElementPanels;

            if (showElementPanels)
                table.transform.SetAsLastSibling();
            else
                table.transform.SetAsFirstSibling();
        }

        foreach (var controller in zones)
        {
            controller.enabled = showZonePanels;
            controller.transform.SetSiblingIndex(showZonePanels ? transform.GetSiblingIndex() + 1 : 0);
            Debug.Log($"Zone {controller.name} controller set to {showZonePanels}");
        }
    }

    private void OnElementButtonClick(bool isSouth)
    {
        if (isSouth)
        {
            isElementSouthSelected = !isElementSouthSelected;
            if (isElementSouthSelected) isZoneSouthSelected = false;
        }
        else
        {
            isElementNorthSelected = !isElementNorthSelected;
            if (isElementNorthSelected) isZoneNorthSelected = false;
        }
        UpdateMenuVisibility();
        UpdateButtonVisuals();
    }

    private void OnZoneButtonClick(bool isSouth)
    {
        if (isSouth)
        {
            isZoneSouthSelected = !isZoneSouthSelected;
            if (isZoneSouthSelected) isElementSouthSelected = false;
        }
        else
        {
            isZoneNorthSelected = !isZoneNorthSelected;
            if (isZoneNorthSelected) isElementNorthSelected = false;
        }
        UpdateMenuVisibility();
        UpdateButtonVisuals();
    }

    private void UpdateMenuVisibility()
    {
        bool showElementPanels = isElementSouthSelected && isElementNorthSelected;
        bool showZonePanels = isZoneSouthSelected && isZoneNorthSelected;

        elementPanelSouth.SetActive(showElementPanels);
        elementPanelNorth.SetActive(showElementPanels);
        zoneMenuPanelSouth.SetActive(showZonePanels);
        zoneMenuPanelNorth.SetActive(showZonePanels);

        UpdateScripts(showElementPanels, showZonePanels);
    }

    private void UpdateButtonVisuals()
    {
        UpdateButtonColor(elementButtonSouth, isElementSouthSelected);
        UpdateButtonColor(elementButtonNorth, isElementNorthSelected);
        UpdateButtonColor(zoneButtonSouth, isZoneSouthSelected);
        UpdateButtonColor(zoneButtonNorth, isZoneNorthSelected);
    }

    private void UpdateButtonColor(Button button, bool isSelected)
    {
        if (button.TryGetComponent<Image>(out var image))
        {
            image.color = isSelected ? new Color(0.2f, 0.6f, 1f) : Color.white;
        }
    }
}
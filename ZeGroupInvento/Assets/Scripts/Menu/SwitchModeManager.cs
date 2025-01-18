using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
        elementButtonSouth.onClick.AddListener(() => OnElementButtonClick(true));
        elementButtonNorth.onClick.AddListener(() => OnElementButtonClick(false));
        zoneButtonSouth.onClick.AddListener(() => OnZoneButtonClick(true));
        zoneButtonNorth.onClick.AddListener(() => OnZoneButtonClick(false));

        DisableZoneScripts();
        UpdateMenuVisibility();
        UpdateButtonVisuals();
    }

    private void DisableZoneScripts()
    {
        var zoneControllers = FindObjectsOfType<ZoneControllerPrefab>();
        foreach (var controller in zoneControllers)
        {
            controller.enabled = false;
        }
    }

    private void UpdateScripts(bool showElementPanels, bool showZonePanels)
    {
        var tables = FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("TABLE_RECT_2") || go.name.Contains("TABLE_RECT_4"));
        var zones = FindObjectsOfType<ZoneControllerPrefab>();

        // Gestion des tables
        foreach (var table in tables)
        {
            if (table.TryGetComponent<ElementDragHandler>(out var dragHandler))
                dragHandler.enabled = showElementPanels;

            if (table.TryGetComponent<TableSliceHandler>(out var sliceHandler))
                sliceHandler.enabled = showElementPanels;

            // UI Hierarchy sorting
            if (showElementPanels)
            {
                table.transform.SetAsLastSibling();
            }
            else
            {
                table.transform.SetAsFirstSibling();
            }
        }

        // Gestion des zones
        foreach (var controller in zones)
        {
            controller.enabled = showZonePanels;

            if (showZonePanels)
            {
                controller.transform.SetAsLastSibling();
            }
            else
            {
                controller.transform.SetAsFirstSibling();
            }
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
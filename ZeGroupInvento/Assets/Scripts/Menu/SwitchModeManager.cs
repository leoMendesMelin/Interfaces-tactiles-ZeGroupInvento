using UnityEngine;
using UnityEngine.UI;

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
        // Setup des listeners pour les boutons
        elementButtonSouth.onClick.AddListener(() => OnElementButtonClick(true));
        elementButtonNorth.onClick.AddListener(() => OnElementButtonClick(false));
        zoneButtonSouth.onClick.AddListener(() => OnZoneButtonClick(true));
        zoneButtonNorth.onClick.AddListener(() => OnZoneButtonClick(false));

        // État initial
        UpdateMenuVisibility();
        UpdateButtonVisuals();
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
        // Afficher les panels Element seulement si les deux boutons Element sont sélectionnés
        bool showElementPanels = isElementSouthSelected && isElementNorthSelected;
        elementPanelSouth.SetActive(showElementPanels);
        elementPanelNorth.SetActive(showElementPanels);

        // Afficher les panels Zone seulement si les deux boutons Zone sont sélectionnés
        bool showZonePanels = isZoneSouthSelected && isZoneNorthSelected;
        zoneMenuPanelSouth.SetActive(showZonePanels);
        zoneMenuPanelNorth.SetActive(showZonePanels);
    }

    private void UpdateButtonVisuals()
    {
        // Mettre à jour l'apparence des boutons
        UpdateButtonColor(elementButtonSouth, isElementSouthSelected);
        UpdateButtonColor(elementButtonNorth, isElementNorthSelected);
        UpdateButtonColor(zoneButtonSouth, isZoneSouthSelected);
        UpdateButtonColor(zoneButtonNorth, isZoneNorthSelected);
    }

    private void UpdateButtonColor(Button button, bool isSelected)
    {
        var image = button.GetComponent<Image>();
        image.color = isSelected ? new Color(0.2f, 0.6f, 1f) : Color.white;
    }
}
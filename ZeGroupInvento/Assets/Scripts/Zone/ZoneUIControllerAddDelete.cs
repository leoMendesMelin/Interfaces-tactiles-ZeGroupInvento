using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ZoneUIControllerAddDelete : MonoBehaviour
{
    [SerializeField] private Button addButton;
    [SerializeField] private GameObject menuZonePrefab; // Le prefab pour le menu
    [SerializeField] private GameObject backgroundZonePrefab; // Le prefab pour la grille
    [SerializeField] private RectTransform zoneContainer; // Le parent des zones dans le menu
    [SerializeField] private RectTransform backgroundPanel; // R�f�rence au panel de fond
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private GridManager gridManager;
    private string currentSelectedZoneId;

    private void Start()
    {
        // Enlever la v�rification du deleteButton car il sera cr�� dynamiquement
        if (addButton == null) Debug.LogError("AddButton non assign�");
        if (menuZonePrefab == null) Debug.LogError("MenuZonePrefab non assign�");
        if (backgroundZonePrefab == null) Debug.LogError("BackgroundZonePrefab non assign�");
        if (zoneContainer == null) Debug.LogError("ZoneContainer non assign�");
        if (backgroundPanel == null) Debug.LogError("BackgroundPanel non assign�");
        if (zoneManager == null) Debug.LogError("ZoneManager non assign�");
        if (gridManager == null) Debug.LogError("GridManager non assign�");

        // Setup du listener pour le addButton uniquement
        addButton.onClick.AddListener(OnAddZoneClicked);
    }


    private void OnAddZoneClicked()
    {
        Debug.Log("OnAddZoneClicked called");

        if (menuZonePrefab == null)
        {
            Debug.LogError("menuZonePrefab is null");
            return;
        }

        if (zoneContainer == null)
        {
            Debug.LogError("zoneContainer is null");
            return;
        }

        GameObject menuZoneUI = Instantiate(menuZonePrefab, zoneContainer);

        // La placer au d�but de la liste (� gauche)
        menuZoneUI.transform.SetAsFirstSibling();

        // Trouver AddZone et le mettre � la fin
        Transform addZoneTransform = zoneContainer.Find("AddZone");
        if (addZoneTransform != null)
        {
            addZoneTransform.SetAsLastSibling();
        }
        if (menuZoneUI == null)
        {
            Debug.LogError("Failed to instantiate menuZoneUI");
            return;
        }

        ZoneUIElementMenu zoneElement = menuZoneUI.GetComponent<ZoneUIElementMenu>();
        if (zoneElement == null)
        {
            Debug.LogError("ZoneUIElementMenu component not found on menuZoneUI");
            return;
        }

        if (backgroundZonePrefab == null)
        {
            Debug.LogError("backgroundZonePrefab is null");
            return;
        }

        if (backgroundPanel == null)
        {
            Debug.LogError("backgroundPanel is null");
            return;
        }

        // Cr�er la zone interactive dans le background
        GameObject backgroundZoneUI = Instantiate(backgroundZonePrefab, backgroundPanel);
        if (backgroundZoneUI == null)
        {
            Debug.LogError("Failed to instantiate backgroundZoneUI");
            return;
        }

        ZoneControllerPrefab zoneController = backgroundZoneUI.GetComponent<ZoneControllerPrefab>();
        if (zoneController == null)
        {
            Debug.LogError("ZoneControllerPrefab component not found on backgroundZoneUI");
            return;
        }

        if (zoneManager == null)
        {
            Debug.LogError("zoneManager is null");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("gridManager is null");
            return;
        }

        // Cr�er les donn�es de la zone
        ZoneData newZone = new ZoneData
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Zone",
            color = "#FF0000",
            position = new Vector2Int(5, 5),
            width = 3,
            height = 3
        };

        // Initialiser les deux composants
        zoneElement.Initialize(newZone.id, this);
        zoneController.Initialize(newZone, zoneManager, gridManager);

        // Mettre � jour le ZoneManager
        zoneManager.RegisterZone(newZone.id, backgroundZoneUI);
    }


    private void OnDeleteZoneClicked()
    {
        if (!string.IsNullOrEmpty(currentSelectedZoneId))
        {
            zoneManager.DeleteZone(currentSelectedZoneId);
        }
    }


    public void OnZoneSelected(string zoneId, Button deleteButton)
    {
        currentSelectedZoneId = zoneId;
        // Le deleteButton est maintenant pass� en param�tre depuis le ZoneUIElementMenu
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(OnDeleteZoneClicked);
        deleteButton.interactable = true;
    }

    public void OnZoneDeleted(string zoneId)
    {
        if (currentSelectedZoneId == zoneId)
        {
            currentSelectedZoneId = null;
        }
    }
}
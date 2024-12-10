using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ZoneUIControllerAddDelete : MonoBehaviour // C'EST LE CONTROLLER QUI EST SUR ListZone et qui permet de gérer la création d'une zone
{
    [SerializeField] private Button addButton;
    [SerializeField] private GameObject menuZonePrefab;
    [SerializeField] private GameObject backgroundZonePrefab;
    [SerializeField] private RectTransform zoneContainer;
    [SerializeField] private RectTransform backgroundPanel;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject addZonePrefab; // Référence au prefab AddZone

    private string currentSelectedZoneId;
    private Dictionary<string, GameObject> menuZoneInstances = new Dictionary<string, GameObject>();

    private void Start()
    {
        ValidateComponents();
        addButton.onClick.AddListener(OnAddZoneClicked);
    }

    private void ValidateComponents()
    {
        if (addButton == null) Debug.LogError("AddButton non assigné");
        if (menuZonePrefab == null) Debug.LogError("MenuZonePrefab non assigné");
        if (backgroundZonePrefab == null) Debug.LogError("BackgroundZonePrefab non assigné");
        if (zoneContainer == null) Debug.LogError("ZoneContainer non assigné");
        if (backgroundPanel == null) Debug.LogError("BackgroundPanel non assigné");
        if (zoneManager == null) Debug.LogError("ZoneManager non assigné");
        if (gridManager == null) Debug.LogError("GridManager non assigné");
    }

    private void OnAddZoneClicked()
    {
        // Créer la nouvelle zone dans le menu
        GameObject menuZoneUI = Instantiate(menuZonePrefab, zoneContainer);

        // Générer un nouvel ID pour la zone
        string newZoneId = System.Guid.NewGuid().ToString();

        // Créer les données de la zone
        ZoneData newZone = new ZoneData
        {
            id = newZoneId,
            name = "New Zone",
            color = "#FF0000",
            position = new Vector2Int(5, 5),
            width = 3,
            height = 3
        };

        // Initialiser la zone du menu
        ZoneUIElementMenu zoneElement = menuZoneUI.GetComponent<ZoneUIElementMenu>();
        zoneElement.Initialize(newZoneId, this);

        // Créer et initialiser la zone de fond
        GameObject backgroundZoneUI = Instantiate(backgroundZonePrefab, backgroundPanel);
        ZoneControllerPrefab zoneController = backgroundZoneUI.GetComponent<ZoneControllerPrefab>();
        zoneController.Initialize(newZone, zoneManager, gridManager);

        // Enregistrer les instances
        menuZoneInstances[newZoneId] = menuZoneUI;
        zoneManager.RegisterZone(newZoneId, backgroundZoneUI);

        // Réorganiser le layout
        ReorganizeLayout();
    }

    private void ReorganizeLayout()
    {
        // Trouver l'objet AddZone
        Transform addZoneTransform = zoneContainer.Find("AddZone");
        if (addZoneTransform != null)
        {
            // Déplacer AddZone à la fin
            addZoneTransform.SetAsLastSibling();
        }

        // Forcer la mise à jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(zoneContainer);
    }

    public void OnDeleteZoneClicked(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId)) return;

        // Supprimer la zone du background
        zoneManager.DeleteZone(zoneId);

        // Supprimer la zone du menu si elle existe
        if (menuZoneInstances.TryGetValue(zoneId, out GameObject menuZone))
        {
            Destroy(menuZone);
            menuZoneInstances.Remove(zoneId);
        }

        // Réinitialiser la sélection si nécessaire
        if (currentSelectedZoneId == zoneId)
        {
            currentSelectedZoneId = null;
        }

        // Réorganiser le layout
        ReorganizeLayout();
    }

    public void OnZoneSelected(string zoneId, Button deleteButton)
    {
        currentSelectedZoneId = zoneId;
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDeleteZoneClicked(zoneId));
        deleteButton.interactable = true;
    }

    public void OnZoneDeleted(string zoneId)
    {
        // Cette méthode peut être appelée par ZoneUIElementMenu
        OnDeleteZoneClicked(zoneId);
    }
}

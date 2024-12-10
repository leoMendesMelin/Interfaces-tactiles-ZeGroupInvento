using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ZoneUIControllerAddDelete : MonoBehaviour // C'EST LE CONTROLLER QUI EST SUR ListZone et qui permet de g�rer la cr�ation d'une zone
{
    [SerializeField] private Button addButton;
    [SerializeField] private GameObject menuZonePrefab;
    [SerializeField] private GameObject backgroundZonePrefab;
    [SerializeField] private RectTransform zoneContainer;
    [SerializeField] private RectTransform backgroundPanel;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject addZonePrefab; // R�f�rence au prefab AddZone

    private string currentSelectedZoneId;
    private Dictionary<string, GameObject> menuZoneInstances = new Dictionary<string, GameObject>();

    private void Start()
    {
        ValidateComponents();
        addButton.onClick.AddListener(OnAddZoneClicked);
    }

    private void ValidateComponents()
    {
        if (addButton == null) Debug.LogError("AddButton non assign�");
        if (menuZonePrefab == null) Debug.LogError("MenuZonePrefab non assign�");
        if (backgroundZonePrefab == null) Debug.LogError("BackgroundZonePrefab non assign�");
        if (zoneContainer == null) Debug.LogError("ZoneContainer non assign�");
        if (backgroundPanel == null) Debug.LogError("BackgroundPanel non assign�");
        if (zoneManager == null) Debug.LogError("ZoneManager non assign�");
        if (gridManager == null) Debug.LogError("GridManager non assign�");
    }

    private void OnAddZoneClicked()
    {
        // Cr�er la nouvelle zone dans le menu
        GameObject menuZoneUI = Instantiate(menuZonePrefab, zoneContainer);

        // G�n�rer un nouvel ID pour la zone
        string newZoneId = System.Guid.NewGuid().ToString();

        // Cr�er les donn�es de la zone
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

        // Cr�er et initialiser la zone de fond
        GameObject backgroundZoneUI = Instantiate(backgroundZonePrefab, backgroundPanel);
        ZoneControllerPrefab zoneController = backgroundZoneUI.GetComponent<ZoneControllerPrefab>();
        zoneController.Initialize(newZone, zoneManager, gridManager);

        // Enregistrer les instances
        menuZoneInstances[newZoneId] = menuZoneUI;
        zoneManager.RegisterZone(newZoneId, backgroundZoneUI);

        // R�organiser le layout
        ReorganizeLayout();
    }

    private void ReorganizeLayout()
    {
        // Trouver l'objet AddZone
        Transform addZoneTransform = zoneContainer.Find("AddZone");
        if (addZoneTransform != null)
        {
            // D�placer AddZone � la fin
            addZoneTransform.SetAsLastSibling();
        }

        // Forcer la mise � jour du layout
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

        // R�initialiser la s�lection si n�cessaire
        if (currentSelectedZoneId == zoneId)
        {
            currentSelectedZoneId = null;
        }

        // R�organiser le layout
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
        // Cette m�thode peut �tre appel�e par ZoneUIElementMenu
        OnDeleteZoneClicked(zoneId);
    }
}

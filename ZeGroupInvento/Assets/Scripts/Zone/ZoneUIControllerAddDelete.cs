using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
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
    public GameObject MenuZonePrefab => menuZonePrefab;

    private string currentSelectedZoneId;
    public Dictionary<string, List<GameObject>> menuZoneInstances = new Dictionary<string, List<GameObject>>();

    private ZoneWebSocketManager webSocketManager;


    void Start()
    {
        ValidateComponents();
        if (ValidateComponents())
        {
            addButton.onClick.AddListener(OnAddZoneClicked);
            zoneManager.Initialize(gridManager); // Initialiser le ZoneManager avec GridManager
        }
        webSocketManager = FindObjectOfType<ZoneWebSocketManager>();

        if (webSocketManager == null)
        {
            Debug.LogError("ZoneWebSocketManager not found in scene");
        }
    }

    private bool ValidateComponents()
    {
        bool isValid = true;
        if (addButton == null) { Debug.LogError("AddButton non assigné"); isValid = false; }
        if (menuZonePrefab == null) { Debug.LogError("MenuZonePrefab non assigné"); isValid = false; }
        if (backgroundZonePrefab == null) { Debug.LogError("BackgroundZonePrefab non assigné"); isValid = false; }
        if (zoneContainer == null) { Debug.LogError("ZoneContainer non assigné"); isValid = false; }
        if (backgroundPanel == null) { Debug.LogError("BackgroundPanel non assigné"); isValid = false; }
        if (zoneManager == null) { Debug.LogError("ZoneManager non assigné"); isValid = false; }
        if (gridManager == null) { Debug.LogError("GridManager non assigné"); isValid = false; }
        return isValid;
    }

    // Dans ZoneUIControllerAddDelete.cs
    private void OnAddZoneClicked()
    {
        // Générer un nouvel ID pour la zone
        string newZoneId = System.Guid.NewGuid().ToString();

        // Obtenir une couleur unique
        string zoneColor = ColorManager.Instance.GetUniqueColor();

        // Calculer la position centrale de la grille
        int gridSize = RoomManager.Instance.GetCurrentRoom().gridSize;
        Vector2Int centerPosition = new Vector2Int(gridSize / 2, gridSize / 2);

        // Créer les données de la zone au centre
        ZoneData newZone = new ZoneData
        {
            id = newZoneId,
            name = "New Zone",
            color = zoneColor,
            position = centerPosition, // Utiliser la position centrale
            width = 6,
            height = 6
        };

        // Le reste du code reste identique...
        Transform[] allListZones = GameObject.FindObjectsOfType<Transform>()
                                           .Where(t => t.name == "ListZones")
                                           .ToArray();

        List<GameObject> instances = new List<GameObject>();

        foreach (Transform listZone in allListZones)
        {
            GameObject menuZoneUI = Instantiate(menuZonePrefab, listZone);
            ZoneUIElementMenu zoneElement = menuZoneUI.GetComponent<ZoneUIElementMenu>();
            zoneElement.Initialize(newZoneId, this);

            // Appliquer la couleur au menu UI
            Image menuZoneImage = menuZoneUI.GetComponent<Image>();
            if (menuZoneImage != null)
            {
                Color newColor;
                if (ColorUtility.TryParseHtmlString(zoneColor, out newColor))
                {
                    menuZoneImage.color = newColor;
                }
            }

            instances.Add(menuZoneUI);
            ReorganizeLayout(listZone);
        }

        // Laisser le ZoneManager gérer la création de la zone
        zoneManager.InstantiateZoneUI(newZone);

        // Enregistrer les instances de menu
        menuZoneInstances[newZoneId] = instances;

        if (webSocketManager != null)
        {
            webSocketManager.EmitZoneCreated(newZone);
        }
    }




    private void ReorganizeLayout(Transform listZoneTransform)
    {
        // Trouver l'objet AddZone dans ce ListZone spécifique
        Transform addZoneTransform = listZoneTransform.Find("AddZone");
        if (addZoneTransform != null)
        {
            // Déplacer AddZone à la fin
            addZoneTransform.SetAsLastSibling();
        }

        // Forcer la mise à jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(listZoneTransform as RectTransform);
    }

    public void OnDeleteZoneClicked(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId)) return;

        // Récupérer les données de la zone avant de la supprimer
        ZoneData zoneToDelete = null;
        if (menuZoneInstances.TryGetValue(zoneId, out List<GameObject> menuZones) &&
            menuZones.Count > 0 &&
            menuZones[0] != null)
        {
            Image image = menuZones[0].GetComponent<Image>();
            if (image != null)
            {
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(image.color);
                zoneToDelete = new ZoneData
                {
                    id = zoneId,
                    color = colorHex
                };
            }
        }

        ReleaseZoneColor(zoneId);
        DeleteMenuInstances(zoneId);
        DeleteBackgroundZone(zoneId);
        ResetSelectedZoneIfNeeded(zoneId);
        ReorganizeAllLayouts();

        // Envoyer l'événement de suppression via WebSocket
        if (webSocketManager != null && zoneToDelete != null)
        {
            webSocketManager.EmitZoneDeleted(zoneToDelete);
        }
    }

    private void ReleaseZoneColor(string zoneId)
    {
        if (menuZoneInstances.TryGetValue(zoneId, out List<GameObject> menuZones) &&
            menuZones.Count > 0 &&
            menuZones[0] != null)
        {
            Image image = menuZones[0].GetComponent<Image>();
            if (image != null)
            {
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(image.color);
                ColorManager.Instance.ReleaseColor(colorHex);
            }
        }
    }

    private void DeleteMenuInstances(string zoneId)
    {
        if (menuZoneInstances.TryGetValue(zoneId, out List<GameObject> menuZones))
        {
            foreach (GameObject menuZone in menuZones)
            {
                if (menuZone != null)
                {
                    Destroy(menuZone);
                }
            }
            menuZoneInstances.Remove(zoneId);
        }
    }

    private void DeleteBackgroundZone(string zoneId)
    {
        zoneManager.DeleteZone(zoneId);
    }

    private void ResetSelectedZoneIfNeeded(string zoneId)
    {
        if (currentSelectedZoneId == zoneId)
        {
            currentSelectedZoneId = null;
        }
    }

    private void ReorganizeAllLayouts()
    {
        var allListZones = GameObject.FindObjectsOfType<Transform>()
                                    .Where(t => t.name == "ListZones");

        foreach (Transform listZone in allListZones)
        {
            ReorganizeLayout(listZone);
        }
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

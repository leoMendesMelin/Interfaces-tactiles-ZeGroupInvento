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

    private string currentSelectedZoneId;
    private Dictionary<string, List<GameObject>> menuZoneInstances = new Dictionary<string, List<GameObject>>();


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

    // Dans ZoneUIControllerAddDelete.cs
    private void OnAddZoneClicked()
    {
        // Générer un nouvel ID pour la zone
        string newZoneId = System.Guid.NewGuid().ToString();

        // Obtenir une couleur unique
        string zoneColor = ColorManager.Instance.GetUniqueColor();

        // Créer les données de la zone
        ZoneData newZone = new ZoneData
        {
            id = newZoneId,
            name = "New Zone",
            color = zoneColor,
            position = new Vector2Int(5, 5),
            width = 3,
            height = 3
        };

        // Trouver tous les ListZones dans la scène
        Transform[] allListZones = GameObject.FindObjectsOfType<Transform>()
                                           .Where(t => t.name == "ListZones")
                                           .ToArray();

        List<GameObject> instances = new List<GameObject>();

        foreach (Transform listZone in allListZones)
        {
            // Créer la nouvelle zone dans chaque menu
            GameObject menuZoneUI = Instantiate(menuZonePrefab, listZone);

            // Initialiser la zone du menu
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

            // Réorganiser le layout pour ce ListZone
            ReorganizeLayout(listZone);
        }

        // Créer et initialiser la zone de fond
        GameObject backgroundZoneUI = Instantiate(backgroundZonePrefab, backgroundPanel);
        ZoneControllerPrefab zoneController = backgroundZoneUI.GetComponent<ZoneControllerPrefab>();
        zoneController.Initialize(newZone, zoneManager, gridManager);

        // Appliquer la même couleur au background zone
        Image backgroundZoneImage = backgroundZoneUI.GetComponent<Image>();
        if (backgroundZoneImage != null)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString(zoneColor, out newColor))
            {
                backgroundZoneImage.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
            }
        }

        // Enregistrer toutes les instances
        menuZoneInstances[newZoneId] = instances;
        zoneManager.RegisterZone(newZoneId, backgroundZoneUI);
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

        ReleaseZoneColor(zoneId);
        DeleteMenuInstances(zoneId);
        DeleteBackgroundZone(zoneId);
        ResetSelectedZoneIfNeeded(zoneId);
        ReorganizeAllLayouts();
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

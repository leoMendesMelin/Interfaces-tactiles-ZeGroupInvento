using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    [SerializeField] private GameObject zonePrefab;
    private Dictionary<string, GameObject> zoneInstances = new Dictionary<string, GameObject>();


    private GridManager gridManager;

    public void Initialize(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public string CreateZone() // Modifié pour retourner string
    {
        // Créer une nouvelle zone au centre
        ZoneData newZone = new ZoneData
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Zone",
            color = "#FF0000", // Rouge par défaut
            position = new Vector2Int(5, 5), // Position centrale approximative
            width = 3, // Taille par défaut
            height = 3
        };
        InstantiateZoneUI(newZone);
        return newZone.id; // Retourne l'ID
    }

    public void RegisterZone(string zoneId, GameObject zoneInstance)
    {
        zoneInstances[zoneId] = zoneInstance;
    }

    public void DeleteZone(string zoneId)
    {
        if (zoneInstances.TryGetValue(zoneId, out GameObject zoneObj))
        {
            Destroy(zoneObj);
            zoneInstances.Remove(zoneId);
        }
    }

    private void InstantiateZoneUI(ZoneData zoneData)
    {
        GameObject zoneObj = Instantiate(zonePrefab, transform);
        RectTransform rectTransform = zoneObj.GetComponent<RectTransform>();

        // Positionner la zone
        rectTransform.anchoredPosition = gridManager.GetWorldPosition(zoneData.position);

        // Configurer la taille
        float cellWidth = gridManager.GetCellSize().x;
        float cellHeight = gridManager.GetCellSize().y;
        rectTransform.sizeDelta = new Vector2(cellWidth * zoneData.width, cellHeight * zoneData.height);

        // Initialiser le composant de manipulation
        ZoneControllerPrefab controller = zoneObj.GetComponent<ZoneControllerPrefab>();
        controller.Initialize(zoneData, this, gridManager);

        zoneInstances[zoneData.id] = zoneObj;
    }
}
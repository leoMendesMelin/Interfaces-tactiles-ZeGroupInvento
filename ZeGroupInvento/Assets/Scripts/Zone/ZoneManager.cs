using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
public class ZoneManager : MonoBehaviour
{
    [SerializeField] private GameObject zonePrefab;  // Vérifier dans l'inspecteur Unity que c'est bien assigné
    private Dictionary<string, GameObject> zoneInstances = new Dictionary<string, GameObject>();
    private GridManager gridManager;
    private bool isInitialized = false;
    [SerializeField] private RectTransform backgroundPanel; // Ajouter cette référence



    public void Initialize(GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError("Cannot initialize ZoneManager: GridManager is null!");
            return;
        }

        this.gridManager = gridManager;
        isInitialized = true;
        Debug.Log("ZoneManager successfully initialized with GridManager");
    }

    public void InstantiateZoneUI(ZoneData zoneData)
    {
        if (zonePrefab == null || backgroundPanel == null)
        {
            Debug.LogError("ZonePrefab or BackgroundPanel is not assigned in ZoneManager");
            return;
        }

        // Créer l'instance du prefab dans le BackgroundPanel
        GameObject zoneObj = Instantiate(zonePrefab, backgroundPanel);

        // Le reste du code reste identique...
        RectTransform rectTransform = zoneObj.GetComponent<RectTransform>();
        Vector2 cellSize = gridManager.GetCellSize();
        rectTransform.sizeDelta = new Vector2(
            cellSize.x * zoneData.width,
            cellSize.y * zoneData.height
        );
        rectTransform.anchoredPosition = gridManager.GetWorldPosition(zoneData.position);

        ZoneControllerPrefab controller = zoneObj.GetComponent<ZoneControllerPrefab>();
        controller.Initialize(zoneData, this, gridManager);

        zoneInstances[zoneData.id] = zoneObj;
    }

    public void RegisterZone(string zoneId, GameObject zoneInstance)
    {
        if (string.IsNullOrEmpty(zoneId) || zoneInstance == null)
        {
            Debug.LogError($"Invalid parameters in RegisterZone: zoneId={zoneId}, instance={(zoneInstance != null)}");
            return;
        }
        zoneInstances[zoneId] = zoneInstance;
    }

    public void DeleteZone(string zoneId)
    {
        if (zoneInstances.TryGetValue(zoneId, out GameObject zoneObj))
        {
            if (zoneObj != null)
            {
                Destroy(zoneObj);
            }
            zoneInstances.Remove(zoneId);
        }
    }

    public void UpdateZone(ZoneData zoneData)
    {
        if (!isInitialized || zoneData == null)
        {
            Debug.LogError("Cannot update zone: ZoneManager not initialized or zoneData is null");
            return;
        }

        if (zoneInstances.TryGetValue(zoneData.id, out GameObject zoneInstance))
        {
            // Mettre à jour la position et la taille
            RectTransform rectTransform = zoneInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Mettre à jour la position
                Vector2 worldPosition = gridManager.GetWorldPosition(zoneData.position);
                rectTransform.anchoredPosition = worldPosition;

                // Mettre à jour la taille
                Vector2 cellSize = gridManager.GetCellSize();
                rectTransform.sizeDelta = new Vector2(
                    cellSize.x * zoneData.width,
                    cellSize.y * zoneData.height
                );
            }

            // Mettre à jour la couleur si nécessaire
            ZoneControllerPrefab controller = zoneInstance.GetComponent<ZoneControllerPrefab>();
            if (controller != null)
            {
                controller.UpdateColor(zoneData.color);
            }
        }
        else
        {
            // Si la zone n'existe pas, la créer
            InstantiateZoneUI(zoneData);
        }
    }
}
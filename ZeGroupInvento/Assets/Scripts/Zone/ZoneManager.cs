using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
public class ZoneManager : MonoBehaviour
{
    [SerializeField] private GameObject zonePrefab;  // V�rifier dans l'inspecteur Unity que c'est bien assign�
    private Dictionary<string, GameObject> zoneInstances = new Dictionary<string, GameObject>();
    private GridManager gridManager;
    private bool isInitialized = false;
    [SerializeField] private RectTransform backgroundPanel; // Ajouter cette r�f�rence

    private const string SERVER_URL = "http://localhost:9090";



    public IEnumerator FetchRoom(System.Action<Room> onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/room"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                jsonResponse = jsonResponse.TrimStart('[').TrimEnd(']');
                Room room = JsonUtility.FromJson<Room>(jsonResponse);

                // R�cup�rer les zones pour cette room
                if (room != null)
                {
                    StartCoroutine(FetchZones(room, onSuccess));
                }
                else
                {
                    onSuccess?.Invoke(null);
                }
            }
        }
    }

    private IEnumerator FetchZones(Room room, System.Action<Room> onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/room/{room.id}/zones"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                // D�s�rialiser le tableau de zones
                ZoneData[] zones = JsonUtility.FromJson<ZoneDataArray>($"{{\"zones\":{jsonResponse}}}").zones;
                room.zones = zones;
                onSuccess?.Invoke(room);

                // Cr�er les UI des zones
                ZoneManager zoneManager = FindObjectOfType<ZoneManager>();
                if (zoneManager != null && zones != null)
                {
                    foreach (var zone in zones)
                    {
                        zoneManager.UpdateZone(zone);
                    }
                }
            }
            else
            {
                Debug.LogError($"Error fetching zones: {request.error}");
                onSuccess?.Invoke(room);
            }
        }
    }

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

        // Cr�er l'instance du prefab dans le BackgroundPanel
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
            // Mettre � jour la position et la taille
            RectTransform rectTransform = zoneInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Mettre � jour la position
                Vector2 worldPosition = gridManager.GetWorldPosition(zoneData.position);
                rectTransform.anchoredPosition = worldPosition;

                // Mettre � jour la taille
                Vector2 cellSize = gridManager.GetCellSize();
                rectTransform.sizeDelta = new Vector2(
                    cellSize.x * zoneData.width,
                    cellSize.y * zoneData.height
                );
            }

            // Mettre � jour la couleur si n�cessaire
            ZoneControllerPrefab controller = zoneInstance.GetComponent<ZoneControllerPrefab>();
            if (controller != null)
            {
                controller.UpdateColor(zoneData.color);
            }
        }
        else
        {
            // Si la zone n'existe pas, la cr�er
            InstantiateZoneUI(zoneData);
        }
    }
}
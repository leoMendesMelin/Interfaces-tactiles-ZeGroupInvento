using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
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
            ZoneData[] zones = JsonUtility.FromJson<ZoneDataArray>($"{{\"zones\":{jsonResponse}}}").zones;
            room.zones = zones;
            onSuccess?.Invoke(room);

            // Cr�er les UI des zones et les �l�ments de menu
            if (zones != null)
            {
                // Trouver le ZoneUIControllerAddDelete de mani�re plus robuste
                ZoneUIControllerAddDelete menuController = FindObjectOfType<ZoneUIControllerAddDelete>();
                
                if (menuController == null)
                {
                    Debug.LogError("ZoneUIControllerAddDelete not found in scene");
                    yield break;
                }

                foreach (var zone in zones)
                {
                    UpdateZone(zone);
                    
                    yield return new WaitForEndOfFrame();
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
        if (!isInitialized || zoneData == null) return;

        // Partie ZonePrefab...
        if (zoneInstances.TryGetValue(zoneData.id, out GameObject zoneInstance))
        {
            // Update existant...
        }
        else
        {
            InstantiateZoneUI(zoneData);
        }

        var zoneController = FindObjectOfType<ZoneUIControllerAddDelete>();
        if (zoneController == null) return;

        if (!zoneController.menuZoneInstances.ContainsKey(zoneData.id))
        {
            Transform[] allListZones = GameObject.FindObjectsOfType<Transform>(true)
                                               .Where(t => t.name == "ListZones")
                                               .ToArray();

            List<GameObject> instances = new List<GameObject>();

            foreach (Transform listZone in allListZones)
            {
                // Sauvegarder l'�tat d'activation initial
                bool wasActive = listZone.gameObject.activeSelf;
                GridLayoutGroup gridLayout = listZone.GetComponent<GridLayoutGroup>();

                // Activer temporairement pour la mise � jour
                listZone.gameObject.SetActive(true);

                bool instanceExists = false;
                foreach (Transform child in listZone)
                {
                    var existingElement = child.GetComponent<ZoneUIElementMenu>();
                    if (existingElement != null && existingElement.zoneId == zoneData.id)
                    {
                        instanceExists = true;
                        instances.Add(child.gameObject);
                        break;
                    }
                }

                if (!instanceExists)
                {
                    // D�sactiver temporairement le GridLayoutGroup
                    if (gridLayout != null)
                    {
                        gridLayout.enabled = false;
                    }

                    GameObject menuZoneUI = Instantiate(zoneController.MenuZonePrefab, listZone);
                    ZoneUIElementMenu zoneElement = menuZoneUI.GetComponent<ZoneUIElementMenu>();
                    zoneElement.Initialize(zoneData.id, zoneController);

                    Image menuZoneImage = menuZoneUI.GetComponent<Image>();
                    if (menuZoneImage != null)
                    {
                        Color newColor;
                        if (ColorUtility.TryParseHtmlString(zoneData.color, out newColor))
                        {
                            menuZoneImage.color = newColor;
                        }
                    }

                    instances.Add(menuZoneUI);

                    // R�activer et forcer la mise � jour du GridLayoutGroup
                    if (gridLayout != null)
                    {
                        gridLayout.enabled = true;

                        // Forcer le recalcul des positions
                        gridLayout.CalculateLayoutInputHorizontal();
                        gridLayout.CalculateLayoutInputVertical();
                        gridLayout.SetLayoutHorizontal();
                        gridLayout.SetLayoutVertical();
                    }

                    // R�organiser
                    Transform addZone = listZone.Find("AddZone");
                    if (addZone != null)
                    {
                        addZone.SetAsLastSibling();
                    }

                    // Forcer la mise � jour du layout
                    var rectTransform = listZone as RectTransform;
                    if (rectTransform != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    }
                }

                // Restaurer l'�tat d'activation original
                listZone.gameObject.SetActive(wasActive);

                // Forcer une derni�re mise � jour du Canvas parent
                var canvas = listZone.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Canvas.ForceUpdateCanvases();
                }
            }

            if (!zoneController.menuZoneInstances.ContainsKey(zoneData.id))
            {
                zoneController.menuZoneInstances[zoneData.id] = instances;
            }
        }
    }
}
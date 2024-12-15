using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;  // Pour LayoutGroup et LayoutRebuilder

public class WaiterManager : MonoBehaviour
{
    public static WaiterManager Instance { get; private set; }

    [SerializeField] private GameObject waiterPrefab;

    private Dictionary<string, List<WaiterUI>> waiterInstances = new Dictionary<string, List<WaiterUI>>();
    private List<Transform> assignedGrids;
    private List<Transform> nonAssignedGrids;

    private void Awake()
    {
        Debug.Log("WaiterManager Awake called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("WaiterManager instance set");
        }
        else
        {
            Debug.Log("Destroying duplicate WaiterManager");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Debug.Log("WaiterManager OnEnable called");
    }


    private void Start()
    {
        Debug.Log("WaiterManager Start called");

        // Vérifier si le prefab est assigné
        if (waiterPrefab == null)
        {
            Debug.LogError("Waiter prefab is not assigned in the inspector!");
        }

        RefreshGrids();

        // Forcer une nouvelle recherche après un court délai
        Invoke("DelayedRefresh", 1f);
    }

    private void DelayedRefresh()
    {
        Debug.Log("Performing delayed grid refresh...");
        RefreshGrids();
    }

    public void RefreshGrids()
    {
        Debug.Log("RefreshGrids called");



        // Utiliser Resources.FindObjectsOfTypeAll pour trouver aussi les objets inactifs
        assignedGrids = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(t => t.parent != null
                   && t.parent.name == "AssignateWaiterPanel"
                   && t.name == "GridAssigned")
            .ToList();

        nonAssignedGrids = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(t => t.parent != null
                   && t.parent.name == "AssignateWaiterPanel"
                   && t.name == "GridNonAssigned")
            .ToList();

        Debug.Log($"Found GridAssigned count: {assignedGrids.Count}");
        Debug.Log($"Found GridNonAssigned count: {nonAssignedGrids.Count}");

        // Pour debug, afficher le chemin complet et l'état d'activation
        foreach (var grid in assignedGrids)
        {
            Debug.Log($"Found GridAssigned at: {GetGameObjectPath(grid)} (Active: {grid.gameObject.activeInHierarchy})");
        }
        foreach (var grid in nonAssignedGrids)
        {
            Debug.Log($"Found GridNonAssigned at: {GetGameObjectPath(grid)} (Active: {grid.gameObject.activeInHierarchy})");
        }
    }

    private string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }








    public void UpdateWaiters(List<WaiterData> assignedWaiters, List<WaiterData> nonAssignedWaiters, Dictionary<string, Color> waiterColors)
    {
        if (waiterPrefab == null)
        {
            Debug.LogError("Waiter prefab is not assigned!");
            return;
        }

        Debug.Log($"Starting UpdateWaiters with {assignedWaiters?.Count ?? 0} assigned and {nonAssignedWaiters?.Count ?? 0} non-assigned");

        // HashSet pour suivre les waiters déjà traités et éviter les doublons
        HashSet<string> assignedWaiterIds = new HashSet<string>();

        // Traiter d'abord les waiters assignés (ceux des zones)
        List<WaiterData> uniqueAssignedWaiters = new List<WaiterData>();
        if (assignedWaiters != null)
        {
            foreach (var waiter in assignedWaiters)
            {
                if (!assignedWaiterIds.Contains(waiter.id))
                {
                    uniqueAssignedWaiters.Add(waiter);
                    assignedWaiterIds.Add(waiter.id);
                    Debug.Log($"Adding unique assigned waiter: {waiter.name}");
                }
            }
        }

        // Nettoyer toutes les instances existantes
        ClearAllWaiters();

        // Créer les waiters assignés uniques
        foreach (var waiter in uniqueAssignedWaiters)
        {
            if (waiterColors.TryGetValue(waiter.id, out Color zoneColor))
            {
                Color colorWithAlpha = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.5f);
                CreateWaiterInstances(waiter, true, colorWithAlpha);
            }
            else
            {
                CreateWaiterInstances(waiter, true);
            }
        }

        // Créer les waiters non-assignés
        if (nonAssignedWaiters != null)
        {
            foreach (var waiter in nonAssignedWaiters)
            {
                if (!assignedWaiterIds.Contains(waiter.id))
                {
                    CreateWaiterInstances(waiter, false);
                    Debug.Log($"Creating non-assigned waiter: {waiter.name}");
                }
            }
        }
    }

    private void CreateWaiterInstances(WaiterData waiterData, bool isAssigned)
    {
        if (waiterPrefab == null) return;

        var targetGrids = isAssigned ? assignedGrids : nonAssignedGrids;
        if (targetGrids == null || targetGrids.Count == 0)
        {
            Debug.LogError($"No {(isAssigned ? "assigned" : "non-assigned")} grids found!");
            return;
        }

        List<WaiterUI> instances = new List<WaiterUI>();

        foreach (var grid in targetGrids)
        {
            if (grid == null) continue;

            try
            {
                bool wasActive = grid.gameObject.activeSelf;
                grid.gameObject.SetActive(true);

                GameObject waiterObj = Instantiate(waiterPrefab, grid);
                waiterObj.name = $"Waiter_{waiterData.name}";
                waiterObj.SetActive(true);

                WaiterUI waiterUI = waiterObj.GetComponent<WaiterUI>();
                if (waiterUI != null)
                {
                    waiterUI.Initialize(waiterData);
                    instances.Add(waiterUI);
                    Debug.Log($"Created {(isAssigned ? "assigned" : "non-assigned")} waiter: {waiterData.name}");
                }

                grid.gameObject.SetActive(wasActive);

                // Forcer la mise à jour du layout
                if (grid is RectTransform rectTransform)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating waiter instance: {e.Message}");
            }
        }

        if (instances.Count > 0)
        {
            waiterInstances[waiterData.id] = instances;
        }
    }

    private void CreateWaiterInstances(WaiterData waiterData, bool isAssigned, Color color)
    {
        if (waiterPrefab == null) return;

        var targetGrids = isAssigned ? assignedGrids : nonAssignedGrids;
        if (targetGrids == null || targetGrids.Count == 0)
        {
            Debug.LogError($"No {(isAssigned ? "assigned" : "non-assigned")} grids found!");
            return;
        }

        List<WaiterUI> instances = new List<WaiterUI>();

        foreach (var grid in targetGrids)
        {
            if (grid == null) continue;

            try
            {
                bool wasActive = grid.gameObject.activeSelf;
                grid.gameObject.SetActive(true);

                GameObject waiterObj = Instantiate(waiterPrefab, grid);
                waiterObj.name = $"Waiter_{waiterData.name}";
                waiterObj.SetActive(true);

                WaiterUI waiterUI = waiterObj.GetComponent<WaiterUI>();
                if (waiterUI != null)
                {
                    waiterUI.Initialize(waiterData);
                    waiterUI.SetBackgroundColor(color);
                    instances.Add(waiterUI);
                    Debug.Log($"Created {(isAssigned ? "assigned" : "non-assigned")} waiter: {waiterData.name}");
                }

                grid.gameObject.SetActive(wasActive);

                // Forcer la mise à jour du layout
                if (grid is RectTransform rectTransform)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating waiter instance: {e.Message}");
            }
        }

        if (instances.Count > 0)
        {
            waiterInstances[waiterData.id] = instances;
        }
    }
    public void UpdateWaiterStatus(string waiterId, string newStatus)
    {
        if (waiterInstances.TryGetValue(waiterId, out List<WaiterUI> instances))
        {
            foreach (var instance in instances)
            {
                instance.UpdateStatus(newStatus);
            }
        }
    }

    private void ClearAllWaiters()
    {
        foreach (var instancesList in waiterInstances.Values)
        {
            foreach (var instance in instancesList)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
            }
        }
        waiterInstances.Clear();
    }
}
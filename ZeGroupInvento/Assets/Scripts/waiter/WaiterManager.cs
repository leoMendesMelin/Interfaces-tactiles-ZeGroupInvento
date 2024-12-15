using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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








    public void UpdateWaiters(List<WaiterData> assignedWaiters, List<WaiterData> nonAssignedWaiters)
    {
        Debug.Log($"UpdateWaiters called with {assignedWaiters?.Count ?? 0} assigned and {nonAssignedWaiters?.Count ?? 0} non-assigned waiters");

        if (waiterPrefab == null)
        {
            Debug.LogError("Waiter prefab is not assigned!");
            return;
        }

        ClearAllWaiters();

        // Créer les waiters assignés
        foreach (var waiter in assignedWaiters ?? new List<WaiterData>())
        {
            CreateWaiterInstances(waiter, true);
        }

        // Créer les waiters non-assignés
        foreach (var waiter in nonAssignedWaiters ?? new List<WaiterData>())
        {
            Debug.Log($"Creating non-assigned waiter: {waiter.name}");
            CreateWaiterInstances(waiter, false);
        }
    }

    private void CreateWaiterInstances(WaiterData waiterData, bool isAssigned)
    {
        if (waiterPrefab == null)
        {
            Debug.LogError("Waiter prefab is not assigned in WaiterManager!");
            return;
        }

        var targetGrids = isAssigned ? assignedGrids : nonAssignedGrids;
        Debug.Log($"Creating {(isAssigned ? "assigned" : "non-assigned")} waiter {waiterData.name} in {targetGrids.Count} grids");

        if (targetGrids.Count == 0)
        {
            Debug.LogError($"No {(isAssigned ? "assigned" : "non-assigned")} grids found!");
            return;
        }

        List<WaiterUI> instances = new List<WaiterUI>();

        foreach (var grid in targetGrids)
        {
            try
            {
                // Activer temporairement le parent pour permettre l'instantiation
                bool wasActive = grid.gameObject.activeSelf;
                grid.gameObject.SetActive(true);

                GameObject waiterObj = Instantiate(waiterPrefab, grid);
                waiterObj.name = $"Waiter_{waiterData.name}";

                // S'assurer que le waiter est actif
                waiterObj.SetActive(true);

                WaiterUI waiterUI = waiterObj.GetComponent<WaiterUI>();
                if (waiterUI == null)
                {
                    Debug.LogError("WaiterUI component not found on instantiated prefab!");
                    continue;
                }

                waiterUI.Initialize(waiterData);
                instances.Add(waiterUI);

                Debug.Log($"Successfully created waiter {waiterData.name} in {grid.name}");

                // Restaurer l'état d'activation original
                grid.gameObject.SetActive(wasActive);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating waiter instance: {e}");
            }
        }

        waiterInstances[waiterData.id] = instances;
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
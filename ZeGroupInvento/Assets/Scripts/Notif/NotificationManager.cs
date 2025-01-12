using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private GameObject notifModifTablesPrefab;

    void Awake()
    {
        if (notifModifTablesPrefab == null)
        {
            Debug.LogError("NotifModifTables prefab is not assigned in NotificationManager!");
        }
    }

    public void CreateTableUpdateNotification(string waiterName, string requestId, RoomElement[] tables)
    {
        Debug.Log($"Creating notification for waiter: {waiterName}, requestId: {requestId}");

        // Trouver tous les NotifPanel, y compris les inactifs
        var notifPanels = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.CompareTag("NotifPanel"))
            .ToArray();

        Debug.Log($"Found {notifPanels.Length} NotifPanels (including inactive)");

        if (notifPanels.Length == 0)
        {
            Debug.LogWarning("No NotifPanels found! Searching by name...");

            // Recherche alternative par nom, incluant les objets inactifs
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            notifPanels = allObjects.Where(go => go.name == "NotifPanel").ToArray();

            if (notifPanels.Length > 0)
            {
                Debug.Log($"Found {notifPanels.Length} NotifPanels by name");
            }
        }

        foreach (var panel in notifPanels)
        {
            Debug.Log($"Creating notification in panel: {panel.name} (Active: {panel.activeInHierarchy})");

            // Activer le panel s'il est inactif
            if (!panel.activeInHierarchy)
            {
                panel.SetActive(true);
                Debug.Log($"Activated panel: {panel.name}");
            }

            // Instancier le prefab
            GameObject notification = Instantiate(notifModifTablesPrefab, panel.transform);
            if (notification == null)
            {
                Debug.LogError("Failed to instantiate notification prefab!");
                continue;
            }

            // Configurer le nom du waiter
            var nameWaiterText = notification.transform.Find("Container/NameWaiter/Text (TMP)");
            if (nameWaiterText != null)
            {
                var tmp = nameWaiterText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = waiterName;
                }
            }

            // Configurer les boutons
            SetupButton(notification, "Container/ACCEPT", () => HandleAccept(requestId, tables));
            SetupButton(notification, "Container/REJECT", () => HandleReject(requestId, tables));

            // S'assurer que la notification est active
            notification.SetActive(true);
        }
    }

    private void SetupButton(GameObject notification, string path, System.Action onClick)
    {
        var buttonObj = notification.transform.Find(path);
        if (buttonObj != null)
        {
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => onClick());
            }
        }
    }

    private void HandleAccept(string requestId, RoomElement[] tables)
    {
        Debug.Log($"Accepting request: {requestId}");
        SendUpdateResponse(requestId, true, tables);
    }

    private void HandleReject(string requestId, RoomElement[] tables)
    {
        Debug.Log($"Rejecting request: {requestId}");
        SendUpdateResponse(requestId, false, tables);
    }

    private void SendUpdateResponse(string requestId, bool approved, RoomElement[] tables)
    {
        var webSocketManager = FindObjectOfType<WebSocketManager>();
        if (webSocketManager != null)
        {
            webSocketManager.SendTableUpdateResponse(requestId, approved, tables);
        }
        else
        {
            Debug.LogError("WebSocketManager not found!");
        }
    }
}
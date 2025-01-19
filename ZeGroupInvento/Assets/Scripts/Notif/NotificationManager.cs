using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private GameObject notifModifTablesPrefab;
    private Dictionary<string, List<GameObject>> previewInstances = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, List<GameObject>> tablesBeingMoved = new Dictionary<string, List<GameObject>>();


    private GridUIManager gridUIManager;

    private void Awake()
    {
        if (notifModifTablesPrefab == null)
        {
            Debug.LogError("[NotificationManager] ERREUR: notifModifTablesPrefab n'est pas assigné!");
            return;
        }
        gridUIManager = FindObjectOfType<GridUIManager>();
    }

    public void CreateTableUpdateNotification(string waiterName, string requestId, RoomElement[] tables)
    {
        Debug.Log($"[NotificationManager] Début création notification pour {waiterName}");

        var notifPanels = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.name == "NotifPanel")
            .ToArray();

        Debug.Log($"[NotificationManager] Nombre de NotifPanels trouvés: {notifPanels.Length}");

        if (notifPanels.Length == 0)
        {
            Debug.LogError("[NotificationManager] Aucun NotifPanel trouvé!");
            return;
        }

        foreach (var panel in notifPanels)
        {
            GameObject notification = Instantiate(notifModifTablesPrefab, panel.transform);
            if (notification == null) continue;

            SetupNotificationText(notification, waiterName, tables.Length);
            SetupButtonColors(notification);
            SetupPreviewButton(notification, requestId, tables);
            SetupActionButtons(notification, requestId, tables);
        }
    }

    private void SetupNotificationText(GameObject notification, string waiterName, int tableCount)
    {
        // Cherchons directement le TextMeshProUGUI sur le NameWaiter
        var nameWaiterText = notification.transform.Find("Container/NameWaiter")?.GetComponent<TextMeshProUGUI>();
        if (nameWaiterText != null)
        {
            nameWaiterText.text = $"Serveur: {waiterName}";
            nameWaiterText.color = Color.white;
        }
        else
        {
            Debug.LogError($"[NotificationManager] TextMeshProUGUI non trouvé sur NameWaiter. Structure actuelle: {GetGameObjectHierarchy(notification)}");
        }

        SetText(notification, "Container/TitleNotif/Text (TMP)", $"Demande de modification de {tableCount} table(s)");
    }

    private string GetGameObjectHierarchy(GameObject obj)
    {
        string hierarchy = obj.name;
        Transform current = obj.transform;
        while (current.parent != null)
        {
            current = current.parent;
            hierarchy = current.name + "/" + hierarchy;
        }
        return hierarchy;
    }

    private void SetupButtonColors(GameObject notification)
    {
        SetButtonColor(notification, "Container/ACCEPT", new Color(0.2f, 0.8f, 0.2f), new Color(0.3f, 0.9f, 0.3f));
        SetButtonColor(notification, "Container/REJECT", new Color(0.8f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.3f));
    }

    private void SetText(GameObject notification, string path, string text)
    {
        var textComponent = notification.transform.Find(path)?.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = Color.white;
        }
    }

    private void SetButtonColor(GameObject notification, string path, Color normalColor, Color highlightedColor)
    {
        var button = notification.transform.Find(path)?.GetComponent<Button>();
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            button.colors = colors;
        }
    }

    private void SetupPreviewButton(GameObject notification, string requestId, RoomElement[] tables)
    {
        var previewBtn = notification.transform.Find("Container/Preview")?.GetComponent<Button>();
        if (previewBtn == null) return;

        var eventTrigger = previewBtn.gameObject.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => ShowPreview(requestId, tables));
        eventTrigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => HidePreview(requestId));
        eventTrigger.triggers.Add(pointerUp);
    }

    private void ShowPreview(string requestId, RoomElement[] futureTables)
    {
        previewInstances[requestId] = new List<GameObject>();
        var gridUIManager = FindObjectOfType<GridUIManager>();

        foreach (var futureTable in futureTables)
        {
            // Trouver la table existante correspondante via son ID
            GameObject existingTable = gridUIManager.elementInstances[futureTable.id];
            if (existingTable != null)
            {
                // Animer la table existante
                var existingRectTransform = existingTable.GetComponent<RectTransform>();
                StartCoroutine(AnimatePreviewScale(existingRectTransform));

                // Créer la prévisualisation de la future position
                var previewTable = CreatePreviewTable(futureTable);
                if (previewTable != null)
                {
                    StartCoroutine(AnimatePreviewScale(previewTable.GetComponent<RectTransform>()));
                    previewInstances[requestId].Add(previewTable);
                }
            }
        }
    }

    private void HidePreview(string requestId)
    {
        if (previewInstances.ContainsKey(requestId))
        {
            foreach (var preview in previewInstances[requestId])
            {
                if (preview != null) Destroy(preview);
            }
            previewInstances.Remove(requestId);
        }
        StopAllCoroutines(); // Arrête toutes les animations
    }


    private GameObject CreatePreviewTable(RoomElement table)
    {
        var mapping = gridUIManager.prefabMappings.Find(pm => pm.elementType == table.type);
        if (mapping == null) return null;

        var backgroundPanel = GameObject.Find("BackgroundPanel").GetComponent<RectTransform>();
        var previewTable = Instantiate(mapping.prefab, backgroundPanel);

        var rectTransform = previewTable.GetComponent<RectTransform>();
        var gridManager = FindObjectOfType<GridManager>();

        Vector2Int gridPosition = new Vector2Int(
            Mathf.RoundToInt(table.position.x),
            Mathf.RoundToInt(table.position.y)
        );

        rectTransform.anchoredPosition = gridManager.GetWorldPosition(gridPosition);
        rectTransform.rotation = Quaternion.Euler(0, 0, table.rotation);

        // Définir la couleur de prévisualisation et ajouter l'animation
        var tableImage = previewTable.transform.Find("Tbale")?.GetComponent<Image>();
        if (tableImage != null)
        {
            tableImage.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        }

        return previewTable;
    }

    private IEnumerator AnimatePreviewScale(RectTransform rectTransform)
    {
        Vector3 originalScale = rectTransform.localScale;
        float animationSpeed = 2f;
        float scaleAmount = 0.2f;

        while (true)
        {
            float scale = 1f + scaleAmount * Mathf.Sin(Time.time * animationSpeed);
            rectTransform.localScale = originalScale * scale;
            yield return null;
        }
    }

  

    private void SetupButton(GameObject notification, string buttonPath, UnityAction action)
    {
        var buttonTransform = notification.transform.Find(buttonPath);
        if (buttonTransform != null)
        {
            var button = buttonTransform.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(action);
                Debug.Log($"[NotificationManager] Bouton configuré: {buttonPath}");
            }
        }
    }

    private void SetupActionButtons(GameObject notification, string requestId, RoomElement[] tables)
    {
        SetupButton(notification, "Container/ACCEPT", () => OnAcceptClick(requestId, tables));
        SetupButton(notification, "Container/REJECT", () => OnRejectClick(requestId, tables));
    }

    private void OnAcceptClick(string requestId, RoomElement[] tables)
    {
        var room = RoomManager.Instance.GetCurrentRoom();
        var socketManager = FindObjectOfType<WebSocketManager>();

        HidePreview(requestId);

        foreach (var updatedTable in tables)
        {
            RoomManager.Instance.updateUIElement(updatedTable);
            var existingTable = room.elements.FirstOrDefault(e => e.id == updatedTable.id);
            if (existingTable != null)
            {
                existingTable.position = updatedTable.position;
                existingTable.rotation = updatedTable.rotation;
                existingTable.isBeingEdited = false;
            }
        }

        if (socketManager != null)
        {
            // Envoyer toutes les tables au serveur
            foreach (var table in tables)
            {
                socketManager.EmitElementDragEnd(table);
            }
            socketManager.SendTableUpdateResponse(requestId, true, tables);
        }
    }

    private void OnRejectClick(string requestId, RoomElement[] tables)
    {
        HidePreview(requestId);

        foreach (var table in tables)
        {
            table.isBeingEdited = false;
        }

        var socketManager = FindObjectOfType<WebSocketManager>();
        if (socketManager != null)
        {
            socketManager.SendTableUpdateResponse(requestId, false, tables);
        }
    }
}
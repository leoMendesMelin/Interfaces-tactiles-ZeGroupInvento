using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private GameObject notifModifTablesPrefab;

    private void Awake()
    {
        if (notifModifTablesPrefab == null)
        {
            Debug.LogError("[NotificationManager] ERREUR: notifModifTablesPrefab n'est pas assigné!");
            return;
        }
        Debug.Log("[NotificationManager] Initialized successfully");
    }

    public void CreateTableUpdateNotification(string waiterName, string requestId, RoomElement[] tables)
    {
        Debug.Log($"[NotificationManager] Début création notification pour {waiterName}");

        if (notifModifTablesPrefab == null)
        {
            Debug.LogError("[NotificationManager] Erreur: prefab non disponible!");
            return;
        }

        // On utilise Resources.FindObjectsOfTypeAll pour trouver TOUS les GameObject, même désactivés
        var notifPanels = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.name == "NotifPanel")
            .ToArray();

        Debug.Log($"[NotificationManager] Nombre de NotifPanels trouvés (incluant inactifs): {notifPanels.Length}");

        if (notifPanels.Length == 0)
        {
            Debug.LogError("[NotificationManager] Aucun NotifPanel trouvé, même inactif!");
            return;
        }

        foreach (var panel in notifPanels)
        {
            GameObject notification = Instantiate(notifModifTablesPrefab, panel.transform);
            if (notification != null)
            {
                // Configuration du texte serveur 
                var waiterText = notification.transform.Find("Container/NameWaiter/Text (TMP)")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (waiterText != null)
                {
                    waiterText.text = $"Serveur: {waiterName}";
                    waiterText.color = Color.white;
                }

                // Configuration titre
                var titleText = notification.transform.Find("Container/TitleNotif/Text (TMP)")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = $"Demande de modification de {tables.Length} table(s)";
                    titleText.color = new Color(0.9f, 0.9f, 0.9f); // Gris clair
                }

                // Configuration boutons avec couleurs
                var acceptBtn = notification.transform.Find("Container/ACCEPT")?.GetComponent<Button>();
                if (acceptBtn != null)
                {
                    // Vert validation
                    var btnColors = acceptBtn.colors;
                    btnColors.normalColor = new Color(0.2f, 0.8f, 0.2f);
                    btnColors.highlightedColor = new Color(0.3f, 0.9f, 0.3f);
                    acceptBtn.colors = btnColors;
                }

                var rejectBtn = notification.transform.Find("Container/REJECT")?.GetComponent<Button>();
                if (rejectBtn != null)
                {
                    // Rouge refus  
                    var btnColors = rejectBtn.colors;
                    btnColors.normalColor = new Color(0.8f, 0.2f, 0.2f);
                    btnColors.highlightedColor = new Color(0.9f, 0.3f, 0.3f);
                    rejectBtn.colors = btnColors;
                }

                // Ajouter les listeners
                SetupButton(notification, "Container/ACCEPT", () => OnAcceptClick(requestId, tables));
                SetupButton(notification, "Container/REJECT", () => OnRejectClick(requestId, tables));
            }
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
            else
            {
                Debug.LogError($"[NotificationManager] Bouton component manquant sur {buttonPath}");
            }
        }
        else
        {
            Debug.LogError($"[NotificationManager] Chemin du bouton non trouvé: {buttonPath}");
        }
    }

    private void ConfigureButton(GameObject notification, string path, UnityAction action)
    {
        var button = notification.transform.Find(path)?.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(action);
            Debug.Log($"[NotificationManager] Bouton configuré: {path}");
        }
        else
        {
            Debug.LogError($"[NotificationManager] ERREUR: Bouton non trouvé: {path}");
        }
    }

    private void OnAcceptClick(string requestId, RoomElement[] tables)
    {
        Debug.Log($"[NotificationManager] Accept clicked for request: {requestId}");
        SendUpdateResponse(true, requestId, tables);
    }

    private void OnRejectClick(string requestId, RoomElement[] tables)
    {
        Debug.Log($"[NotificationManager] Reject clicked for request: {requestId}");
        SendUpdateResponse(false, requestId, tables);
    }

    private void SendUpdateResponse(bool approved, string requestId, RoomElement[] tables)
    {
        var socketManager = FindObjectOfType<WebSocketManager>();
        if (socketManager != null)
        {
            socketManager.SendTableUpdateResponse(requestId, approved, tables);
            Debug.Log($"[NotificationManager] Réponse envoyée: {(approved ? "Accept" : "Reject")}");
        }
        else
        {
            Debug.LogError("[NotificationManager] ERREUR: WebSocketManager non trouvé!");
        }
    }
}
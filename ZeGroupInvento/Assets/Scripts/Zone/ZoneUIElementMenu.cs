using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneUIElementMenu : MonoBehaviour
{
    private string zoneId;
    private ZoneUIControllerAddDelete controller;
    private Button deleteButton;

    public void Initialize(string zoneId, ZoneUIControllerAddDelete controller)
    {
        this.zoneId = zoneId;
        this.controller = controller;

        deleteButton = transform.Find("deleteButton")?.GetComponent<Button>();
        if (deleteButton == null)
        {
            Debug.LogError("deleteButton non trouvé dans les enfants du prefab Zone");
            return;
        }

        Button zoneButton = GetComponent<Button>();
        if (zoneButton != null)
        {
            zoneButton.onClick.AddListener(OnSelected);
        }

        // Configurer le bouton de suppression
        deleteButton.onClick.AddListener(() => OnZoneDeleted());
    }

    private void OnSelected()
    {
        if (deleteButton != null)
        {
            controller.OnZoneSelected(zoneId, deleteButton);
        }
    }

    public void OnZoneDeleted()
    {
        controller.OnZoneDeleted(zoneId);
    }

    private void OnDestroy()
    {
        // Nettoyage des listeners si nécessaire
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
        }
    }
}
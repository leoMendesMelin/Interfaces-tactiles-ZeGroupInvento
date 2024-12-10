using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneUIElementMenu : MonoBehaviour
{
    private string zoneId;
    private ZoneUIControllerAddDelete controller;
    private Button deleteButton; // Ajout de la référence au deleteButton

    public void Initialize(string zoneId, ZoneUIControllerAddDelete controller)
    {
        this.zoneId = zoneId;
        this.controller = controller;

        // Trouver le bouton de suppression dans les enfants
        deleteButton = transform.Find("deleteButton")?.GetComponent<Button>();
        if (deleteButton == null)
        {
            Debug.LogError("deleteButton non trouvé dans les enfants du prefab Zone");
        }

        // Ajouter un listener pour la sélection de la zone entière
        Button zoneButton = GetComponent<Button>();
        if (zoneButton != null)
        {
            zoneButton.onClick.AddListener(OnSelected);
        }
    }

    private void OnSelected()
    {
        if (deleteButton != null)
        {
            controller.OnZoneSelected(zoneId, deleteButton);
        }
        else
        {
            Debug.LogError("Tentative d'utiliser OnSelected avec un deleteButton null");
        }
    }

    public void OnZoneDeleted()
    {
        controller.OnZoneDeleted(zoneId);
        Destroy(gameObject);
    }
}
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridUIManager : MonoBehaviour
{
    [System.Serializable]
    public class PrefabMapping
    {
        public string elementType;
        public GameObject prefab;
    }

    public List<PrefabMapping> prefabMappings;
    private Dictionary<string, GameObject> elementInstances = new Dictionary<string, GameObject>();
    private GridManager gridManager;
    private RectTransform backgroundPanel;
    private const float POSITION_OFFSET = 2f;

    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    public void Initialize(RectTransform panel)
    {
        backgroundPanel = panel;
        gridManager = FindObjectOfType<GridManager>();
    }


    private void ClearCurrentElements()
    {
        foreach (var instance in elementInstances.Values)
        {
            Destroy(instance);
        }
        elementInstances.Clear();
    }

    public void CreateOrUpdateElementUI(RoomElement element)
    {
        var prefabMapping = prefabMappings.Find(pm => pm.elementType == element.type);
        if (prefabMapping == null)
        {
            Debug.LogError($"No prefab found for element type: {element.type}");
            return;
        }

        // V�rifier si l'�l�ment existe d�j�
        if (elementInstances.TryGetValue(element.id, out GameObject existingElement))
        {
            // Mettre � jour la position de l'�l�ment existant
            RectTransform rectTransform = existingElement.GetComponent<RectTransform>();
            Vector2 worldPos = gridManager.GetWorldPosition(new Vector2Int(
                Mathf.RoundToInt(element.position.x),
                Mathf.RoundToInt(element.position.y)
            ));
            rectTransform.anchoredPosition = worldPos;
            rectTransform.rotation = Quaternion.Euler(0, 0, element.rotation);
        }
        else
        {
            // Cr�er un nouvel �l�ment
            GameObject elementObj = Instantiate(prefabMapping.prefab, backgroundPanel);
            RectTransform rectTransform = elementObj.GetComponent<RectTransform>();
            Vector2 worldPos = gridManager.GetWorldPosition(new Vector2Int(
                Mathf.RoundToInt(element.position.x),
                Mathf.RoundToInt(element.position.y)
            ));
            rectTransform.anchoredPosition = worldPos;
            rectTransform.rotation = Quaternion.Euler(0, 0, element.rotation);

            // Ajouter l'�l�ment au dictionnaire
            elementInstances[element.id] = elementObj;

            // Ajouter les composants n�cessaires (comme ElementDragHandler)
            ElementDragHandler dragHandler = elementObj.GetComponent<ElementDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.Initialize(element);
            }
        }
    }

    // Mettre � jour la m�thode DisplayElements pour utiliser CreateOrUpdateElementUI
    public void DisplayElements(RoomElement[] elements)
    {
        // Cr�er un ensemble d'IDs � conserver
        HashSet<string> activeIds = new HashSet<string>();

        foreach (var element in elements)
        {
            CreateOrUpdateElementUI(element);
            activeIds.Add(element.id);
        }

        // Supprimer les �l�ments qui ne sont plus pr�sents
        List<string> idsToRemove = new List<string>();
        foreach (var kvp in elementInstances)
        {
            if (!activeIds.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                idsToRemove.Add(kvp.Key);
            }
        }

        foreach (var id in idsToRemove)
        {
            elementInstances.Remove(id);
        }
    }

    public Vector2Int ValidatePosition(Vector2Int position)
    {
        Vector2Int newPosition = position;
        int attempts = 0;
        const int MAX_ATTEMPTS = 8;

        while (IsPositionOccupied(newPosition) && attempts < MAX_ATTEMPTS)
        {
            // Essayer les positions en spirale
            int offset = (attempts / 4 + 1) * (int)POSITION_OFFSET;
            switch (attempts % 4)
            {
                case 0: newPosition = position + new Vector2Int(offset, 0); break;  // Droite
                case 1: newPosition = position + new Vector2Int(0, offset); break;  // Haut
                case 2: newPosition = position + new Vector2Int(-offset, 0); break; // Gauche
                case 3: newPosition = position + new Vector2Int(0, -offset); break; // Bas
            }
            attempts++;
        }

        return newPosition;
    }

    private bool IsPositionOccupied(Vector2Int position)
    {
        foreach (var instance in elementInstances.Values)
        {
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            Vector2Int instancePos = gridManager.GetGridPosition(rectTransform.anchoredPosition);
            if (instancePos == position)
            {
                return true;
            }
        }
        return false;
    }
}
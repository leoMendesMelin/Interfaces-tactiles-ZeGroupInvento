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
    private const float POSITION_OFFSET = 1f;

    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    public void Initialize(RectTransform panel)
    {
        backgroundPanel = panel;
        gridManager = FindObjectOfType<GridManager>();
    }

    public void DisplayElements(RoomElement[] elements)
    {
        ClearCurrentElements();
        foreach (var element in elements)
        {
            CreateElementUI(element);
        }
    }

    private void ClearCurrentElements()
    {
        foreach (var instance in elementInstances.Values)
        {
            Destroy(instance);
        }
        elementInstances.Clear();
    }

    public void CreateElementUI(RoomElement element)
    {
        var prefabMapping = prefabMappings.Find(pm => pm.elementType == element.type);
        if (prefabMapping == null)
        {
            Debug.LogError($"No prefab found for element type: {element.type}");
            return;
        }

        GameObject elementObj = Instantiate(prefabMapping.prefab, backgroundPanel);
        RectTransform rectTransform = elementObj.GetComponent<RectTransform>();

        Vector2 worldPos = gridManager.GetWorldPosition(new Vector2Int(
            Mathf.RoundToInt(element.position.x),
            Mathf.RoundToInt(element.position.y)
        ));

        rectTransform.anchoredPosition = worldPos;
        elementInstances[element.id] = elementObj;
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
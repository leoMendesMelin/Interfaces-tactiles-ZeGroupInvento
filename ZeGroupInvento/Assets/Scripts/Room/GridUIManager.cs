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

    private void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    public void Initialize(RectTransform panel)
    {
        backgroundPanel = panel;
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

        int gridX = Mathf.RoundToInt(element.position.x);
        int gridY = Mathf.RoundToInt(element.position.y);
        Vector2 worldPos = gridManager.GetWorldPosition(new Vector2Int(gridX, gridY));

        rectTransform.anchoredPosition = worldPos;
        elementInstances[element.id] = elementObj;
    }
}
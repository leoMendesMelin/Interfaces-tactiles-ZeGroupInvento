using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ElementSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableElement
    {
        public string buttonName;
        public GameObject prefab;
    }

    [Header("Spawnable Elements")]
    public List<SpawnableElement> spawnableElements = new List<SpawnableElement>();

    private GridManager gridManager;
    private RoomManager roomManager;
    private GridUIManager gridUIManager;

    private void Awake()
    {
        roomManager = RoomManager.Instance;
        gridManager = FindObjectOfType<GridManager>();
        gridUIManager = FindObjectOfType<GridUIManager>();
    }
    private void Start()
    {
        ConfigureAllButtons();
    }

    private void ConfigureAllButtons()
    {
        Transform canvas = transform.root;
        foreach (var panelName in new[] { "MenuPanelSouth", "MenuPanelNorth" })
        {
            Transform panel = FindInactiveObject(canvas, panelName);
            if (panel != null)
            {
                ConfigureButtonsInPanel(panel);
            }
        }
    }

    private void ConfigureButtonsInPanel(Transform panel)
    {
        var buttons = GetAllButtons(panel);
        foreach (var button in buttons)
        {
            foreach (var element in spawnableElements)
            {
                if (button.gameObject.name.Equals(element.buttonName))
                {
                    var elementCopy = element;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SpawnElement(elementCopy.prefab));
                }
            }
        }
    }

    private List<Button> GetAllButtons(Transform parent)
    {
        List<Button> buttons = new List<Button>();

        // Obtenir le composant Button s'il existe
        Button button = parent.GetComponent<Button>();
        if (button != null)
        {
            buttons.Add(button);
        }

        // Recherche récursive dans les enfants
        foreach (Transform child in parent)
        {
            buttons.AddRange(GetAllButtons(child));
        }

        return buttons;
    }

    private Transform FindInactiveObject(Transform parent, string name)
    {
        if (parent == null) return null;

        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindInactiveObject(child, name);
            if (found != null)
                return found;
        }

        return null;
    }



    private void SpawnElement(GameObject prefab)
    {
        Vector2Int gridPos = gridManager.GetGridPosition(Vector2.zero); // Position centrale par défaut
        roomManager.AddElement(prefab.name, gridPos, 0f);
    }
}
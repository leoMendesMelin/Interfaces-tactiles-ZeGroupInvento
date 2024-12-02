using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ElementSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject toiletPrefab;
    public GameObject circleTablePrefab;
    public GameObject rectangleTablePrefab;

    [Header("Spawn Settings")]
    public float spawnRadius = 100f;
    public Vector2 centerOffset = new Vector2(0, 0);

    private List<GameObject> spawnedElements = new List<GameObject>();
    private Transform backgroundPanel;

    private void Start()
    {
        backgroundPanel = transform.parent;
        if (backgroundPanel == null || !backgroundPanel.name.Equals("BackgroundPanel"))
        {
            Debug.LogError($"Ce script doit être enfant du BackgroundPanel! Parent actuel : {(backgroundPanel != null ? backgroundPanel.name : "null")}");
            return;
        }

        Debug.Log($"BackgroundPanel trouvé : {backgroundPanel.name}");

        // Trouver TablesPanel en utilisant le chemin complet depuis Canvas
        Transform canvas = backgroundPanel.parent;
        if (canvas != null)
        {
            // Chercher TablesPanel dans tous les enfants de Canvas, même désactivés
            Transform tablesPanel = FindInactiveObject(canvas, "TablesPanel");

            if (tablesPanel != null)
            {
                Debug.Log("TablesPanel trouvé, même s'il est désactivé");
                SetupButtonInPanel(tablesPanel, "rectangleButtonSpawn", OnRectangleTableSpawn);
                SetupButtonInPanel(tablesPanel, "CircleButtonSpawn", OnCircleTableSpawn);
            }
            else
            {
                Debug.LogError("TablesPanel non trouvé dans la hiérarchie du Canvas!");
            }
        }

        if (toiletPrefab == null || circleTablePrefab == null || rectangleTablePrefab == null)
        {
            Debug.LogError("Un ou plusieurs prefabs ne sont pas assignés dans l'inspecteur!");
        }
    }

    private Transform FindInactiveObject(Transform parent, string name)
    {
        // Vérifier d'abord l'objet parent lui-même
        if (parent.name == name)
            return parent;

        // Parcourir tous les enfants, même désactivés
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            // Recherche récursive dans les enfants
            Transform found = FindInactiveObject(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private void SetupButtonInPanel(Transform panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        // Chercher le bouton dans le panel, même s'il est désactivé
        Transform buttonTransform = FindInactiveObject(panel, buttonName);
        if (buttonTransform == null)
        {
            Debug.LogError($"Bouton {buttonName} non trouvé dans TablesPanel!");
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            button = buttonTransform.gameObject.AddComponent<Button>();
            Debug.Log($"Composant Button ajouté à {buttonName}");
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        Debug.Log($"Bouton configuré avec succès : {buttonName}");
    }

    private void OnToiletSpawn()
    {
        Debug.Log("Toilet spawn button pressed");
        SpawnElement(toiletPrefab);
    }

    private void OnCircleTableSpawn()
    {
        Debug.Log("Circle table spawn button pressed");
        SpawnElement(circleTablePrefab);
    }

    private void OnRectangleTableSpawn()
    {
        Debug.Log("Rectangle table spawn button pressed");
        SpawnElement(rectangleTablePrefab);
    }

    private void SpawnElement(GameObject prefab)
    {
        if (prefab == null || backgroundPanel == null)
        {
            Debug.LogError("Prefab ou BackgroundPanel manquant!");
            return;
        }

        Vector2 spawnPosition = FindValidSpawnPosition();

        GameObject newElement = Instantiate(prefab, backgroundPanel);
        RectTransform rectTransform = newElement.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnPosition;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            spawnedElements.Add(newElement);
            Debug.Log($"Element spawned at position: {spawnPosition}");
        }
        else
        {
            Debug.LogError("L'élément spawné n'a pas de composant RectTransform!");
            Destroy(newElement);
        }
    }

    private Vector2 FindValidSpawnPosition()
    {
        Vector2 centerPos = centerOffset;

        if (spawnedElements.Count == 0)
            return centerPos;

        spawnedElements.RemoveAll(element => element == null);

        for (int ring = 1; ring <= spawnedElements.Count + 1; ring++)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (2 * Mathf.PI / 8);
                Vector2 testPos = centerPos + new Vector2(
                    Mathf.Cos(angle) * spawnRadius * ring,
                    Mathf.Sin(angle) * spawnRadius * ring
                );

                if (IsPositionFree(testPos))
                    return testPos;
            }
        }

        return centerPos + new Vector2(spawnedElements.Count * spawnRadius, 0);
    }

    private bool IsPositionFree(Vector2 position)
    {
        foreach (GameObject element in spawnedElements.ToList())
        {
            if (element == null) continue;

            RectTransform elementRect = element.GetComponent<RectTransform>();
            if (elementRect == null) continue;

            float distance = Vector2.Distance(elementRect.anchoredPosition, position);
            if (distance < spawnRadius)
                return false;
        }
        return true;
    }
}
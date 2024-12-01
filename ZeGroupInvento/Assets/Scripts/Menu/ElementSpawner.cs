using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ElementSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject toiletPrefab;
    public GameObject circleTablePrefab;
    public GameObject rectangleTablePrefab;

    [Header("Spawn Settings")]
    public float spawnRadius = 100f; // Distance entre les éléments
    public Vector2 centerOffset = new Vector2(0, 0); // Offset du centre de l'écran si nécessaire

    private List<GameObject> spawnedElements = new List<GameObject>();
    private Transform contentArea; // Référence à la zone de contenu

    private void Start()
    {
        // Trouver la zone de contenu
        contentArea = GameObject.Find("ContentArea").transform;

        // Setup des boutons
        SetupButton("Toilet", OnToiletSpawn);
        SetupButton("CircleButtonSpawn", OnCircleTableSpawn);
        SetupButton("rectangleButtonSpaw", OnRectangleTableSpawn);
    }

    private void SetupButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = GameObject.Find(buttonName)?.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
        else
        {
            Debug.LogWarning($"Button {buttonName} not found!");
        }
    }

    private void OnToiletSpawn()
    {
        SpawnElement(toiletPrefab);
    }

    private void OnCircleTableSpawn()
    {
        SpawnElement(circleTablePrefab);
    }

    private void OnRectangleTableSpawn()
    {
        SpawnElement(rectangleTablePrefab);
    }

    private void SpawnElement(GameObject prefab)
    {
        if (prefab == null) return;

        Vector2 spawnPosition = FindValidSpawnPosition();

        GameObject newElement = Instantiate(prefab, contentArea);
        RectTransform rectTransform = newElement.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnPosition;
            spawnedElements.Add(newElement);
        }
    }

    private Vector2 FindValidSpawnPosition()
    {
        Vector2 centerPos = centerOffset;

        // Si aucun élément n'est spawné, utiliser le centre
        if (spawnedElements.Count == 0)
            return centerPos;

        // Chercher une position valide
        for (int ring = 1; ring <= spawnedElements.Count + 1; ring++)
        {
            // Essayer 8 positions autour du centre
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (2 * Mathf.PI / 8);
                Vector2 testPos = centerPos + new Vector2(
                    Mathf.Cos(angle) * spawnRadius * ring,
                    Mathf.Sin(angle) * spawnRadius * ring
                );

                // Vérifier si la position est libre
                if (IsPositionFree(testPos))
                    return testPos;
            }
        }

        // Si aucune position n'est trouvée, retourner une position par défaut
        return centerPos + new Vector2(spawnedElements.Count * spawnRadius, 0);
    }

    private bool IsPositionFree(Vector2 position)
    {
        foreach (GameObject element in spawnedElements)
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
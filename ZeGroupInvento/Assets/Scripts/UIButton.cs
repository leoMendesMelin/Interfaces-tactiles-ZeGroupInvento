using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIButton : MonoBehaviour
{
    [SerializeField] private string prefabName;
    [SerializeField] private float spacing = 200f;
    [SerializeField] private float startX = 100f;
    [SerializeField] private float startY = 100f;
    [SerializeField] private int maxItemsPerRow = 4;

    private MenuManager menuManager;
    private static Dictionary<Vector2, GameObject> positionMap = new Dictionary<Vector2, GameObject>();
    private static Vector2 nextPosition;

    private void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        GetComponent<Button>().onClick.AddListener(OnClick);

        // Initialiser si c'est le premier bouton
        if (positionMap.Count == 0)
        {
            ResetPositions();
        }
    }

    private void OnClick()
    {
        // Nettoyer les positions des objets détruits avant de placer un nouveau
        CleanupDestroyedObjects();

        // Trouver une position libre
        Vector2 spawnPosition = FindFreePosition();

        // Créer l'objet
        menuManager.SpawnPrefab(prefabName);
        GameObject spawnedObject = menuManager.GetLastSpawnedObject();

        if (spawnedObject != null)
        {
            // Positionner l'objet
            RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = spawnPosition;
            }

            // Enregistrer la position
            positionMap[spawnPosition] = spawnedObject;

            // Commencer à surveiller la position de l'objet
            StartCoroutine(MonitorObjectPosition(spawnedObject, spawnPosition));
        }
    }

    private System.Collections.IEnumerator MonitorObjectPosition(GameObject obj, Vector2 originalPosition)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector2 lastPosition = rt.anchoredPosition;

        while (obj != null)
        {
            // Si l'objet a bougé significativement
            if (Vector2.Distance(rt.anchoredPosition, lastPosition) > 10f)
            {
                // Retirer l'ancienne position de la carte
                if (positionMap.ContainsKey(originalPosition))
                {
                    positionMap.Remove(originalPosition);
                }
                yield break; // Arrêter la surveillance une fois que l'objet a été déplacé
            }

            yield return new WaitForSeconds(0.5f); // Vérifier toutes les 0.5 secondes
        }
    }

    private Vector2 FindFreePosition()
    {
        Vector2 position = new Vector2(startX, startY);
        int currentRow = 0;
        int currentCol = 0;

        while (positionMap.ContainsKey(position))
        {
            currentCol++;
            if (currentCol >= maxItemsPerRow)
            {
                currentCol = 0;
                currentRow++;
                position = new Vector2(startX, startY - (spacing * currentRow));
            }
            else
            {
                position = new Vector2(startX + (spacing * currentCol), startY - (spacing * currentRow));
            }
        }

        return position;
    }

    private void CleanupDestroyedObjects()
    {
        List<Vector2> positionsToRemove = new List<Vector2>();

        foreach (var kvp in positionMap)
        {
            if (kvp.Value == null)
            {
                positionsToRemove.Add(kvp.Key);
            }
        }

        foreach (var pos in positionsToRemove)
        {
            positionMap.Remove(pos);
        }
    }

    private static void ResetPositions()
    {
        positionMap.Clear();
        nextPosition = new Vector2(0, 0);
    }

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            ResetPositions();
        }
    }
}
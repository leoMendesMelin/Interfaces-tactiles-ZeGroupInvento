using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private Dictionary<string, Coroutine> colorTransitionCoroutines = new Dictionary<string, Coroutine>();
    private const float COLOR_TRANSITION_DURATION = 0.5f; // Durée de la transition en secondes


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
        foreach (var coroutine in colorTransitionCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        colorTransitionCoroutines.Clear();

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

    // On part de la position grille
    Vector2Int gridPosition = new Vector2Int(
        Mathf.RoundToInt(element.position.x),
        Mathf.RoundToInt(element.position.y)
    );

    // Si l'élément existe, on le détruit
    if (elementInstances.TryGetValue(element.id, out GameObject existingElement))
    {
        Destroy(existingElement);
    }

    // Créer un nouvel élément
    GameObject elementObj = Instantiate(prefabMapping.prefab, backgroundPanel);
    RectTransform rectTransform = elementObj.GetComponent<RectTransform>();

    // Positionner directement sur la grille
    rectTransform.anchoredPosition = gridManager.GetWorldPosition(gridPosition);

    // Appliquer la rotation normalisée
    float normalizedRotation = element.rotation % 360;
    if (normalizedRotation < 0) normalizedRotation += 360;
    rectTransform.rotation = Quaternion.Euler(0, 0, normalizedRotation);

        if (element.type.StartsWith("TABLE_"))
        {
            Transform tTableTransform = elementObj.transform.Find("Tbale");
            if (tTableTransform != null)
            {
                Image tableImage = tTableTransform.GetComponent<Image>();
                if (tableImage != null)
                {
                    Color targetColor = GetColorForState(element.state);

                    // Arrêter la coroutine précédente si elle existe
                    if (colorTransitionCoroutines.ContainsKey(element.id))
                    {
                        StopCoroutine(colorTransitionCoroutines[element.id]);
                        colorTransitionCoroutines.Remove(element.id);
                    }

                    // Démarrer la nouvelle transition
                    Coroutine transitionCoroutine = StartCoroutine(TransitionColor(tableImage, targetColor));
                    colorTransitionCoroutines[element.id] = transitionCoroutine;
                }
            }
        }

        // Mettre à jour ou ajouter l'élément dans le dictionnaire
        elementInstances[element.id] = elementObj;

    // Initialiser ElementDragHandler
    ElementDragHandler dragHandler = elementObj.GetComponent<ElementDragHandler>();
    if (dragHandler != null)
    {
        dragHandler.Initialize(element);
    }
}

    private Color GetColorForState(string state)
    {
        switch (state)
        {
            case "Available":
                return new Color(0.0f, 1.0f, 0.0f, 1.0f); // Vert
            case "WaitingForOrder":
                return new Color(1.0f, 0.65f, 0.0f, 1.0f); // Orange
            case "WaitingForPayment":
                return new Color(1.0f, 0.0f, 0.0f, 1.0f); // Rouge
            default:
                return Color.white;
        }
    }

    private IEnumerator TransitionColor(Image image, Color targetColor)
    {
        Color startColor = image.color;
        float elapsedTime = 0f;

        while (elapsedTime < COLOR_TRANSITION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / COLOR_TRANSITION_DURATION;

            // Utiliser une courbe d'interpolation pour une transition plus fluide
            t = Mathf.SmoothStep(0, 1, t);

            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // S'assurer que la couleur finale est exactement la couleur cible
        image.color = targetColor;
    }



    // Mettre à jour la méthode DisplayElements pour utiliser CreateOrUpdateElementUI
    public void DisplayElements(RoomElement[] elements)
    {
        // Créer un ensemble d'IDs à conserver
        HashSet<string> activeIds = new HashSet<string>();

        foreach (var element in elements)
        {
            CreateOrUpdateElementUI(element);
            activeIds.Add(element.id);
        }

        // Supprimer les éléments qui ne sont plus présents
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
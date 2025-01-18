using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// ElementDragHandler.cs
public class ElementDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RoomElement elementData;
    private GridManager gridManager;
    private RoomManager roomManager;
    private Vector2 originalPosition;
    private WebSocketManager webSocketManager;

    private void Awake()
    {
        Debug.Log("ElementDragHandler Awake called");
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        gridManager = FindObjectOfType<GridManager>();
        roomManager = RoomManager.Instance;
        webSocketManager = FindObjectOfType<WebSocketManager>();

        if (webSocketManager == null)
        {
            Debug.LogError("WebSocketManager not found in scene. Make sure it exists!");
        }
    }

    public void Initialize(RoomElement element)
    {
        Debug.Log($"Initializing ElementDragHandler with element ID: {element?.id}");
        if (element == null)
        {
            Debug.LogError("Trying to initialize ElementDragHandler with null element");
            return;
        }
        elementData = element;

        // V�rifier que tous les composants n�cessaires sont pr�sents
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roomManager == null) roomManager = RoomManager.Instance;
        if (webSocketManager == null) webSocketManager = FindObjectOfType<WebSocketManager>();

      
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // V�rifications de s�curit�
        if (elementData == null)
        {
            Debug.LogError("elementData is null in OnBeginDrag");
            return;
        }
        if (webSocketManager == null)
        {
            Debug.LogError("webSocketManager is null in OnBeginDrag");
            return;
        }

        Debug.Log($"Beginning drag for element ID: {elementData.id}");
        originalPosition = rectTransform.anchoredPosition;
        elementData.isBeingEdited = true;
        webSocketManager.EmitElementDragStart(elementData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (elementData == null || webSocketManager == null || canvas == null)
        {
            Debug.LogError($"Missing components in OnDrag");
            return;
        }

        // Convertir la position de la souris en position locale dans le canvas
        Vector2 mousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            eventData.position,
            canvas.worldCamera,
            out mousePosition))
        {
            // Convertir en position grille
            Vector2Int gridPosition = gridManager.GetGridPosition(mousePosition);

            // Convertir la position grille en position monde et l'appliquer directement
            rectTransform.anchoredPosition = gridManager.GetWorldPosition(gridPosition);

            // Mettre � jour les donn�es de l'�l�ment
            elementData.position = new Position
            {
                x = gridPosition.x,
                y = gridPosition.y
            };

            // �mettre l'�v�nement de drag
            webSocketManager.EmitElementDragging(elementData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (elementData == null || webSocketManager == null || gridManager == null || roomManager == null)
        {
            Debug.LogError("Missing components in OnEndDrag");
            return;
        }

        Vector2Int newGridPosition = gridManager.GetGridPosition(rectTransform.anchoredPosition);

        // On v�rifie si c'est une table rectangulaire
        if (elementData.type.StartsWith("TABLE_RECT_"))
        {
            // Rechercher une table � proximit� (2 cases autour)
            RoomElement nearbyTable = FindNearbyTable(newGridPosition);

            if (nearbyTable != null)
            {
                // On peut fusionner les tables
                int currentSize = int.Parse(elementData.type.Substring(elementData.type.Length - 1));
                int otherSize = int.Parse(nearbyTable.type.Substring(nearbyTable.type.Length - 1));

                // V�rifier si la fusion est possible (max TABLE_RECT_6)
                if (currentSize + otherSize <= 6)
                {
                    // D�placer la table dragu�e hors de la grille
                    elementData.position = new Position { x = 99999, y = 99999 };
                    elementData.isBeingEdited = false;

                    // Mettre � jour le type de la table qui re�oit
                    nearbyTable.type = $"TABLE_RECT_{currentSize + otherSize}";

                    // Mettre � jour l'UI des deux tables
                    var gridUIManager = FindObjectOfType<GridUIManager>();
                    gridUIManager.CreateOrUpdateElementUI(elementData);
                    gridUIManager.CreateOrUpdateElementUI(nearbyTable);

                    // Envoyer la mise � jour au serveur
                    webSocketManager.EmitElementDragEnd(elementData);
                    return;
                }
            }
        }

        // Si pas de fusion, on applique simplement la nouvelle position
        elementData.position = new Position
        {
            x = newGridPosition.x,
            y = newGridPosition.y
        };
        elementData.isBeingEdited = false;
        webSocketManager.EmitElementDragEnd(elementData);
    }

    private RoomElement FindNearbyTable(Vector2Int position)
    {
        // V�rifier les 8 cases autour + la case actuelle
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector2Int checkPosition = position + new Vector2Int(dx, dy);

                RoomElement nearbyTable = roomManager.GetCurrentRoom().elements.FirstOrDefault(e =>
                    e.id != elementData.id &&
                    e.type.StartsWith("TABLE_RECT_") &&
                    Mathf.RoundToInt(e.position.x) == checkPosition.x &&
                    Mathf.RoundToInt(e.position.y) == checkPosition.y);

                if (nearbyTable != null)
                {
                    return nearbyTable;
                }
            }
        }
        return null;
    }
}
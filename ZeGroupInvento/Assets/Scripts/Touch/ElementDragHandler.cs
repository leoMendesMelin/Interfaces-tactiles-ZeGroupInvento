using System.Collections;
using System.Collections.Generic;
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

        // Vérifier que tous les composants nécessaires sont présents
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roomManager == null) roomManager = RoomManager.Instance;
        if (webSocketManager == null) webSocketManager = FindObjectOfType<WebSocketManager>();

      
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Vérifications de sécurité
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

            // Mettre à jour les données de l'élément
            elementData.position = new Position
            {
                x = gridPosition.x,
                y = gridPosition.y
            };

            // Émettre l'événement de drag
            webSocketManager.EmitElementDragging(elementData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Vérifications de sécurité
        if (elementData == null || webSocketManager == null || gridManager == null || roomManager == null)
        {
            Debug.LogError("Missing components in OnEndDrag");
            return;
        }

        Vector2Int newGridPosition = gridManager.GetGridPosition(rectTransform.anchoredPosition);
        
        elementData.position = new Position
        {
            x = newGridPosition.x,
            y = newGridPosition.y
        };
        elementData.isBeingEdited = false;
        webSocketManager.EmitElementDragEnd(elementData);
       
    }
}
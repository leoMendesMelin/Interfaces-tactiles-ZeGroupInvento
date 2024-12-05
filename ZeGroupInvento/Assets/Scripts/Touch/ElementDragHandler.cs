using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ElementDragHandler.cs
public class ElementDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Ajouter les interfaces manquantes
    private RectTransform rectTransform;
    private Canvas canvas;
    private RoomElement elementData;
    private GridManager gridManager;
    private RoomManager roomManager;
    private Vector2 originalPosition;
    private WebSocketManager webSocketManager; // Nouveau

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        gridManager = FindObjectOfType<GridManager>();
        roomManager = RoomManager.Instance;
        webSocketManager = FindObjectOfType<WebSocketManager>(); // Nouveau
    }

    public void Initialize(RoomElement element)
    {
        elementData = element;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        elementData.isBeingEdited = true;
        // Émettre l'événement de début de drag
        webSocketManager.EmitElementDragStart(elementData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            eventData.position,
            canvas.worldCamera,
            out position
        );

        rectTransform.anchoredPosition = position;

        // Émettre l'événement de drag en cours avec la nouvelle position
        elementData.position = new Position
        {
            x = position.x,
            y = position.y
        };
        webSocketManager.EmitElementDragging(elementData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2Int newGridPosition = gridManager.GetGridPosition(rectTransform.anchoredPosition);

        bool isValidPosition = roomManager.ValidatePosition(newGridPosition);

        if (isValidPosition)
        {
            elementData.position = new Position
            {
                x = newGridPosition.x,
                y = newGridPosition.y
            };
            elementData.isBeingEdited = false;

            // Émettre l'événement de fin de drag
            webSocketManager.EmitElementDragEnd(elementData);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ElementDragHandler : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RoomElement elementData;
    private GridManager gridManager;
    private RoomManager roomManager;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        gridManager = FindObjectOfType<GridManager>();
        roomManager = RoomManager.Instance;
    }

    public void Initialize(RoomElement element)
    {
        elementData = element;
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        PointerEventData pointerData = (PointerEventData)eventData;
        originalPosition = rectTransform.anchoredPosition;
        // Notifier que l'élément est en cours d'édition
        elementData.isBeingEdited = true;
    }

    public void OnDrag(BaseEventData eventData)
    {
        PointerEventData pointerData = (PointerEventData)eventData;

        // Convertir la position du pointeur en position locale dans le canvas
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            pointerData.position,
            canvas.worldCamera,
            out position
        );

        rectTransform.anchoredPosition = position;
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        Vector2Int newGridPosition = gridManager.GetGridPosition(rectTransform.anchoredPosition);

        // Vérifier si la position est valide
        bool isValidPosition = roomManager.ValidatePosition(newGridPosition);

        if (isValidPosition)
        {
            // Mettre à jour la position dans les données
            elementData.position = new Position
            {
                x = newGridPosition.x,
                y = newGridPosition.y
            };
            elementData.isBeingEdited = false;

            // Demander au RoomManager de mettre à jour l'élément
            roomManager.UpdateElement(elementData);
        }
        else
        {
            // Revenir à la position d'origine
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
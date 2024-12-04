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
        // Notifier que l'�l�ment est en cours d'�dition
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

        // V�rifier si la position est valide
        bool isValidPosition = roomManager.ValidatePosition(newGridPosition);

        if (isValidPosition)
        {
            // Mettre � jour la position dans les donn�es
            elementData.position = new Position
            {
                x = newGridPosition.x,
                y = newGridPosition.y
            };
            elementData.isBeingEdited = false;

            // Demander au RoomManager de mettre � jour l'�l�ment
            roomManager.UpdateElement(elementData);
        }
        else
        {
            // Revenir � la position d'origine
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
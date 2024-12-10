using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ZoneControllerPrefab : MonoBehaviour // c'est le script associé au prefab sur le bnackgroundPanel
{
    private ZoneData data;
    private ZoneManager manager;
    private GridManager gridManager;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private const float RESIZE_HANDLE_THRESHOLD = 20f;

    public void Initialize(ZoneData data, ZoneManager manager, GridManager gridManager)
    {
        this.data = data;
        this.manager = manager;
        this.gridManager = gridManager;
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        PointerEventData pointerData = (PointerEventData)eventData;
        isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, pointerData.position, pointerData.pressEventCamera, out dragOffset);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (!isDragging) return;

        PointerEventData pointerData = (PointerEventData)eventData;

        // Mettre à jour la position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, pointerData.position, pointerData.pressEventCamera, out localPoint);

        rectTransform.anchoredPosition += localPoint - dragOffset;
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        isDragging = false;
        // Snapper à la grille
        Vector2Int gridPos = gridManager.GetGridPosition(rectTransform.anchoredPosition);
        rectTransform.anchoredPosition = gridManager.GetWorldPosition(gridPos);
        data.position = gridPos;
    }

    public void OnDelete()
    {
        manager.DeleteZone(data.id);
    }
}
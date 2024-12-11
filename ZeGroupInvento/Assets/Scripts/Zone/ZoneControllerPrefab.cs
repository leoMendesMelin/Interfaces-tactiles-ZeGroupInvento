using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // Ajoutez cette ligne pour Image

public class ZoneControllerPrefab : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ZoneData data;
    private ZoneManager manager;
    private GridManager gridManager;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Vector2 lastGridPosition;

    public void Initialize(ZoneData data, ZoneManager manager, GridManager gridManager)
    {
        this.data = data;
        this.manager = manager;
        this.gridManager = gridManager;
        rectTransform = GetComponent<RectTransform>();

        // Appliquer la couleur lors de l'initialisation
        Image image = GetComponent<Image>();
        if (image != null && data.color != null)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString(data.color, out newColor))
            {
                image.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f); // Avec transparence
            }
        }
    }

    public void UpdateColor(string newColor)
    {
        if (data != null)
        {
            data.color = newColor;
            Image image = GetComponent<Image>();
            if (image != null)
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(newColor, out color))
                {
                    image.color = new Color(color.r, color.g, color.b, 0.5f); // Avec transparence
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        // Sauvegarder la position initiale sur la grille
        lastGridPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Convertir la position de la souris en position locale
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Obtenir la position sur la grille
            Vector2Int gridPos = gridManager.GetGridPosition(localPoint);
            // Convertir la position de la grille en position monde
            Vector2 newWorldPos = gridManager.GetWorldPosition(gridPos);

            // Ne mettre à jour que si la position a changé
            if (newWorldPos != lastGridPosition)
            {
                rectTransform.anchoredPosition = newWorldPos;
                lastGridPosition = newWorldPos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // Snapper à la position finale de la grille
        Vector2Int finalGridPos = gridManager.GetGridPosition(rectTransform.anchoredPosition);
        Vector2 finalWorldPos = gridManager.GetWorldPosition(finalGridPos);
        rectTransform.anchoredPosition = finalWorldPos;
        data.position = finalGridPos;
    }

    public void OnDelete()
    {
        manager.DeleteZone(data.id);
    }
}
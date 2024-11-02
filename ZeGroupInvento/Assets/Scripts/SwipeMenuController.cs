using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
public class SwipeMenuController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private float minSwipeDistance = 20f;  // Nouveau seuil minimal
    [SerializeField] private float animationSpeed = 10f;
    private Vector2 dragStartPosition;
    private bool isMenuVisible = false;
    private bool isAnimating = false;
    private Vector2 targetPosition;
    private Vector2 startPosition;
    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(true);
            menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, -menuPanel.rect.height);
        }
    }
    private void Update()
    {
        if (isAnimating)
        {
            float step = animationSpeed * Time.deltaTime;
            menuPanel.anchoredPosition = Vector2.Lerp(menuPanel.anchoredPosition, targetPosition, step);
            if (Vector2.Distance(menuPanel.anchoredPosition, targetPosition) < 1f)
            {
                menuPanel.anchoredPosition = targetPosition;
                isAnimating = false;
                if (!isMenuVisible)
                {
                    menuPanel.gameObject.SetActive(false);
                }
            }
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Vous pouvez ajouter un suivi en temps réel si souhaité
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        float swipeDistance = eventData.position.y - dragStartPosition.y;
        // Vérifie si le swipe dépasse la distance minimale
        if (Mathf.Abs(swipeDistance) >= minSwipeDistance)
        {
            // Swipe vers le haut
            if (swipeDistance > 0 && !isMenuVisible)
            {
                ShowMenu(eventData.position);
            }
            // Swipe vers le bas
            else if (swipeDistance < 0 && isMenuVisible)
            {
                HideMenu();
            }
        }
    }
    private void ShowMenu(Vector2 position)
    {
        menuPanel.gameObject.SetActive(true);
        // Convertit la position du swipe en position locale
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            menuPanel.parent as RectTransform,
            position,
            null,
            out localPoint
        );
        // Configure la position initiale et cible
        menuPanel.anchoredPosition = new Vector2(localPoint.x, -menuPanel.rect.height);
        targetPosition = new Vector2(localPoint.x, 0);
        isMenuVisible = true;
        isAnimating = true;
    }
    private void HideMenu()
    {
        targetPosition = new Vector2(menuPanel.anchoredPosition.x, -menuPanel.rect.height);
        isMenuVisible = false;
        isAnimating = true;
    }
}
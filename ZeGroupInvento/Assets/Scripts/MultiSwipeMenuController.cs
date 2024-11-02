using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class MultiDirectionalSwipeMenuController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPrefab;
    [SerializeField] private float minSwipeDistance = 20f;
    [SerializeField] private float animationSpeed = 10f;
    [SerializeField] private float menuSpacing = 50f;

    private List<MenuInstance> activeMenus = new List<MenuInstance>();
    private Vector2 dragStartPosition;
    private RectTransform swipeZone;
    private bool isNorthZone;

    private class MenuInstance
    {
        public GameObject menuObject;
        public RectTransform menuPanel;
        public bool isVisible;
        public bool isAnimating;
        public Vector2 targetPosition;
        public Vector2 startPosition;
        public float swipePosition;

        public MenuInstance(GameObject obj, float pos)
        {
            menuObject = obj;
            menuPanel = obj.GetComponent<RectTransform>();
            isVisible = false;
            isAnimating = false;
            swipePosition = pos;
        }
    }

    private void Start()
    {
        swipeZone = GetComponent<RectTransform>();
        // Détermine si c'est une zone nord en fonction du nom de l'objet
        isNorthZone = gameObject.name.ToLower().Contains("north");

        Debug.Log($"SwipeZone initialized as {(isNorthZone ? "North" : "South")} zone");

        if (menuPrefab != null)
        {
            RectTransform menuRect = menuPrefab.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.gameObject.SetActive(false);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
        Debug.Log($"Drag started in {(isNorthZone ? "North" : "South")} zone");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Optionnel : Ajouter un feedback visuel pendant le drag
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 swipeVector = eventData.position - dragStartPosition;

        Vector2 localStartPoint, localEndPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            swipeZone,
            dragStartPosition,
            null,
            out localStartPoint
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            swipeZone,
            eventData.position,
            null,
            out localEndPoint
        );

        if (swipeVector.magnitude >= minSwipeDistance)
        {
            float swipeVertical = localEndPoint.y - localStartPoint.y;

            if (isNorthZone)
            {
                // Zone Nord - Swipe inversé
                if (swipeVertical > minSwipeDistance)
                {
                    HideNearestMenu(localEndPoint.x);
                }
                else if (swipeVertical < -minSwipeDistance)
                {
                    ShowMenu(localEndPoint.x);
                }
            }
            else
            {
                // Zone Sud - Comportement normal
                if (swipeVertical > minSwipeDistance)
                {
                    ShowMenu(localEndPoint.x);
                }
                else if (swipeVertical < -minSwipeDistance)
                {
                    HideNearestMenu(localEndPoint.x);
                }
            }
        }
    }

    private void ShowMenu(float xPosition)
    {
        foreach (var menu in activeMenus)
        {
            if (Mathf.Abs(menu.swipePosition - xPosition) < menuSpacing)
            {
                return;
            }
        }

        GameObject newMenuObj = Instantiate(menuPrefab, transform);
        RectTransform menuRect = newMenuObj.GetComponent<RectTransform>();

        if (isNorthZone)
        {
            // Configuration pour la zone Nord
            menuRect.rotation = Quaternion.Euler(0, 0, 180f);
            menuRect.anchoredPosition = new Vector2(xPosition, menuRect.rect.height);
            newMenuObj.transform.SetParent(transform.parent); // Pour éviter les problèmes de rotation
        }
        else
        {
            // Configuration pour la zone Sud
            menuRect.anchoredPosition = new Vector2(xPosition, -menuRect.rect.height);
        }

        MenuInstance newMenu = new MenuInstance(newMenuObj, xPosition);
        newMenu.isVisible = true;
        newMenu.isAnimating = true;
        newMenu.startPosition = menuRect.anchoredPosition;
        newMenu.targetPosition = new Vector2(xPosition, isNorthZone ? menuRect.rect.height : 0);

        newMenuObj.SetActive(true);
        activeMenus.Add(newMenu);

        Debug.Log($"Menu shown in {(isNorthZone ? "North" : "South")} zone at position {xPosition}");
    }

    private void HideNearestMenu(float xPosition)
    {
        MenuInstance nearestMenu = null;
        float nearestDistance = float.MaxValue;

        foreach (var menu in activeMenus)
        {
            float distance = Mathf.Abs(menu.swipePosition - xPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMenu = menu;
            }
        }

        if (nearestMenu != null)
        {
            nearestMenu.isVisible = false;
            nearestMenu.isAnimating = true;

            float targetY = isNorthZone ?
                nearestMenu.menuPanel.rect.height * 2 :
                -nearestMenu.menuPanel.rect.height;

            nearestMenu.targetPosition = new Vector2(nearestMenu.swipePosition, targetY);

            Debug.Log($"Menu hidden in {(isNorthZone ? "North" : "South")} zone");
        }
    }

    private void Update()
    {
        foreach (var menu in activeMenus.ToArray())
        {
            if (menu.isAnimating)
            {
                float step = animationSpeed * Time.deltaTime;
                menu.menuPanel.anchoredPosition = Vector2.Lerp(
                    menu.menuPanel.anchoredPosition,
                    menu.targetPosition,
                    step
                );

                if (Vector2.Distance(menu.menuPanel.anchoredPosition, menu.targetPosition) < 1f)
                {
                    menu.menuPanel.anchoredPosition = menu.targetPosition;
                    menu.isAnimating = false;

                    if (!menu.isVisible)
                    {
                        Object.Destroy(menu.menuObject);
                        activeMenus.Remove(menu);
                    }
                }
            }
        }
    }
}
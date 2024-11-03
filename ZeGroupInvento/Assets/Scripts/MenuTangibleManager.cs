using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TangibleMenuManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject tangibleMenuGlobal;
    [SerializeField] private GameObject tangibleMenuSubMenu;
    [SerializeField] private GameObject tangibleMenuSubMenuFirstSelected;
    [SerializeField] private GameObject tangibleMenuSubMenuSecondSelected;

    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTimeThreshold = 0.3f;
    [SerializeField] private float tapDistanceThreshold = 50f;

    private GameObject currentActiveMenu;
    private Canvas parentCanvas;

    // Variables pour le système de détection de double tap
    private float lastTapTime;
    private Vector2 lastTapPosition;
    private bool readyForSecondTap;
    private bool isProcessingTouch;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("Canvas non trouvé!");
        }

        lastTapTime = -doubleTapTimeThreshold;
        readyForSecondTap = false;
    }

    private void Update()
    {
        // Réinitialiser si trop de temps s'est écoulé depuis le premier tap
        if (readyForSecondTap && Time.time - lastTapTime > doubleTapTimeThreshold)
        {
            readyForSecondTap = false;
        }

        // Traiter les touches
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    ProcessTap(touch.position);
                }
            }
        }
        // Support de la souris pour les tests
        else if (Input.GetMouseButtonDown(0))
        {
            ProcessTap(Input.mousePosition);
        }
    }

    private void ProcessTap(Vector2 tapPosition)
    {
        if (!readyForSecondTap)
        {
            // Premier tap
            lastTapTime = Time.time;
            lastTapPosition = tapPosition;
            readyForSecondTap = true;
        }
        else
        {
            // Second tap - vérifier si c'est un double tap valide
            float timeSinceLastTap = Time.time - lastTapTime;
            float distanceFromLastTap = Vector2.Distance(tapPosition, lastTapPosition);

            if (timeSinceLastTap <= doubleTapTimeThreshold && distanceFromLastTap <= tapDistanceThreshold)
            {
                // Double tap détecté
                ShowMenuGlobalAtPosition(tapPosition);
            }

            // Réinitialiser après le second tap
            readyForSecondTap = false;
        }
    }

    private void ShowMenuGlobalAtPosition(Vector2 screenPosition)
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
        }

        currentActiveMenu = Instantiate(tangibleMenuGlobal, transform);
        PositionMenuAtScreenPoint(currentActiveMenu, screenPosition);
    }

    private void PositionMenuAtScreenPoint(GameObject menu, Vector2 screenPoint)
    {
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localPoint
        );

        menuRect.anchoredPosition = localPoint;
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
    }

    public void ShowSubMenu()
    {
        if (currentActiveMenu != null)
        {
            Vector2 currentPos = currentActiveMenu.GetComponent<RectTransform>().anchoredPosition;
            SwitchMenu(tangibleMenuSubMenu, currentPos);
        }
    }

    public void ShowSubMenuFirstSelected()
    {
        if (currentActiveMenu != null)
        {
            Vector2 currentPos = currentActiveMenu.GetComponent<RectTransform>().anchoredPosition;
            SwitchMenu(tangibleMenuSubMenuFirstSelected, currentPos);
        }
    }

    public void ShowSubMenuSecondSelected()
    {
        if (currentActiveMenu != null)
        {
            Vector2 currentPos = currentActiveMenu.GetComponent<RectTransform>().anchoredPosition;
            SwitchMenu(tangibleMenuSubMenuSecondSelected, currentPos);
        }
    }

    private void SwitchMenu(GameObject newMenuPrefab, Vector2 position)
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
        }

        currentActiveMenu = Instantiate(newMenuPrefab, transform);
        RectTransform rectTransform = currentActiveMenu.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
    }

    public void HideMenu()
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
            currentActiveMenu = null;
        }

        // Réinitialiser l'état du double tap
        readyForSecondTap = false;
        lastTapTime = -doubleTapTimeThreshold;
    }
}
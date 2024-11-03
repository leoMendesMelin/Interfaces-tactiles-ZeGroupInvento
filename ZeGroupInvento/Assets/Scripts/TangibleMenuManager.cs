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
    private Vector2 menuPosition;

    // Variables pour le système de détection de double tap
    private float lastTapTime;
    private Vector2 lastTapPosition;
    private bool readyForSecondTap;
    private bool isProcessingTouch;

    // État actuel du menu
    private enum MenuState
    {
        Global,
        Guidelines,
        GVertical,
        GHorizontal
    }
    private MenuState currentState = MenuState.Global;

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

    public void OnHomeButtonClicked()
    {
        Debug.Log("Home button clicked");
        if (currentActiveMenu != null)
        {
            Vector2 currentPos = currentActiveMenu.GetComponent<RectTransform>().anchoredPosition;
            SwitchMenu(tangibleMenuGlobal, currentPos);
            currentState = MenuState.Global;
        }
    }

    private void SetupMenuButtons(GameObject menu)
    {
        if (menu == null) return;

        Debug.Log("Setting up buttons for menu: " + menu.name);
        Button[] buttons = menu.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            Debug.Log("Found button: " + button.gameObject.name);
            button.onClick.RemoveAllListeners();

            switch (button.gameObject.name)
            {
                case "Home":
                    Debug.Log("Setting up Home button");
                    button.onClick.AddListener(OnHomeButtonClicked);
                    break;
                case "Guidelines":
                    Debug.Log("Setting up Guidelines button");
                    button.onClick.AddListener(OnGuidelinesButtonClicked);
                    break;
                case "GVertical":
                    Debug.Log("Setting up GVertical button");
                    button.onClick.AddListener(OnGVerticalButtonClicked);
                    break;
                case "GHorizontal":
                    Debug.Log("Setting up GHorizontal button");
                    button.onClick.AddListener(OnGHorizontalButtonClicked);
                    break;
            }
        }
    }

    public void OnGuidelinesButtonClicked()
    {
        if (currentState == MenuState.Global)
        {
            ShowSubMenu();
            currentState = MenuState.Guidelines;
        }
    }

    public void OnGVerticalButtonClicked()
    {
        Debug.Log("GVertical clicked, current state: " + currentState);
        if (currentState == MenuState.Guidelines)
        {
            ShowSubMenuFirstSelected();
            currentState = MenuState.GVertical;
        }
        else if (currentState == MenuState.GHorizontal) // Ajout de cette condition
        {
            ShowSubMenuFirstSelected();
            currentState = MenuState.GVertical;
        }
    }

    public void OnGHorizontalButtonClicked()
    {
        Debug.Log("GHorizontal clicked, current state: " + currentState);
        if (currentState == MenuState.Guidelines)
        {
            ShowSubMenuSecondSelected();
            currentState = MenuState.GHorizontal;
        }
        else if (currentState == MenuState.GVertical) // Ajout de cette condition
        {
            ShowSubMenuSecondSelected();
            currentState = MenuState.GHorizontal;
        }
    }

    private void ProcessTap(Vector2 tapPosition)
    {
        if (!readyForSecondTap)
        {
            lastTapTime = Time.time;
            lastTapPosition = tapPosition;
            readyForSecondTap = true;
        }
        else
        {
            float timeSinceLastTap = Time.time - lastTapTime;
            float distanceFromLastTap = Vector2.Distance(tapPosition, lastTapPosition);

            if (timeSinceLastTap <= doubleTapTimeThreshold && distanceFromLastTap <= tapDistanceThreshold)
            {
                if (currentActiveMenu != null && IsPositionOverMenu(tapPosition))
                {
                    HideMenu();
                }
                else
                {
                    ShowMenuGlobalAtPosition(tapPosition);
                    currentState = MenuState.Global; // Réinitialiser l'état
                }
            }

            readyForSecondTap = false;
        }
    }


    private bool IsPositionOverMenu(Vector2 screenPosition)
    {
        if (currentActiveMenu == null) return false;

        RectTransform menuRect = currentActiveMenu.GetComponent<RectTransform>();
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            menuRect,
            screenPosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localPoint))
        {
            // Convertir le point local en coordonnées relatives à la RectTransform
            Vector2 normalizedPoint = new Vector2(
                (localPoint.x + menuRect.rect.width * 0.5f) / menuRect.rect.width,
                (localPoint.y + menuRect.rect.height * 0.5f) / menuRect.rect.height
            );

            return normalizedPoint.x >= 0 && normalizedPoint.x <= 1 &&
                   normalizedPoint.y >= 0 && normalizedPoint.y <= 1;
        }

        return false;
    }

    private void ShowMenuGlobalAtPosition(Vector2 screenPosition)
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
        }

        Debug.Log("Creating new menu at position: " + screenPosition);
        currentActiveMenu = Instantiate(tangibleMenuGlobal, transform);
        menuPosition = screenPosition;
        PositionMenuAtScreenPoint(currentActiveMenu, screenPosition);

        // Configure les boutons immédiatement après la création
        SetupMenuButtons(currentActiveMenu);
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
        Debug.Log("ShowSubMenu called");
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

        Debug.Log("Switching to menu: " + newMenuPrefab.name);
        currentActiveMenu = Instantiate(newMenuPrefab, transform);
        RectTransform rectTransform = currentActiveMenu.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;

        // Configure les boutons immédiatement après la création
        SetupMenuButtons(currentActiveMenu);
    }


    public void HideMenu()
    {
        if (currentActiveMenu != null)
        {
            Destroy(currentActiveMenu);
            currentActiveMenu = null;
        }

        // Réinitialiser l'état du menu
        currentState = MenuState.Global;

        // Réinitialiser l'état du double tap
        readyForSecondTap = false;
        lastTapTime = -doubleTapTimeThreshold;
    }
}
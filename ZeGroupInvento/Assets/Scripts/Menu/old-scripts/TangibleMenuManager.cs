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
    [SerializeField] private GameObject gVerticalPrefab;
    [SerializeField] private GameObject gHorizontalPrefab;

    [Header("Triple Tap Settings")]
    [SerializeField] private float tripleTapTimeThreshold = 0.3f;
    [SerializeField] private float tapDistanceThreshold = 50f;

    private GameObject currentActiveMenu;
    private Canvas parentCanvas;
    private Vector2 menuPosition;

    // Variables pour le système de détection de triple tap
    private float[] lastTapTimes = new float[2]; // Stocke les temps des 2 derniers taps
    private Vector2[] lastTapPositions = new Vector2[2]; // Stocke les positions des 2 derniers taps
    private int tapCount = 0;
    private bool isProcessingTouch;
    private GameObject currentGuidelinePrefab;

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

        // Initialiser les tableaux de temps et positions
        for (int i = 0; i < 2; i++)
        {
            lastTapTimes[i] = -tripleTapTimeThreshold;
            lastTapPositions[i] = Vector2.zero;
        }
    }




    private void Update()
    {
        // Réinitialiser si trop de temps s'est écoulé depuis le premier tap
        if (tapCount > 0 && Time.time - lastTapTimes[0] > tripleTapTimeThreshold)
        {
            tapCount = 0;
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
            // Afficher le menu FirstSelected
            ShowSubMenuFirstSelected();
            // Afficher le prefab GVertical au centre
            ShowGuidelineAtCenter(gVerticalPrefab);
            currentState = MenuState.GVertical;
        }
        else if (currentState == MenuState.GHorizontal)
        {
            ShowSubMenuFirstSelected();
            ShowGuidelineAtCenter(gVerticalPrefab);
            currentState = MenuState.GVertical;
        }
    }

    public void OnGHorizontalButtonClicked()
    {
        Debug.Log("GHorizontal clicked, current state: " + currentState);
        if (currentState == MenuState.Guidelines)
        {
            // Afficher le menu SecondSelected
            ShowSubMenuSecondSelected();
            // Afficher le prefab GHorizontal au centre
            ShowGuidelineAtCenter(gHorizontalPrefab);
            currentState = MenuState.GHorizontal;
        }
        else if (currentState == MenuState.GVertical)
        {
            ShowSubMenuSecondSelected();
            ShowGuidelineAtCenter(gHorizontalPrefab);
            currentState = MenuState.GHorizontal;
        }
    }

    private void ShowGuidelineAtCenter(GameObject prefab)
    {
        // Détruire l'ancien prefab s'il existe
        if (currentGuidelinePrefab != null)
        {
            Destroy(currentGuidelinePrefab);
        }

        // Créer le nouveau prefab au centre de l'écran
        currentGuidelinePrefab = Instantiate(prefab, transform);
        RectTransform rectTransform = currentGuidelinePrefab.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero; // Position au centre
    }

    private void ProcessTap(Vector2 tapPosition)
    {
        float currentTime = Time.time;

        // Si c'est le premier tap ou si le tap précédent est trop ancien
        if (tapCount == 0 || currentTime - lastTapTimes[0] > tripleTapTimeThreshold)
        {
            tapCount = 1;
            lastTapTimes[0] = currentTime;
            lastTapPositions[0] = tapPosition;
        }
        // Deuxième tap
        else if (tapCount == 1)
        {
            float distanceFromLastTap = Vector2.Distance(tapPosition, lastTapPositions[0]);

            if (distanceFromLastTap <= tapDistanceThreshold)
            {
                tapCount = 2;
                lastTapTimes[1] = currentTime;
                lastTapPositions[1] = tapPosition;
            }
            else
            {
                tapCount = 1;
                lastTapTimes[0] = currentTime;
                lastTapPositions[0] = tapPosition;
            }
        }
        // Troisième tap
        else if (tapCount == 2)
        {
            float distanceFromLastTap = Vector2.Distance(tapPosition, lastTapPositions[1]);
            float distanceFromFirstTap = Vector2.Distance(tapPosition, lastTapPositions[0]);

            if (distanceFromLastTap <= tapDistanceThreshold && distanceFromFirstTap <= tapDistanceThreshold)
            {
                // Triple tap détecté
                if (currentActiveMenu != null && IsPositionOverMenu(tapPosition))
                {
                    HideMenu();
                }
                else
                {
                    ShowMenuGlobalAtPosition(tapPosition);
                    currentState = MenuState.Global;
                }
            }

            tapCount = 0; // Réinitialiser après le traitement
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

        if (currentGuidelinePrefab != null)
        {
            Destroy(currentGuidelinePrefab);
            currentGuidelinePrefab = null;
        }

        // Réinitialiser l'état et les variables du triple tap
        currentState = MenuState.Global;
        tapCount = 0;

        // Réinitialiser les tableaux de temps et positions
        for (int i = 0; i < 2; i++)
        {
            lastTapTimes[i] = -tripleTapTimeThreshold;
            lastTapPositions[i] = Vector2.zero;
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SwipeableZonePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Panels")]
    public GameObject createZonePanel;
    public GameObject assignWaiterPanel;
    public GameObject notificationsPanel;
    public GameObject tablesPanel;

    [Header("Buttons")]
    public Button createZoneButton;
    public Button assignWaiterButton;
    public Button notificationsButton;
    public Button tablesButton;

    [Header("Position Settings")]
    public float offsetY = 250f;
    public float swipeThreshold = 50f;
    public float animationDuration = 0.3f;
    public bool isInverted = false;

    private RectTransform rectTransform;
    private Vector2 dragStartPos;
    private Vector2 originalPosition;
    private bool isOpen = false;
    private Coroutine animationCoroutine;
    private Color defaultButtonColor;
    private Color selectedButtonColor = new Color(0.2f, 0.6f, 1f);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultButtonColor = createZoneButton.GetComponent<Image>().color;

        // Sauvegarder la position originale (position ouverte)
        originalPosition = rectTransform.anchoredPosition;

        // Définir l'état fermé
        isOpen = false;

        // Setup initial
        createZonePanel.SetActive(true);
        assignWaiterPanel.SetActive(false);
        notificationsPanel.SetActive(false);
        tablesPanel.SetActive(false);

        // Setup des boutons
        createZoneButton.onClick.AddListener(() => SwitchPanel(createZonePanel, createZoneButton));
        assignWaiterButton.onClick.AddListener(() => SwitchPanel(assignWaiterPanel, assignWaiterButton));
        notificationsButton.onClick.AddListener(() => SwitchPanel(notificationsPanel, notificationsButton));
        tablesButton.onClick.AddListener(() => SwitchPanel(tablesPanel, tablesButton));

        SetButtonSelected(createZoneButton);

        // Forcer la fermeture du panel
        ClosePanel();
    }

    private void SwitchPanel(GameObject targetPanel, Button targetButton)
    {
        // Activer uniquement le panel ciblé
        createZonePanel.SetActive(targetPanel == createZonePanel);
        assignWaiterPanel.SetActive(targetPanel == assignWaiterPanel);
        notificationsPanel.SetActive(targetPanel == notificationsPanel);
        tablesPanel.SetActive(targetPanel == tablesPanel);

        // Mettre à jour les couleurs des boutons
        ResetButtonColors();
        SetButtonSelected(targetButton);

        // Ne pas ouvrir automatiquement - garder l'état fermé
        if (isOpen)
        {
            ClosePanel(); // Si le menu était ouvert, le fermer
        }
    }

    private void SetButtonSelected(Button button)
    {
        button.GetComponent<Image>().color = selectedButtonColor;
    }

    private void ResetButtonColors()
    {
        createZoneButton.GetComponent<Image>().color = defaultButtonColor;
        assignWaiterButton.GetComponent<Image>().color = defaultButtonColor;
        notificationsButton.GetComponent<Image>().color = defaultButtonColor;
        tablesButton.GetComponent<Image>().color = defaultButtonColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float dragDelta = eventData.position.y - dragStartPos.y;
        Vector2 targetPos;

        if (isInverted)
        {
            if (isOpen)
            {
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta, 0, offsetY));
            }
            else
            {
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta + offsetY, 0, offsetY));
            }
        }
        else
        {
            if (isOpen)
            {
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta, -offsetY, 0));
            }
            else
            {
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta - offsetY, -offsetY, 0));
            }
        }

        rectTransform.anchoredPosition = targetPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float dragDelta = eventData.position.y - dragStartPos.y;

        if (Mathf.Abs(dragDelta) > swipeThreshold)
        {
            if (isInverted)
            {
                // Pour le menu inversé
                // Si on swipe vers le haut et que le menu est ouvert -> fermer
                // Si on swipe vers le bas et que le menu est fermé -> ouvrir
                if (dragDelta > 0 && !isOpen)  // Swipe vers le bas quand fermé
                {
                    OpenPanel();
                }
                else if (dragDelta < 0 && isOpen)  // Swipe vers le haut quand ouvert
                {
                    ClosePanel();
                }
            }
            else
            {
                // Logique normale (inchangée)
                if (dragDelta > 0 && !isOpen)  // Swipe vers le haut quand fermé
                {
                    OpenPanel();
                }
                else if (dragDelta < 0 && isOpen)  // Swipe vers le bas quand ouvert
                {
                    ClosePanel();
                }
            }
        }
        else
        {
            // Retour à la position précédente
            if (isOpen)
                OpenPanel();
            else
                ClosePanel();
        }
    }

    private void OpenPanel()
    {
        isOpen = true;
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateToPosition(originalPosition));
    }

    public void ClosePanel()
    {
        isOpen = false;
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        float actualOffset = isInverted ? offsetY : -offsetY;
        animationCoroutine = StartCoroutine(AnimateToPosition(originalPosition + new Vector2(0, actualOffset)));
    }

    private IEnumerator AnimateToPosition(Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
}
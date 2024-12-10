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

        // Sauvegarder la position originale
        originalPosition = rectTransform.anchoredPosition;

        // Pour le mode inversé, on commence par la position ouverte
        if (isInverted)
        {
            rectTransform.anchoredPosition = originalPosition + new Vector2(0, offsetY);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition + new Vector2(0, -offsetY);
        }

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
    }

    private void SwitchPanel(GameObject targetPanel, Button targetButton)
    {
        createZonePanel.SetActive(targetPanel == createZonePanel);
        assignWaiterPanel.SetActive(targetPanel == assignWaiterPanel);
        notificationsPanel.SetActive(targetPanel == notificationsPanel);
        tablesPanel.SetActive(targetPanel == tablesPanel);

        ResetButtonColors();
        SetButtonSelected(targetButton);

        if (!isOpen)
        {
            OpenPanel();
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
                // Si ouvert, permet de descendre jusqu'à la position avec offset positif
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta + offsetY, 0, offsetY));
            }
            else
            {
                // Si fermé, permet de descendre de la position avec offset positif
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta + offsetY, 0, offsetY));
            }
        }
        else
        {
            if (isOpen)
            {
                // Si ouvert, permet de descendre jusqu'à la position avec offset négatif
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta, -offsetY, 0));
            }
            else
            {
                // Si fermé, permet de monter à partir de la position avec offset négatif
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
                // En mode inversé, swipe down pour ouvrir, swipe up pour fermer
                if (dragDelta > 0 && !isOpen)
                {
                    ClosePanel(); // Ferme quand on swipe vers le bas
                }
                else if (dragDelta < 0 && isOpen)
                {
                    OpenPanel(); // Ouvre quand on swipe vers le haut
                }
            }
            else
            {
                // Mode normal
                if (dragDelta > 0 && !isOpen)
                {
                    OpenPanel();
                }
                else if (dragDelta < 0 && isOpen)
                {
                    ClosePanel();
                }
            }
        }
        else
        {
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

        Vector2 targetPos = isInverted ?
            originalPosition + new Vector2(0, 0) :  // Pour le mode inversé, position originale
            originalPosition;                       // Pour le mode normal, position originale

        animationCoroutine = StartCoroutine(AnimateToPosition(targetPos));
    }

    public void ClosePanel()
    {
        isOpen = false;
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        Vector2 targetPos = isInverted ?
            originalPosition + new Vector2(0, offsetY) :     // Pour le mode inversé, monter
            originalPosition + new Vector2(0, -offsetY);     // Pour le mode normal, descendre

        animationCoroutine = StartCoroutine(AnimateToPosition(targetPos));
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
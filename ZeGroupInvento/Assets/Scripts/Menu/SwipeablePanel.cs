using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MenuPanelManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Panels")]
    public GameObject elementsPanel;
    public GameObject tablesPanel;
    public GameObject settingsPanel;

    [Header("Buttons")]
    public Button elementsButton;
    public Button tablesButton;
    public Button settingsButton;

    [Header("Position Settings")]
    public float offsetY = 250f;
    public float swipeThreshold = 50f;
    public float animationDuration = 0.3f;
    public bool isInverted = false; // Nouvelle variable pour indiquer si le menu est inversé


    private RectTransform rectTransform;
    private Vector2 dragStartPos;
    private Vector2 originalPosition; // Position d'origine du menu
    private bool isOpen = false;
    private Coroutine animationCoroutine;
    private Color defaultButtonColor;
    private Color selectedButtonColor = new Color(0.2f, 0.6f, 1f);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultButtonColor = elementsButton.GetComponent<Image>().color;

        // Sauvegarder la position originale
        originalPosition = rectTransform.anchoredPosition;
        float actualOffset = isInverted ? offsetY : -offsetY;
        rectTransform.anchoredPosition = originalPosition + new Vector2(0, actualOffset);


  

        // Setup initial
        elementsPanel.SetActive(true);
        tablesPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Setup des boutons
        elementsButton.onClick.AddListener(() => SwitchPanel(elementsPanel, elementsButton));
        tablesButton.onClick.AddListener(() => SwitchPanel(tablesPanel, tablesButton));
        settingsButton.onClick.AddListener(() => SwitchPanel(settingsPanel, settingsButton));

        SetButtonSelected(elementsButton);
    }

    private void SwitchPanel(GameObject targetPanel, Button targetButton)
    {
        // Activer uniquement le panel ciblé
        elementsPanel.SetActive(targetPanel == elementsPanel);
        tablesPanel.SetActive(targetPanel == tablesPanel);
        settingsPanel.SetActive(targetPanel == settingsPanel);

        // Mettre à jour les couleurs des boutons
        ResetButtonColors();
        SetButtonSelected(targetButton);

        // Si le menu est fermé, l'ouvrir
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
        elementsButton.GetComponent<Image>().color = defaultButtonColor;
        tablesButton.GetComponent<Image>().color = defaultButtonColor;
        settingsButton.GetComponent<Image>().color = defaultButtonColor;
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

        // Inverser la logique selon l'orientation
        if (isInverted)
        {
            if (isOpen)
            {
                // Si ouvert, permet de monter jusqu'à la position avec offset
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta, 0, offsetY));
            }
            else
            {
                // Si fermé, permet de descendre jusqu'à la position originale
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta + offsetY, 0, offsetY));
            }
        }
        else
        {
            if (isOpen)
            {
                // Si ouvert, permet de descendre jusqu'à la position avec offset
                targetPos = originalPosition + new Vector2(0, Mathf.Clamp(dragDelta, -offsetY, 0));
            }
            else
            {
                // Si fermé, permet de monter jusqu'à la position originale
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
                // Logique inversée
                if (dragDelta < 0 && !isOpen)
                {
                    OpenPanel();
                }
                else if (dragDelta > 0 && isOpen)
                {
                    ClosePanel();
                }
            }
            else
            {
                // Logique normale
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

        // Appliquer l'offset dans la bonne direction
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

    private IEnumerator AnimatePanelSize(float targetSize)
    {
        float startSize = rectTransform.sizeDelta.y;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float newSize = Mathf.Lerp(startSize, targetSize, t);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newSize);
            yield return null;
        }

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, targetSize);

        // Lorsque le menu est fermé, on garde le panel actif mais on ajuste sa taille
        if (!isOpen)
        {
            // On ne change pas l'état des panels ici, on garde celui qui est actif
            // mais on met à jour la taille uniquement
        }
    }

    private void ShowPanel(GameObject panel)
    {
        panel.SetActive(true);
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WaiterDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private WaiterUI waiterUI;
    private Transform originalParent;
    private GameObject draggedInstance;
    private Vector3 originalScale;
    private GridLayoutGroup originalLayoutGroup;
    private ScrollRect parentScrollRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        waiterUI = GetComponent<WaiterUI>();

        // Trouver le ScrollRect parent s'il existe
        parentScrollRect = GetComponentInParent<ScrollRect>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    // Cette m�thode est appel�e avant OnBeginDrag
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // D�sactiver le scroll du parent pendant le drag du waiter
        if (parentScrollRect != null)
        {
            eventData.useDragThreshold = false;
            parentScrollRect.enabled = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Arr�ter la propagation de l'�v�nement aux parents
        eventData.Use();

        // Stocker l'�chelle et le parent originaux
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalLayoutGroup = GetComponentInParent<GridLayoutGroup>();

        // Cr�er une copie pour le drag qui sera au niveau du canvas
        draggedInstance = Instantiate(gameObject, canvas.transform);
        Destroy(draggedInstance.GetComponent<WaiterDragHandler>()); // �viter les conflits de drag

        // Configurer l'instance de drag
        var draggedRect = draggedInstance.GetComponent<RectTransform>();
        draggedRect.position = rectTransform.position;
        draggedInstance.GetComponent<CanvasGroup>().alpha = 0.6f;

        // D�sactiver le raycast sur l'original
        canvasGroup.blocksRaycasts = false;

        // Cacher temporairement l'original
        canvasGroup.alpha = 0.3f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Arr�ter la propagation de l'�v�nement aux parents
        eventData.Use();

        if (draggedInstance != null)
        {
            RectTransform draggedRect = draggedInstance.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            draggedRect.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // R�activer le scroll du parent
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = true;
        }

        // R�activer le raycast et la visibilit�
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool validDrop = false;

        // V�rifier si on a touch� une zone
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var zoneController = eventData.pointerCurrentRaycast.gameObject.GetComponent<ZoneControllerPrefab>();
            if (zoneController != null)
            {
                // Trouver le GridAssigned
                var gridAssigned = GameObject.Find("GridAssigned")?.transform;
                if (gridAssigned != null)
                {
                    // D�placer vers GridAssigned
                    transform.SetParent(gridAssigned);

                    // Mettre � jour la couleur avec celle de la zone
                    var zoneImage = zoneController.GetComponent<Image>();
                    var waiterImage = GetComponent<Image>();
                    if (zoneImage != null && waiterImage != null)
                    {
                        Color zoneColor = zoneImage.color;
                        waiterImage.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                    }

                    // Ajouter le waiter � la zone
                    zoneController.AssignWaiter(waiterUI.GetWaiterData());

                    validDrop = true;
                }
            }
        }

        if (!validDrop)
        {
            // Retourner � la position d'origine
            transform.SetParent(originalParent);
            transform.localScale = originalScale;
        }

        // Forcer la mise � jour du layout
        if (originalLayoutGroup != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalLayoutGroup.GetComponent<RectTransform>());
        }

        // D�truire l'instance de drag
        if (draggedInstance != null)
        {
            Destroy(draggedInstance);
        }
    }
}
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

    // Cette méthode est appelée avant OnBeginDrag
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // Désactiver le scroll du parent pendant le drag du waiter
        if (parentScrollRect != null)
        {
            eventData.useDragThreshold = false;
            parentScrollRect.enabled = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Arrêter la propagation de l'événement aux parents
        eventData.Use();

        // Stocker l'échelle et le parent originaux
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalLayoutGroup = GetComponentInParent<GridLayoutGroup>();

        // Créer une copie pour le drag qui sera au niveau du canvas
        draggedInstance = Instantiate(gameObject, canvas.transform);
        Destroy(draggedInstance.GetComponent<WaiterDragHandler>()); // Éviter les conflits de drag

        // Configurer l'instance de drag
        var draggedRect = draggedInstance.GetComponent<RectTransform>();
        draggedRect.position = rectTransform.position;
        draggedInstance.GetComponent<CanvasGroup>().alpha = 0.6f;

        // Désactiver le raycast sur l'original
        canvasGroup.blocksRaycasts = false;

        // Cacher temporairement l'original
        canvasGroup.alpha = 0.3f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Arrêter la propagation de l'événement aux parents
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
        // Réactiver le scroll du parent
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = true;
        }

        // Réactiver le raycast et la visibilité
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        bool validDrop = false;

        // Vérifier si on a touché une zone
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var zoneController = eventData.pointerCurrentRaycast.gameObject.GetComponent<ZoneControllerPrefab>();
            if (zoneController != null)
            {
                // Trouver le GridAssigned
                var gridAssigned = GameObject.Find("GridAssigned")?.transform;
                if (gridAssigned != null)
                {
                    // Déplacer vers GridAssigned
                    transform.SetParent(gridAssigned);

                    // Mettre à jour la couleur avec celle de la zone
                    var zoneImage = zoneController.GetComponent<Image>();
                    var waiterImage = GetComponent<Image>();
                    if (zoneImage != null && waiterImage != null)
                    {
                        Color zoneColor = zoneImage.color;
                        waiterImage.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                    }

                    // Ajouter le waiter à la zone
                    zoneController.AssignWaiter(waiterUI.GetWaiterData());

                    validDrop = true;
                }
            }
        }

        if (!validDrop)
        {
            // Retourner à la position d'origine
            transform.SetParent(originalParent);
            transform.localScale = originalScale;
        }

        // Forcer la mise à jour du layout
        if (originalLayoutGroup != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalLayoutGroup.GetComponent<RectTransform>());
        }

        // Détruire l'instance de drag
        if (draggedInstance != null)
        {
            Destroy(draggedInstance);
        }
    }
}
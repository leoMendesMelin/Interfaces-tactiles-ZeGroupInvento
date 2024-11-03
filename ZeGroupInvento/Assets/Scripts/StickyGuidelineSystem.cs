using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class StickyGuidelineSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float snapThreshold = 30f;
    [SerializeField] private Color stickyColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color previewColor = new Color(0, 1, 0, 0.3f); // Couleur plus légère pour le preview

    private Canvas parentCanvas;
    private Dictionary<GameObject, HashSet<GameObject>> attachedObjects = new Dictionary<GameObject, HashSet<GameObject>>();
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private GameObject draggedObject;
    private RectTransform draggedRect;
    private Vector2 dragStartPosition;
    private Vector2 dragOffset;
    private Camera canvasCamera;
    private GameObject currentSnapGuide = null;
    private bool isSnapping = false;

    private float lastSnapCheckTime = 0f;
    private const float SnapCheckInterval = 0.05f; // Vérifie toutes les 50ms au lieu de chaque frame
    private GameObject lastNearestGuideline = null;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (!parentCanvas)
        {
            Debug.LogError("StickyGuidelineSystem: Canvas not found!");
            enabled = false;
            return;
        }

        canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            HandleInput(touch.position, touch.phase);
        }
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            HandleInput(Input.mousePosition,
                Input.GetMouseButtonDown(0) ? TouchPhase.Began :
                Input.GetMouseButtonUp(0) ? TouchPhase.Ended :
                TouchPhase.Moved);
        }
    }


    private void HandleInput(Vector2 position, TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                BeginDrag(position);
                break;
            case TouchPhase.Moved:
                if (draggedObject != null)
                {
                    if (draggedObject.CompareTag("Draggable"))
                    {
                        HandleDraggableMovement(position);
                    }
                    else
                    {
                        // Pour les guidelines
                        DragObject(position);
                    }
                }
                break;
            case TouchPhase.Ended:
                FinalizeSnapping();
                EndDrag();
                break;
        }
    }

    private GameObject FindNearestGuideline(Vector2 position, out bool shouldSnap, out Vector2 snappedPosition)
    {
        shouldSnap = false;
        snappedPosition = position;
        GameObject nearestGuideline = null;
        float nearestDistance = float.MaxValue;

        var guidelines = FindObjectsOfType<RectTransform>()
            .Where(rt => rt.gameObject != null &&
                   (rt.gameObject.name.Contains("GVertical") || rt.gameObject.name.Contains("GHorizontal")));

        foreach (var guidelineRect in guidelines)
        {
            if (!guidelineRect) continue;

            bool isVertical = guidelineRect.gameObject.name.Contains("GVertical");
            float distance = isVertical ?
                Mathf.Abs(position.x - guidelineRect.anchoredPosition.x) :
                Mathf.Abs(position.y - guidelineRect.anchoredPosition.y);

            if (distance < snapThreshold && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGuideline = guidelineRect.gameObject;

                Vector2 snapped = position;
                if (isVertical)
                {
                    snapped.x = guidelineRect.anchoredPosition.x;
                }
                else
                {
                    snapped.y = guidelineRect.anchoredPosition.y;
                }
                snappedPosition = snapped;
                shouldSnap = true;
            }
        }

        return nearestGuideline;
    }


    private void HandleDraggableMovement(Vector2 screenPosition)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPosition,
            canvasCamera,
            out localPoint))
        {
            Vector2 newPosition = localPoint + dragOffset;

            // Vérifie le snap seulement si suffisamment de temps s'est écoulé
            bool shouldCheckSnap = Time.time - lastSnapCheckTime >= SnapCheckInterval;
            GameObject nearestGuideline = null;
            bool shouldSnap = false;
            Vector2 snappedPosition = newPosition;

            if (shouldCheckSnap)
            {
                nearestGuideline = FindNearestGuideline(newPosition, out shouldSnap, out snappedPosition);
                lastSnapCheckTime = Time.time;
                lastNearestGuideline = nearestGuideline;
            }
            else
            {
                // Utilise le dernier résultat connu
                nearestGuideline = lastNearestGuideline;
                if (nearestGuideline != null)
                {
                    bool isVertical = nearestGuideline.name.Contains("GVertical");
                    float distance = isVertical ?
                        Mathf.Abs(newPosition.x - nearestGuideline.GetComponent<RectTransform>().anchoredPosition.x) :
                        Mathf.Abs(newPosition.y - nearestGuideline.GetComponent<RectTransform>().anchoredPosition.y);

                    shouldSnap = distance < snapThreshold;
                    if (shouldSnap)
                    {
                        snappedPosition = newPosition;
                        if (isVertical)
                            snappedPosition.x = nearestGuideline.GetComponent<RectTransform>().anchoredPosition.x;
                        else
                            snappedPosition.y = nearestGuideline.GetComponent<RectTransform>().anchoredPosition.y;
                    }
                }
            }

            // Si on ne déplace pas un objet déjà attaché
            if (!IsAttached(draggedObject))
            {
                bool isNearGuideline = nearestGuideline != null && shouldSnap;

                if (isNearGuideline && nearestGuideline != currentSnapGuide)
                {
                    // Reset l'ancienne guideline si elle existe
                    if (currentSnapGuide != null && !HasAttachedObjects(currentSnapGuide))
                    {
                        var oldGuideImage = currentSnapGuide.GetComponent<Image>();
                        if (oldGuideImage && originalColors.ContainsKey(currentSnapGuide))
                        {
                            oldGuideImage.color = originalColors[currentSnapGuide];
                        }
                    }

                    // Applique le preview seulement si on est vraiment proche
                    var draggedImage = draggedObject.GetComponent<Image>();
                    var guideImage = nearestGuideline.GetComponent<Image>();

                    if (draggedImage)
                    {
                        if (!originalColors.ContainsKey(draggedObject))
                            originalColors[draggedObject] = draggedImage.color;
                        draggedImage.color = previewColor;
                    }

                    if (guideImage && !HasAttachedObjects(nearestGuideline))
                    {
                        if (!originalColors.ContainsKey(nearestGuideline))
                            originalColors[nearestGuideline] = guideImage.color;
                        guideImage.color = previewColor;
                    }

                    currentSnapGuide = nearestGuideline;
                    isSnapping = true;
                }
                else if (!isNearGuideline && currentSnapGuide != null)
                {
                    // Reset les couleurs quand on s'éloigne
                    var draggedImage = draggedObject.GetComponent<Image>();
                    if (draggedImage && originalColors.ContainsKey(draggedObject))
                    {
                        draggedImage.color = originalColors[draggedObject];
                    }

                    if (!HasAttachedObjects(currentSnapGuide))
                    {
                        var guideImage = currentSnapGuide.GetComponent<Image>();
                        if (guideImage && originalColors.ContainsKey(currentSnapGuide))
                        {
                            guideImage.color = originalColors[currentSnapGuide];
                        }
                    }

                    currentSnapGuide = null;
                    isSnapping = false;
                }
            }

            // Met à jour la position
            draggedRect.anchoredPosition = shouldSnap ? snappedPosition : newPosition;
        }
    }

    private bool HasAttachedObjects(GameObject guideline)
    {
        return attachedObjects.ContainsKey(guideline) && attachedObjects[guideline].Count > 0;
    }

    private void ResetObjectToOriginalColor(GameObject obj)
    {
        if (!obj) return;
        var image = obj.GetComponent<Image>();
        if (!image) return;

        // Si l'objet est attaché, garde la couleur sticky
        if (IsAttached(obj))
        {
            image.color = stickyColor;
        }
        // Sinon remet la couleur d'origine
        else if (originalColors.ContainsKey(obj))
        {
            image.color = originalColors[obj];
        }
    }


    private void ApplyPreviewColor(GameObject obj)
    {
        if (!obj) return;
        var image = obj.GetComponent<Image>();
        if (!image) return;

        // Sauvegarde la couleur originale si pas déjà fait
        if (!originalColors.ContainsKey(obj))
        {
            originalColors[obj] = image.color;
        }

        // Apply preview color
        image.color = previewColor;
    }

    private bool IsAttached(GameObject obj)
    {
        return attachedObjects.Any(kvp => kvp.Value.Contains(obj) || kvp.Key == obj);
    }



    private void FinalizeSnapping()
    {
        if (draggedObject != null)
        {
            if (currentSnapGuide != null && isSnapping)
            {
                AttachToGuideline(draggedObject, currentSnapGuide);

                // Applique la couleur sticky seulement lors de l'attachement
                var draggedImage = draggedObject.GetComponent<Image>();
                var guideImage = currentSnapGuide.GetComponent<Image>();

                if (draggedImage) draggedImage.color = stickyColor;
                if (guideImage) guideImage.color = stickyColor;
            }
            else
            {
                DetachFromAllGuidelines(draggedObject);
            }
        }
    }

    private void ApplyStickyColor(GameObject obj)
    {
        if (!obj) return;
        var image = obj.GetComponent<Image>();
        if (!image) return;

        if (!originalColors.ContainsKey(obj))
        {
            originalColors[obj] = image.color;
        }

        image.color = stickyColor;
    }



    private void BeginDrag(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GameObject hitObject = result.gameObject;

            if (hitObject.name.Contains("GVertical") || hitObject.name.Contains("GHorizontal") ||
                hitObject.CompareTag("Draggable"))
            {
                draggedObject = hitObject;
                draggedRect = hitObject.GetComponent<RectTransform>();
                dragStartPosition = draggedRect.anchoredPosition;

                // Sauvegarde la couleur originale si nécessaire
                var image = hitObject.GetComponent<Image>();
                if (image && !originalColors.ContainsKey(hitObject))
                {
                    originalColors[hitObject] = image.color;
                }

                // Si c'est une guideline, s'assure qu'elle garde la bonne couleur
                if (hitObject.name.Contains("GVertical") || hitObject.name.Contains("GHorizontal"))
                {
                    UpdateGuidelineColor(hitObject, HasAttachedObjects(hitObject));
                }

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    screenPosition,
                    canvasCamera,
                    out localPoint
                );

                dragOffset = dragStartPosition - localPoint;

                // Si c'est un objet draggable
                if (hitObject.CompareTag("Draggable"))
                {
                    var objImage = image;
                    if (objImage && !IsAttached(hitObject))
                    {
                        objImage.color = originalColors[hitObject];
                    }
                }
                break;
            }
        }
    }



    private void ResetVisualFeedback(GameObject obj)
    {
        if (!obj) return;

        var image = obj.GetComponent<Image>();
        if (image)
        {
            // Vérifie si l'objet est actuellement attaché à une guideline
            bool isAttachedToAnyGuideline = attachedObjects.Any(kvp =>
                kvp.Value.Contains(obj) || kvp.Key == obj);

            // Si l'objet est attaché, garde la couleur sticky, sinon remet la couleur d'origine
            if (isAttachedToAnyGuideline)
            {
                image.color = stickyColor;
            }
            else if (originalColors.ContainsKey(obj))
            {
                image.color = originalColors[obj];
            }
        }
    }


    private void DragObject(Vector2 screenPosition)
    {
        if (!draggedObject || !draggedRect) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPosition,
            canvasCamera,
            out localPoint))
        {
            Vector2 newPosition = localPoint + dragOffset;

            if (draggedObject.name.Contains("GVertical") || draggedObject.name.Contains("GHorizontal"))
            {
                // Vérifie si la guideline a des objets attachés avant de la déplacer
                bool hasAttachedObjects = HasAttachedObjects(draggedObject);

                // Met à jour la couleur de la guideline en fonction de son état
                UpdateGuidelineColor(draggedObject, hasAttachedObjects);

                MoveGuidelineWithAttachedObjects(newPosition);
            }
            else if (draggedObject.CompareTag("Draggable"))
            {
                CheckForSnapping(newPosition);
            }
        }
    }

    private void UpdateGuidelineColor(GameObject guideline, bool hasAttachedObjects)
    {
        if (!guideline) return;

        var guideImage = guideline.GetComponent<Image>();
        if (!guideImage) return;

        // Sauvegarde la couleur originale si nécessaire
        if (!originalColors.ContainsKey(guideline))
        {
            originalColors[guideline] = guideImage.color;
        }

        // La guideline reste sticky seulement si elle a des objets attachés
        guideImage.color = hasAttachedObjects ? stickyColor : originalColors[guideline];
    }


    private void CheckForSnappingPreview(Vector2 newPosition)
    {
        if (!draggedRect) return;

        // Reset previous preview
        if (currentSnapGuide != null)
        {
            ResetVisualFeedback(draggedObject);
            ResetVisualFeedback(currentSnapGuide);
            currentSnapGuide = null;
        }

        var guidelines = FindObjectsOfType<RectTransform>()
            .Where(rt => rt.gameObject != null &&
                   (rt.gameObject.name.Contains("GVertical") || rt.gameObject.name.Contains("GHorizontal")));

        foreach (var guidelineRect in guidelines)
        {
            if (!guidelineRect) continue;

            bool isVertical = guidelineRect.gameObject.name.Contains("GVertical");
            float distance = isVertical ?
                Mathf.Abs(newPosition.x - guidelineRect.anchoredPosition.x) :
                Mathf.Abs(newPosition.y - guidelineRect.anchoredPosition.y);

            if (distance < snapThreshold)
            {
                if (isVertical)
                {
                    newPosition.x = guidelineRect.anchoredPosition.x;
                }
                else
                {
                    newPosition.y = guidelineRect.anchoredPosition.y;
                }

                // Preview the snap
                PreviewSnap(draggedObject, guidelineRect.gameObject);
                currentSnapGuide = guidelineRect.gameObject;
                break;
            }
        }

        draggedRect.anchoredPosition = newPosition;
    }

    private void PreviewSnap(GameObject obj, GameObject guideline)
    {
        if (!obj || !guideline) return;

        var objImage = obj.GetComponent<Image>();
        var guideImage = guideline.GetComponent<Image>();

        if (!originalColors.ContainsKey(obj) && objImage)
        {
            originalColors[obj] = objImage.color;
        }

        if (!originalColors.ContainsKey(guideline) && guideImage)
        {
            originalColors[guideline] = guideImage.color;
        }

        if (objImage) objImage.color = previewColor;
        if (guideImage) guideImage.color = previewColor;
    }


    private void MoveGuidelineWithAttachedObjects(Vector2 newPosition)
    {
        if (!draggedRect) return;

        bool isVertical = draggedObject.name.Contains("GVertical");
        draggedRect.anchoredPosition = newPosition;

        if (attachedObjects.TryGetValue(draggedObject, out HashSet<GameObject> objects))
        {
            foreach (var obj in objects.ToList())
            {
                if (obj == null) continue;

                RectTransform objRect = obj.GetComponent<RectTransform>();
                if (!objRect) continue;

                Vector2 objPos = objRect.anchoredPosition;
                if (isVertical)
                {
                    objPos.x = newPosition.x;
                }
                else
                {
                    objPos.y = newPosition.y;
                }
                objRect.anchoredPosition = objPos;
            }
        }
    }


    private void CheckForSnapping(Vector2 newPosition)
    {
        if (!draggedRect) return;

        currentSnapGuide = null;
        var guidelines = FindObjectsOfType<RectTransform>()
            .Where(rt => rt.gameObject != null &&
                   (rt.gameObject.name.Contains("GVertical") || rt.gameObject.name.Contains("GHorizontal")));

        foreach (var guidelineRect in guidelines)
        {
            if (!guidelineRect) continue;

            bool isVertical = guidelineRect.gameObject.name.Contains("GVertical");
            float distance = isVertical ?
                Mathf.Abs(newPosition.x - guidelineRect.anchoredPosition.x) :
                Mathf.Abs(newPosition.y - guidelineRect.anchoredPosition.y);

            if (distance < snapThreshold)
            {
                if (isVertical)
                {
                    newPosition.x = guidelineRect.anchoredPosition.x;
                }
                else
                {
                    newPosition.y = guidelineRect.anchoredPosition.y;
                }

                currentSnapGuide = guidelineRect.gameObject;
                break;
            }
        }

        draggedRect.anchoredPosition = newPosition;
    }

    private void AttachToGuideline(GameObject obj, GameObject guideline)
    {
        if (!obj || !guideline) return;

        if (!attachedObjects.ContainsKey(guideline))
        {
            attachedObjects[guideline] = new HashSet<GameObject>();
        }

        if (attachedObjects[guideline].Add(obj))
        {
            var image = obj.GetComponent<Image>();
            if (image && !originalColors.ContainsKey(obj))
            {
                originalColors[obj] = image.color;
            }

            // Met à jour les couleurs avec la couleur sticky
            var objImage = obj.GetComponent<Image>();
            var guideImage = guideline.GetComponent<Image>();

            if (objImage) objImage.color = stickyColor;
            if (guideImage) guideImage.color = stickyColor;
        }
    }

    private void DetachFromAllGuidelines(GameObject obj)
    {
        if (!obj) return;

        foreach (var kvp in attachedObjects.ToList())
        {
            if (kvp.Key == null)
            {
                attachedObjects.Remove(kvp.Key);
                continue;
            }

            if (kvp.Value.Remove(obj))
            {
                var objImage = obj.GetComponent<Image>();
                if (objImage && originalColors.ContainsKey(obj))
                {
                    objImage.color = originalColors[obj];
                }

                // Met à jour la couleur de la guideline en fonction de son état actuel
                if (kvp.Value.Count == 0)
                {
                    var guideImage = kvp.Key.GetComponent<Image>();
                    if (guideImage && originalColors.ContainsKey(kvp.Key))
                    {
                        guideImage.color = originalColors[kvp.Key];
                    }
                }
            }
        }
    }


    private void UpdateVisualFeedback(GameObject obj, bool isAttached)
    {
        if (!obj) return;

        var image = obj.GetComponent<Image>();
        if (!image) return;

        if (!originalColors.ContainsKey(obj))
        {
            originalColors[obj] = image.color;
        }

        image.color = isAttached ? stickyColor : originalColors[obj];
    }

    private void EndDrag()
    {
        if (draggedObject != null)
        {
            bool isGuideline = draggedObject.name.Contains("GVertical") || draggedObject.name.Contains("GHorizontal");

            if (isGuideline)
            {
                // Met à jour la couleur de la guideline basée sur son état
                UpdateGuidelineColor(draggedObject, HasAttachedObjects(draggedObject));
            }
            else if (!isSnapping)
            {
                ResetObjectToOriginalColor(draggedObject);
            }
        }

        draggedObject = null;
        draggedRect = null;
        currentSnapGuide = null;
        isSnapping = false;
    }

    private void OnDestroy()
    {
        foreach (var kvp in originalColors.ToList())
        {
            if (kvp.Key != null)
            {
                var image = kvp.Key.GetComponent<Image>();
                if (image != null)
                {
                    image.color = kvp.Value;
                }
            }
        }

        attachedObjects.Clear();
        originalColors.Clear();
    }

    // Méthode pour détacher manuellement un objet
    public void DetachObject(GameObject obj)
    {
        if (obj != null)
        {
            DetachFromAllGuidelines(obj);
        }
    }

    // Méthode pour nettoyer les références nulles
    public void CleanupNullReferences()
    {
        foreach (var kvp in attachedObjects.ToList())
        {
            if (kvp.Key == null)
            {
                attachedObjects.Remove(kvp.Key);
                continue;
            }

            kvp.Value.RemoveWhere(obj => obj == null);
        }
    }
}
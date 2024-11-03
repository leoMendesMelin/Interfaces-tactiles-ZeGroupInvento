using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

using System.Linq;

public class StickyGuidelineSystem : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private float snapThreshold = 10f;
    [SerializeField] private bool enableSnapping = true;

    [Header("Visual Feedback")]
    [SerializeField] private Color stickyHighlightColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private float highlightDuration = 0.3f;

    private Canvas parentCanvas;
    private Dictionary<int, GuidelineTouch> activeTouches = new Dictionary<int, GuidelineTouch>();
    private Dictionary<GameObject, int> guidelineToTouchId = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, HashSet<GameObject>> stickyObjects = new Dictionary<GameObject, HashSet<GameObject>>();
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();




    private class GuidelineTouch
    {
        public GameObject guideline;
        public Vector2 initialGuidelinePosition;
        public Vector2 initialTouchPosition;
        public Vector2 lastValidPosition;
        public int touchId;
    }

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("Canvas non trouvé!");
        }
    }


    private void Update()
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // Vérifier si le touch est utilisé pour le redimensionnement
                if (IsResizing(touch))
                {
                    continue; // Ignorer ce touch s'il est utilisé pour le redimensionnement
                }

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryStartDrag(touch.fingerId, touch.position);
                        break;

                    case TouchPhase.Moved:
                        if (activeTouches.ContainsKey(touch.fingerId))
                        {
                            if (guidelineToTouchId.ContainsValue(touch.fingerId))
                            {
                                ContinueDrag(touch.fingerId, touch.position);
                            }
                            else
                            {
                                ContinueObjectDrag(touch.fingerId, touch.position);
                            }
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (activeTouches.ContainsKey(touch.fingerId))
                        {
                            EndDrag(touch.fingerId);
                        }
                        break;
                }
            }
        }

#if UNITY_EDITOR
        if (!IsMouseResizing()) // Nouvelle vérification pour la souris
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryStartDrag(-1, Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && activeTouches.ContainsKey(-1))
            {
                if (guidelineToTouchId.ContainsValue(-1))
                {
                    ContinueDrag(-1, Input.mousePosition);
                }
                else
                {
                    ContinueObjectDrag(-1, Input.mousePosition);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndDrag(-1);
            }
        }
#endif
    }

    private bool IsResizing(Touch touch)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = touch.position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            UIDragAndResize resizeComponent = result.gameObject.GetComponent<UIDragAndResize>();
            if (resizeComponent != null)
            {
                // Vérifier si le touch est sur une poignée de redimensionnement
                // Cette vérification dépend de l'implémentation de UIDragAndResize
                return true;
            }
        }

        return false;
    }

    private bool IsMouseResizing()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            UIDragAndResize resizeComponent = result.gameObject.GetComponent<UIDragAndResize>();
            if (resizeComponent != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        // Nettoyer les références et rétablir les couleurs originales
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                Image image = kvp.Key.GetComponent<Image>();
                if (image != null)
                {
                    image.color = kvp.Value;
                }
            }
        }
        originalColors.Clear();
        stickyObjects.Clear();
        activeTouches.Clear();
        guidelineToTouchId.Clear();
    }

    public void OnObjectDestroyed(GameObject obj)
    {
        if (obj == null) return;

        // Parcourir toutes les guidelines et retirer l'objet de leurs listes
        foreach (var kvp in stickyObjects.ToList())
        {
            if (kvp.Value.Contains(obj))
            {
                kvp.Value.Remove(obj);

                // Si la guideline n'a plus d'objets, réinitialiser sa couleur
                if (kvp.Value.Count == 0 && kvp.Key != null)
                {
                    ResetVisualEffect(kvp.Key);
                }
            }
        }

        // Nettoyer les autres références si nécessaire
        if (originalColors.ContainsKey(obj))
        {
            originalColors.Remove(obj);
        }
    }

    public void OnGuidelineDestroyed(GameObject guideline)
    {
        if (guideline == null) return;

        // Détacher tous les objets en premier
        DetachAllObjectsFromGuideline(guideline);

        // Nettoyer les autres références
        if (guidelineToTouchId.ContainsKey(guideline))
        {
            guidelineToTouchId.Remove(guideline);
        }

        // Nettoyer les touches associées
        List<int> touchesToRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            if (kvp.Value.guideline == guideline)
            {
                touchesToRemove.Add(kvp.Key);
            }
        }
        foreach (int touchId in touchesToRemove)
        {
            activeTouches.Remove(touchId);
        }

        // Réinitialiser la couleur si nécessaire
        ResetVisualEffect(guideline);
    }

    private void UpdateStickyObjectPosition(GameObject obj, GameObject guideline)
    {
        RectTransform objRect = obj.GetComponent<RectTransform>();
        RectTransform guideRect = guideline.GetComponent<RectTransform>();
        Vector2 newPos = objRect.anchoredPosition;

        if (guideline.name.StartsWith("GVertical"))
        {
            newPos.x = guideRect.anchoredPosition.x;
        }
        else if (guideline.name.StartsWith("GHorizontal"))
        {
            newPos.y = guideRect.anchoredPosition.y;
        }

        objRect.anchoredPosition = newPos;
    }

    // Méthode utile pour le debug
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualiser les connexions sticky
        foreach (var kvp in stickyObjects)
        {
            if (kvp.Key == null) continue;

            Vector3 guidelinePos = kvp.Key.transform.position;
            Gizmos.color = Color.green;

            foreach (var obj in kvp.Value)
            {
                if (obj == null) continue;
                Gizmos.DrawLine(guidelinePos, obj.transform.position);
            }
        }
    }

    private GameObject FindObjectUnderTouch(Vector2 screenPosition)
    {
        GameObject guideline = FindGuidelineUnderTouch(screenPosition);
        if (guideline != null) return guideline;

        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("Draggable"))
            {
                return result.gameObject;
            }
        }

        return null;
    }

    private void AddStickyObject(GameObject obj, GameObject guideline)
    {
        if (obj == null || guideline == null) return;

        if (!stickyObjects.ContainsKey(guideline))
        {
            stickyObjects[guideline] = new HashSet<GameObject>();
        }

        // Ajouter l'objet s'il n'est pas déjà attaché
        if (!stickyObjects[guideline].Contains(obj))
        {
            stickyObjects[guideline].Add(obj);

            // Aligner initialement l'objet avec la guideline
            RectTransform objRect = obj.GetComponent<RectTransform>();
            RectTransform guideRect = guideline.GetComponent<RectTransform>();

            if (objRect != null && guideRect != null)
            {
                Vector2 pos = objRect.anchoredPosition;
                if (guideline.name.StartsWith("GVertical"))
                {
                    pos.x = guideRect.anchoredPosition.x;
                }
                else if (guideline.name.StartsWith("GHorizontal"))
                {
                    pos.y = guideRect.anchoredPosition.y;
                }
                objRect.anchoredPosition = pos;
            }
        }
    }

    private GameObject FindNearestGuideline(Vector2 position, bool vertical)
    {
        // Chercher toutes les guidelines (en fonction du nom au lieu du tag)
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        GameObject nearest = null;
        float minDistance = snapThreshold;

        foreach (GameObject obj in allObjects)
        {
            // Vérifier si c'est une guideline du bon type
            bool isVerticalGuide = obj.name.StartsWith("GVertical");
            bool isHorizontalGuide = obj.name.StartsWith("GHorizontal");

            if ((vertical && isVerticalGuide) || (!vertical && isHorizontalGuide))
            {
                RectTransform guideRect = obj.GetComponent<RectTransform>();
                if (guideRect == null) continue;

                float distance = vertical ?
                    Mathf.Abs(position.x - guideRect.anchoredPosition.x) :
                    Mathf.Abs(position.y - guideRect.anchoredPosition.y);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = obj;
                }
            }
        }

        return nearest;
    }

    private void ContinueObjectDrag(int touchId, Vector2 screenPosition)
    {
        if (!activeTouches.ContainsKey(touchId)) return;

        GuidelineTouch touch = activeTouches[touchId];
        RectTransform objectRect = touch.guideline.GetComponent<RectTransform>();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        Vector2 currentTouchPositionInCanvas;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out currentTouchPositionInCanvas))
        {
            Vector2 touchDelta = currentTouchPositionInCanvas - touch.initialTouchPosition;
            Vector2 newPosition = touch.initialGuidelinePosition + touchDelta;

            // Vérifier le snap aux guidelines
            GameObject nearestVertical = FindNearestGuideline(newPosition, true);
            GameObject nearestHorizontal = FindNearestGuideline(newPosition, false);

            // Détacher des guidelines si trop loin
            CheckAndDetachFromGuidelines(touch.guideline, newPosition);

            bool wasSnapped = false;

            if (nearestVertical != null)
            {
                RectTransform guideRect = nearestVertical.GetComponent<RectTransform>();
                newPosition.x = guideRect.anchoredPosition.x;
                AddStickyObject(touch.guideline, nearestVertical);
                ShowSnapEffect(touch.guideline, nearestVertical);
                wasSnapped = true;
            }

            if (nearestHorizontal != null)
            {
                RectTransform guideRect = nearestHorizontal.GetComponent<RectTransform>();
                newPosition.y = guideRect.anchoredPosition.y;
                AddStickyObject(touch.guideline, nearestHorizontal);
                ShowSnapEffect(touch.guideline, nearestHorizontal);
                wasSnapped = true;
            }

            objectRect.anchoredPosition = newPosition;
            touch.lastValidPosition = newPosition;

            if (!wasSnapped)
            {
                ResetVisualEffect(touch.guideline);
            }
        }
    }

    private void ShowSnapEffect(GameObject obj, GameObject guideline)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null) return;

        // Sauvegarder la couleur originale
        if (!originalColors.ContainsKey(obj))
        {
            originalColors[obj] = image.color;
        }

        // Si l'objet est une guideline
        if (obj.name.StartsWith("GVertical") || obj.name.StartsWith("GHorizontal"))
        {
            // Vérifier si la guideline a des objets collés
            bool hasAttachedObjects = stickyObjects.ContainsKey(obj) && stickyObjects[obj].Count > 0;

            // Ne colorer que si elle a des objets collés et n'est pas en cours de déplacement
            bool isBeingDragged = guidelineToTouchId.ContainsKey(obj);
            image.color = (hasAttachedObjects && !isBeingDragged) ? stickyHighlightColor : originalColors[obj];
        }
        else // Pour les objets normaux
        {
            bool isSticky = stickyObjects.ContainsKey(guideline) && stickyObjects[guideline].Contains(obj);
            image.color = isSticky ? stickyHighlightColor : originalColors[obj];
        }
    }

    private void DetachAllObjectsFromGuideline(GameObject guideline)
    {
        if (guideline == null || !stickyObjects.ContainsKey(guideline)) return;

        // Créer une copie de la collection pour éviter les problèmes de modification pendant l'itération
        var objectsToDetach = new List<GameObject>(stickyObjects[guideline]);

        foreach (GameObject obj in objectsToDetach)
        {
            if (obj != null)
            {
                // Réinitialiser les effets visuels
                ResetVisualEffect(obj);
            }
        }

        // Vider et supprimer la collection
        stickyObjects[guideline].Clear();
        stickyObjects.Remove(guideline);
    }

    private void ResetVisualEffect(GameObject obj)
    {
        if (obj == null) return;

        Image image = obj.GetComponent<Image>();
        if (image != null && originalColors.ContainsKey(obj))
        {
            image.color = originalColors[obj];
            originalColors.Remove(obj);
        }
    }


    private void CheckAndDetachFromGuidelines(GameObject obj, Vector2 newPosition)
    {
        if (obj == null) return;

        List<GameObject> guidelinesToCheck = new List<GameObject>();
        List<GameObject> guidelinesToRemove = new List<GameObject>();

        // Collecter toutes les guidelines auxquelles l'objet est attaché
        foreach (var kvp in stickyObjects)
        {
            if (kvp.Key == null)
            {
                guidelinesToRemove.Add(kvp.Key);
                continue;
            }

            if (kvp.Value.Contains(obj))
            {
                guidelinesToCheck.Add(kvp.Key);
            }
        }

        // Nettoyer les guidelines nulles
        foreach (var guideline in guidelinesToRemove)
        {
            stickyObjects.Remove(guideline);
        }

        foreach (GameObject guideline in guidelinesToCheck)
        {
            if (guideline == null) continue;

            RectTransform guideRect = guideline.GetComponent<RectTransform>();
            if (guideRect == null) continue;

            bool isVertical = guideline.name.StartsWith("GVertical");

            float distance = isVertical ?
                Mathf.Abs(newPosition.x - guideRect.anchoredPosition.x) :
                Mathf.Abs(newPosition.y - guideRect.anchoredPosition.y);

            if (distance > snapThreshold)
            {
                DetachObjectFromGuideline(obj, guideline);
            }
        }
    }

    private void DetachObjectFromGuideline(GameObject obj, GameObject guideline)
    {
        if (obj == null || guideline == null) return;

        if (stickyObjects.ContainsKey(guideline))
        {
            stickyObjects[guideline].Remove(obj);
            ResetVisualEffect(obj);

            if (stickyObjects[guideline].Count == 0)
            {
                ResetVisualEffect(guideline);
            }
        }
    }


    private void ContinueDrag(int touchId, Vector2 screenPosition)
    {
        if (!activeTouches.ContainsKey(touchId)) return;

        GuidelineTouch touch = activeTouches[touchId];
        if (touch.guideline == null) return;

        RectTransform guidelineRect = touch.guideline.GetComponent<RectTransform>();
        if (guidelineRect == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        Vector2 currentTouchPositionInCanvas;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out currentTouchPositionInCanvas))
        {
            Vector2 touchDelta = currentTouchPositionInCanvas - touch.initialTouchPosition;
            Vector2 newPosition = touch.initialGuidelinePosition + touchDelta;

            if (IsPositionWithinCanvas(newPosition, guidelineRect, canvasRect))
            {
                // Sauvegarder l'ancienne position pour calculer le mouvement
                Vector2 oldPosition = guidelineRect.anchoredPosition;

                // Déplacer la guideline
                guidelineRect.anchoredPosition = newPosition;
                touch.lastValidPosition = newPosition;

                // Ne mettre à jour que les objets attachés à cette guideline spécifique
                if (stickyObjects.ContainsKey(touch.guideline))
                {
                    foreach (var stickyObject in stickyObjects[touch.guideline].ToList())
                    {
                        if (stickyObject == null) continue;

                        RectTransform stickyRect = stickyObject.GetComponent<RectTransform>();
                        if (stickyRect == null) continue;

                        // Déplacer l'objet en fonction du type de guideline
                        Vector2 objectPosition = stickyRect.anchoredPosition;
                        if (touch.guideline.name.StartsWith("GVertical"))
                        {
                            // Pour une guideline verticale, on ne modifie que la position X
                            objectPosition.x = newPosition.x;
                        }
                        else if (touch.guideline.name.StartsWith("GHorizontal"))
                        {
                            // Pour une guideline horizontale, on ne modifie que la position Y
                            objectPosition.y = newPosition.y;
                        }
                        stickyRect.anchoredPosition = objectPosition;
                    }
                }
            }
        }
    }

    private void UpdateAttachedObjects(GameObject guideline, Vector2 newPosition)
    {
        if (guideline == null || !stickyObjects.ContainsKey(guideline)) return;

        bool isVertical = guideline.name.StartsWith("GVertical");
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (GameObject stickyObject in stickyObjects[guideline])
        {
            if (stickyObject == null)
            {
                objectsToRemove.Add(stickyObject);
                continue;
            }

            RectTransform stickyRect = stickyObject.GetComponent<RectTransform>();
            if (stickyRect == null) continue;

            // Mise à jour de la position
            Vector2 currentPos = stickyRect.anchoredPosition;

            if (isVertical)
            {
                // Pour une guideline verticale, on ne met à jour que la position X
                currentPos.x = newPosition.x;
            }
            else
            {
                // Pour une guideline horizontale, on ne met à jour que la position Y
                currentPos.y = newPosition.y;
            }

            stickyRect.anchoredPosition = currentPos;
        }

        // Nettoyer les objets nuls
        foreach (var obj in objectsToRemove)
        {
            stickyObjects[guideline].Remove(obj);
        }

        // Mettre à jour l'effet visuel si nécessaire
        if (stickyObjects[guideline].Count == 0)
        {
            ResetVisualEffect(guideline);
        }
    }

    private void TryStartDrag(int touchId, Vector2 screenPosition)
    {
        GameObject touchedObject = FindObjectUnderTouch(screenPosition);

        if (touchedObject != null)
        {
            RectTransform objectRect = touchedObject.GetComponent<RectTransform>();
            Vector2 touchPositionInCanvas;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.GetComponent<RectTransform>(),
                screenPosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out touchPositionInCanvas
            );

            GuidelineTouch newTouch = new GuidelineTouch
            {
                guideline = touchedObject,
                initialGuidelinePosition = objectRect.anchoredPosition,
                initialTouchPosition = touchPositionInCanvas,
                lastValidPosition = objectRect.anchoredPosition,
                touchId = touchId
            };

            activeTouches[touchId] = newTouch;

            if (touchedObject.CompareTag("Guideline"))
            {
                guidelineToTouchId[touchedObject] = touchId;
            }
        }
    }

    private void EndDrag(int touchId)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            GameObject guideline = activeTouches[touchId].guideline;
            if (guidelineToTouchId.ContainsKey(guideline))
            {
                guidelineToTouchId.Remove(guideline);
            }
            activeTouches.Remove(touchId);
        }
    }

    private bool IsPositionWithinCanvas(Vector2 position, RectTransform objectRect, RectTransform canvasRect)
    {
        float halfWidth = objectRect.rect.width * 0.5f;
        float halfHeight = objectRect.rect.height * 0.5f;
        float minX = -canvasRect.rect.width * 0.5f + halfWidth;
        float maxX = canvasRect.rect.width * 0.5f - halfWidth;
        float minY = -canvasRect.rect.height * 0.5f + halfHeight;
        float maxY = canvasRect.rect.height * 0.5f - halfHeight;

        return position.x >= minX && position.x <= maxX &&
               position.y >= minY && position.y <= maxY;
    }

    private GameObject FindGuidelineUnderTouch(Vector2 screenPosition)
    {
        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // Vérifier si c'est une guideline (GVertical ou GHorizontal)
            if (result.gameObject.name.StartsWith("GVertical") ||
                result.gameObject.name.StartsWith("GHorizontal"))
            {
                return result.gameObject;
            }
        }

        return null;
    }

    public void SetSnappingEnabled(bool enabled)
    {
        enableSnapping = enabled;
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GuidelineDragManager : MonoBehaviour
{
    private Canvas parentCanvas;
    private Dictionary<int, GuidelineTouch> activeTouches = new Dictionary<int, GuidelineTouch>();
    private Dictionary<GameObject, int> guidelineToTouchId = new Dictionary<GameObject, int>(); // Pour tracker quel touch contrôle quelle guideline


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
        // Gérer les touches
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryStartDrag(touch.fingerId, touch.position);
                        break;

                    case TouchPhase.Moved:
                        // Vérifier si ce touch est associé à une guideline
                        if (activeTouches.ContainsKey(touch.fingerId) &&
                            guidelineToTouchId.ContainsValue(touch.fingerId))
                        {
                            ContinueDrag(touch.fingerId, touch.position);
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

        // Support de la souris pour les tests en éditeur
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(-1, Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && activeTouches.ContainsKey(-1) && 
                 guidelineToTouchId.ContainsValue(-1))
        {
            ContinueDrag(-1, Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && activeTouches.ContainsKey(-1))
        {
            EndDrag(-1);
        }
#endif

        // Nettoyage des touches perdues
        CleanupLostTouches();
    }

    private void TryStartDrag(int touchId, Vector2 screenPosition)
    {
        GameObject touchedGuideline = FindGuidelineUnderTouch(screenPosition);

        if (touchedGuideline != null)
        {
            // Vérifier si cette guideline n'est pas déjà en cours de manipulation
            if (!guidelineToTouchId.ContainsKey(touchedGuideline))
            {
                RectTransform guidelineRect = touchedGuideline.GetComponent<RectTransform>();

                Vector2 touchPositionInCanvas;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    screenPosition,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                    out touchPositionInCanvas
                );

                GuidelineTouch newTouch = new GuidelineTouch
                {
                    guideline = touchedGuideline,
                    initialGuidelinePosition = guidelineRect.anchoredPosition,
                    initialTouchPosition = touchPositionInCanvas,
                    lastValidPosition = guidelineRect.anchoredPosition,
                    touchId = touchId
                };

                activeTouches[touchId] = newTouch;
                guidelineToTouchId[touchedGuideline] = touchId;
            }
        }
    }

    private void ContinueDrag(int touchId, Vector2 screenPosition)
    {
        if (!activeTouches.ContainsKey(touchId)) return;

        GuidelineTouch touch = activeTouches[touchId];

        // Vérifier si ce touch est toujours associé à cette guideline
        if (!guidelineToTouchId.ContainsKey(touch.guideline) ||
            guidelineToTouchId[touch.guideline] != touchId) return;

        RectTransform guidelineRect = touch.guideline.GetComponent<RectTransform>();
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
                guidelineRect.anchoredPosition = newPosition;
                touch.lastValidPosition = newPosition;
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

    private void CleanupLostTouches()
    {
        // Créer une liste des touches à nettoyer
        List<int> touchesToRemove = new List<int>();
        List<GameObject> guidelinesToRemove = new List<GameObject>();

        // Vérifier les touches perdues
        foreach (var touch in activeTouches)
        {
            bool touchExists = false;
            if (touch.Key == -1) // Cas spécial pour la souris
            {
                touchExists = Input.GetMouseButton(0);
            }
            else
            {
                foreach (Touch activeTouch in Input.touches)
                {
                    if (activeTouch.fingerId == touch.Key)
                    {
                        touchExists = true;
                        break;
                    }
                }
            }

            if (!touchExists)
            {
                touchesToRemove.Add(touch.Key);
                if (touch.Value.guideline != null)
                {
                    guidelinesToRemove.Add(touch.Value.guideline);
                }
            }
        }

        // Nettoyer les touches perdues
        foreach (int touchId in touchesToRemove)
        {
            activeTouches.Remove(touchId);
        }

        foreach (GameObject guideline in guidelinesToRemove)
        {
            if (guidelineToTouchId.ContainsKey(guideline))
            {
                guidelineToTouchId.Remove(guideline);
            }
        }
    }

    // Les autres méthodes restent identiques...
    private GameObject FindGuidelineUnderTouch(Vector2 screenPosition)
    {
        GameObject[] guidelines = GameObject.FindGameObjectsWithTag("Guideline");

        foreach (GameObject guideline in guidelines)
        {
            RectTransform rect = guideline.GetComponent<RectTransform>();
            if (rect != null && IsPositionOverGuideline(screenPosition, rect))
            {
                return guideline;
            }
        }

        return null;
    }

    private bool IsPositionOverGuideline(Vector2 screenPosition, RectTransform rect)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            screenPosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localPoint))
        {
            Vector2 normalizedPoint = new Vector2(
                (localPoint.x + rect.rect.width * 0.5f) / rect.rect.width,
                (localPoint.y + rect.rect.height * 0.5f) / rect.rect.height
            );

            return normalizedPoint.x >= 0 && normalizedPoint.x <= 1 &&
                   normalizedPoint.y >= 0 && normalizedPoint.y <= 1;
        }

        return false;
    }

    private bool IsPositionWithinCanvas(Vector2 position, RectTransform guidelineRect, RectTransform canvasRect)
    {
        float halfWidth = guidelineRect.rect.width * 0.5f;
        float halfHeight = guidelineRect.rect.height * 0.5f;
        float minX = -canvasRect.rect.width * 0.5f + halfWidth;
        float maxX = canvasRect.rect.width * 0.5f - halfWidth;
        float minY = -canvasRect.rect.height * 0.5f + halfHeight;
        float maxY = canvasRect.rect.height * 0.5f - halfHeight;

        return position.x >= minX && position.x <= maxX &&
               position.y >= minY && position.y <= maxY;
    }
}
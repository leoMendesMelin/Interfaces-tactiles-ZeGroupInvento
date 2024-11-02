using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDragAndResize : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Settings")]
    [SerializeField] private float handleSize = 40f;
    [SerializeField] private float minPinchDistance = 50f;
    [SerializeField] private float pinchThresholdUpgrade = 1.5f;
    [SerializeField] private float pinchThresholdDowngrade = 0.6f;

    [Header("State")]
    private bool isResizable = true;
    private bool isTable = false;
    private string currentTableSize = "x2";
    private string tableType = "";

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas canvas;
    private MenuManager menuManager;

    // Drag state
    private bool isDragging = false;
    private Vector2 startPointerPosition;
    private Vector2 startAnchoredPosition;
    private Vector2 startSize;
    private Vector2 originalPosition;
    private Vector2 originalSize;

    // Pinch state
    private Vector2 touch1StartPos;
    private Vector2 touch2StartPos;
    private float startPinchDistance;
    private bool isPinching = false;

    private enum DragHandle
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }
    private DragHandle currentHandle = DragHandle.None;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
        menuManager = FindObjectOfType<MenuManager>();

        InitializeTableProperties();
    }

    private void InitializeTableProperties()
    {
        string objectName = gameObject.name.Replace("(Clone)", "");
        Debug.Log($"Initializing table: {objectName}"); // Pour debug

        if (objectName.Contains("CircleTable"))
        {
            isTable = true;
            tableType = "Circle";
            if (objectName.Contains("x2")) currentTableSize = "x2";
            else if (objectName.Contains("x4")) currentTableSize = "x4";
            Debug.Log($"Circle table initialized with size: {currentTableSize}"); // Pour debug
        }
        else if (objectName.Contains("RectangleTable"))
        {
            isTable = true;
            tableType = "Rectangle";
            if (objectName.Contains("x2")) currentTableSize = "x2";
            else if (objectName.Contains("x4")) currentTableSize = "x4";
            else if (objectName.Contains("x6")) currentTableSize = "x6";
            Debug.Log($"Rectangle table initialized with size: {currentTableSize}"); // Pour debug
        }
    }

    void Update()
    {
        if (!isTable) return;

        if (Input.touchCount == 2)
        {
            HandlePinchGesture();
        }
        else
        {
            isPinching = false;
        }
    }

    private void HandlePinchGesture()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            StartPinch(touch1, touch2);
        }
        else if (isPinching && (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved))
        {
            UpdatePinch(touch1, touch2);
        }
        else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
        {
            isPinching = false;
        }
    }

    private void StartPinch(Touch touch1, Touch touch2)
    {
        Vector2 touchPos1, touchPos2;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touch1.position, null, out touchPos1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touch2.position, null, out touchPos2);

        if (IsPointInsideRect(touchPos1) && IsPointInsideRect(touchPos2))
        {
            touch1StartPos = touch1.position;
            touch2StartPos = touch2.position;
            startPinchDistance = Vector2.Distance(touch1StartPos, touch2StartPos);
            isPinching = startPinchDistance >= minPinchDistance;
        }
    }

    private void UpdatePinch(Touch touch1, Touch touch2)
    {
        float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);
        float pinchRatio = currentPinchDistance / startPinchDistance;

        // Zoom In (agrandissement)
        if (pinchRatio >= pinchThresholdUpgrade)
        {
            if (tableType == "Circle")
            {
                if (currentTableSize == "x2") UpgradeTable("x4");
            }
            else if (tableType == "Rectangle")
            {
                if (currentTableSize == "x2") UpgradeTable("x4");
                else if (currentTableSize == "x4") UpgradeTable("x6");
            }
        }
        // Zoom Out (réduction)
        else if (pinchRatio <= pinchThresholdDowngrade)
        {
            if (tableType == "Circle")
            {
                // Ne permettre la réduction que pour les tables x4
                if (currentTableSize == "x4") DowngradeTable("x2");
            }
            else if (tableType == "Rectangle")
            {
                // Ne permettre la réduction que pour les tables x6 et x4
                if (currentTableSize == "x6") DowngradeTable("x4");
                else if (currentTableSize == "x4") DowngradeTable("x2");
            }
        }
    }

    private bool IsPointInsideRect(Vector2 localPoint)
    {
        Vector2 halfSize = rectTransform.rect.size * 0.5f;
        return Mathf.Abs(localPoint.x) <= halfSize.x && Mathf.Abs(localPoint.y) <= halfSize.y;
    }

    public void SetResizable(bool resizable)
    {
        isResizable = resizable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1) return;

        currentHandle = isResizable ? GetHandleAtPosition(eventData.position) : DragHandle.None;

        if (currentHandle != DragHandle.None)
        {
            startPointerPosition = eventData.position;
            startSize = rectTransform.rect.size;
            originalPosition = rectTransform.anchoredPosition;
            originalSize = rectTransform.rect.size;
        }
        else
        {
            isDragging = true;
            startPointerPosition = eventData.position;
            startAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1) return;

        if (isDragging)
        {
            HandleDrag(eventData);
        }
        else if (currentHandle != DragHandle.None && isResizable)
        {
            HandleResize(eventData);
        }
    }

    private void HandleDrag(PointerEventData eventData)
    {
        Vector2 difference = eventData.position - startPointerPosition;
        Vector2 proposedPosition = startAnchoredPosition + difference / canvas.scaleFactor;

        float halfWidth = rectTransform.rect.width / 2;
        float halfHeight = rectTransform.rect.height / 2;
        float parentHalfWidth = parentRectTransform.rect.width / 2;
        float parentHalfHeight = parentRectTransform.rect.height / 2;

        proposedPosition.x = Mathf.Clamp(proposedPosition.x, -parentHalfWidth + halfWidth, parentHalfWidth - halfWidth);
        proposedPosition.y = Mathf.Clamp(proposedPosition.y, -parentHalfHeight + halfHeight, parentHalfHeight - halfHeight);

        rectTransform.anchoredPosition = proposedPosition;
    }

    private void HandleResize(PointerEventData eventData)
    {
        Vector2 difference = (eventData.position - startPointerPosition) / canvas.scaleFactor;
        Vector2 newSize = startSize;
        Vector2 newPosition = originalPosition;

        float parentHalfWidth = parentRectTransform.rect.width / 2;
        float parentHalfHeight = parentRectTransform.rect.height / 2;

        switch (currentHandle)
        {
            case DragHandle.Right:
                if (originalPosition.x + (originalSize.x + difference.x) / 2 <= parentHalfWidth)
                {
                    newSize.x = Mathf.Max(0.1f, originalSize.x + difference.x);
                    newPosition.x = originalPosition.x + difference.x / 2;
                }
                break;
            case DragHandle.Left:
                if (originalPosition.x - (originalSize.x - difference.x) / 2 >= -parentHalfWidth)
                {
                    float potentialWidth = originalSize.x - difference.x;
                    if (potentialWidth > 0.1f)
                    {
                        newSize.x = potentialWidth;
                        newPosition.x = originalPosition.x + difference.x / 2;
                    }
                }
                break;
            case DragHandle.Top:
                if (originalPosition.y + (originalSize.y + difference.y) / 2 <= parentHalfHeight)
                {
                    newSize.y = Mathf.Max(0.1f, originalSize.y + difference.y);
                    newPosition.y = originalPosition.y + difference.y / 2;
                }
                break;
            case DragHandle.Bottom:
                if (originalPosition.y - (originalSize.y - difference.y) / 2 >= -parentHalfHeight)
                {
                    float potentialHeight = originalSize.y - difference.y;
                    if (potentialHeight > 0.1f)
                    {
                        newSize.y = potentialHeight;
                        newPosition.y = originalPosition.y + difference.y / 2;
                    }
                }
                break;
        }

        if (newSize.x > 0.1f && newSize.y > 0.1f)
        {
            rectTransform.sizeDelta = newSize;
            rectTransform.anchoredPosition = newPosition;
        }
    }

    private void UpgradeTable(string newSize)
    {
        string objectName = gameObject.name.Replace("(Clone)", "");
        string newTableName = objectName.Replace(currentTableSize, newSize);
        SpawnReplacementTable(newTableName);
    }

    private void DowngradeTable(string newSize)
    {
        string objectName = gameObject.name.Replace("(Clone)", "");
        string newTableName = objectName.Replace(currentTableSize, newSize);
        SpawnReplacementTable(newTableName);
    }

    private void SpawnReplacementTable(string newTableName)
    {
        Vector2 currentPosition = rectTransform.anchoredPosition;
        menuManager.SpawnPrefab(newTableName);

        GameObject lastSpawned = menuManager.GetLastSpawnedObject();
        if (lastSpawned != null)
        {
            RectTransform newRectTransform = lastSpawned.GetComponent<RectTransform>();
            newRectTransform.anchoredPosition = currentPosition;
        }

        Destroy(gameObject);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        currentHandle = DragHandle.None;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    private DragHandle GetHandleAtPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out localPoint);

        if (localPoint.x < -rectTransform.rect.width / 2 + handleSize)
            return DragHandle.Left;
        if (localPoint.x > rectTransform.rect.width / 2 - handleSize)
            return DragHandle.Right;
        if (localPoint.y > rectTransform.rect.height / 2 - handleSize)
            return DragHandle.Top;
        if (localPoint.y < -rectTransform.rect.height / 2 + handleSize)
            return DragHandle.Bottom;

        return DragHandle.None;
    }
}
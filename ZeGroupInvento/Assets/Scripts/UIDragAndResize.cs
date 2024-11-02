using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDragAndResize : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Settings")]
    [SerializeField] private float handleSize = 40f;
    [SerializeField] private float cornerSize = 40f;
    [SerializeField] private float minPinchDistance = 50f;
    [SerializeField] private float pinchThresholdUpgrade = 1.5f;
    [SerializeField] private float pinchThresholdDowngrade = 0.6f;
    [SerializeField] private float rotationSensitivity = 1f;

    [Header("State")]
    private bool isResizable = true;
    private bool isTable = false;
    private string currentTableSize = "x2";
    private string tableType = "";
    private bool isRotating = false;
    private Vector2 rotationCenter;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas canvas;
    private MenuManager menuManager;

    // Structure pour stocker les informations de touch
    private class TouchInfo
    {
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public DragHandle handle;
        public float initialRotation;
        public bool isCornerDrag;
    }

    // Multi-touch state
    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private Vector2 lastCenterPoint;

    private enum DragHandle
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,     // Ajout des coins
        TopRight,
        BottomLeft,
        BottomRight
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
        Debug.Log($"Initializing table: {objectName}");

        if (objectName.Contains("CircleTable"))
        {
            isTable = true;
            tableType = "Circle";
            if (objectName.Contains("x2")) currentTableSize = "x2";
            else if (objectName.Contains("x4")) currentTableSize = "x4";
        }
        else if (objectName.Contains("RectangleTable"))
        {
            isTable = true;
            tableType = "Rectangle";
            if (objectName.Contains("x2")) currentTableSize = "x2";
            else if (objectName.Contains("x4")) currentTableSize = "x4";
            else if (objectName.Contains("x6")) currentTableSize = "x6";
        }
    }

    void Update()
    {
        HandleMultiTouch();
    }

    private void HandleMultiTouch()
    {
        // Gérer les nouveaux touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch);
                    break;

                case TouchPhase.Moved:
                    HandleTouchMoved(touch);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded(touch);
                    break;
            }
        }

        // Si nous avons exactement 2 touches actives et c'est une table, gérer le pinch
        if (isTable && activeTouches.Count == 2)
        {
            HandlePinchGesture();
        }

        // Appliquer les mouvements des touches actifs
        if (activeTouches.Count > 0)
        {
            ApplyMultiTouchTransform();
        }
    }

    private void HandleTouchMoved(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            var touchInfo = activeTouches[touch.fingerId];
            touchInfo.currentPosition = touch.position;

            // Si c'est un touch de rotation (n'importe quel coin)
            if (IsCornerHandle(touchInfo.handle) && touchInfo.isCornerDrag)
            {
                HandleRotation(touchInfo);
            }
            else
            {
                activeTouches[touch.fingerId].currentPosition = touch.position;
            }
        }
    }

    private void HandleTouchBegan(Touch touch)
    {
        if (rectTransform != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touch.position, null, out localPoint))
            {
                if (IsPointInsideRect(localPoint))
                {
                    DragHandle handle = GetHandleAtPosition(touch.position);
                    bool isCorner = IsCornerHandle(handle);

                    if (isCorner)
                    {
                        isRotating = true;
                        rotationCenter = rectTransform.anchoredPosition;
                    }

                    TouchInfo touchInfo = new TouchInfo
                    {
                        startPosition = touch.position,
                        currentPosition = touch.position,
                        handle = handle,
                        initialRotation = rectTransform.rotation.eulerAngles.z,
                        isCornerDrag = isCorner
                    };

                    activeTouches[touch.fingerId] = touchInfo;

                    if (activeTouches.Count == 1)
                    {
                        lastCenterPoint = touch.position;
                    }
                }
            }
        }
    }

    private void HandleTouchEnded(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            var touchInfo = activeTouches[touch.fingerId];
            if (IsCornerHandle(touchInfo.handle))
            {
                isRotating = false;
            }
        }
        activeTouches.Remove(touch.fingerId);
    }

    private bool IsCornerHandle(DragHandle handle)
    {
        return handle == DragHandle.TopLeft ||
               handle == DragHandle.TopRight ||
               handle == DragHandle.BottomLeft ||
               handle == DragHandle.BottomRight;
    }

    private void HandleRotation(TouchInfo touchInfo)
    {
        Vector2 startPos, currentPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, touchInfo.startPosition, null, out startPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, touchInfo.currentPosition, null, out currentPos);

        Vector2 objectCenter = rectTransform.anchoredPosition;
        float startAngle = Mathf.Atan2(startPos.y - objectCenter.y, startPos.x - objectCenter.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(currentPos.y - objectCenter.y, currentPos.x - objectCenter.x) * Mathf.Rad2Deg;

        float deltaAngle = (currentAngle - startAngle) * rotationSensitivity;
        rectTransform.rotation = Quaternion.Euler(0, 0, rectTransform.rotation.eulerAngles.z + deltaAngle);

        touchInfo.startPosition = touchInfo.currentPosition;
    }



    private void ApplyMultiTouchTransform()
    {
        if (activeTouches.Count == 0) return;

        // Vérifier si on est en train de redimensionner
        bool isResizing = false;
        foreach (var touchInfo in activeTouches.Values)
        {
            if (touchInfo.handle != DragHandle.None)
            {
                isResizing = true;
                break;
            }
        }

        // Ne faire le déplacement que si aucun touch n'est en train de resize
        if (!isResizing)
        {
            // Calculer le nouveau centre
            Vector2 centerPoint = Vector2.zero;
            foreach (var touch in activeTouches.Values)
            {
                centerPoint += touch.currentPosition;
            }
            centerPoint /= activeTouches.Count;

            // Convertir les positions de l'écran en positions dans le canvas
            Vector2 screenCenterPoint;
            Vector2 screenLastCenterPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, centerPoint, null, out screenCenterPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, lastCenterPoint, null, out screenLastCenterPoint);

            // Calculer le mouvement dans l'espace du canvas
            Vector2 movement = screenCenterPoint - screenLastCenterPoint;

            // Calculer les dimensions de la boîte englobante après rotation
            float currentRotation = rectTransform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(currentRotation));
            float sin = Mathf.Abs(Mathf.Sin(currentRotation));

            // Calculer la nouvelle largeur et hauteur de la boîte englobante
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float rotatedWidth = width * cos + height * sin;
            float rotatedHeight = width * sin + height * cos;

            // Appliquer le mouvement avec les limites du parent en utilisant la boîte englobante rotative
            Vector2 proposedPosition = rectTransform.anchoredPosition + movement;
            float halfRotatedWidth = rotatedWidth / 2;
            float halfRotatedHeight = rotatedHeight / 2;
            float parentHalfWidth = parentRectTransform.rect.width / 2;
            float parentHalfHeight = parentRectTransform.rect.height / 2;

            // Appliquer les contraintes avec la boîte englobante rotative
            proposedPosition.x = Mathf.Clamp(proposedPosition.x, -parentHalfWidth + halfRotatedWidth, parentHalfWidth - halfRotatedWidth);
            proposedPosition.y = Mathf.Clamp(proposedPosition.y, -parentHalfHeight + halfRotatedHeight, parentHalfHeight - halfRotatedHeight);

            rectTransform.anchoredPosition = proposedPosition;
            lastCenterPoint = centerPoint;
        }

        // Gérer le redimensionnement pour chaque touch sur une poignée
        foreach (var touchInfo in activeTouches.Values)
        {
            if (touchInfo.handle != DragHandle.None && isResizable)
            {
                HandleResizeForTouch(touchInfo);
            }
        }
    }

    private void HandleResizeForTouch(TouchInfo touchInfo)
    {
        // Convertir les positions en coordonnées canvas en tenant compte de la rotation
        Vector2 currentCanvasPos, startCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, touchInfo.currentPosition, null, out currentCanvasPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, touchInfo.startPosition, null, out startCanvasPos);

        // Convertir la différence dans l'espace local de l'objet pivoté
        Vector2 difference = currentCanvasPos - startCanvasPos;
        float currentRotation = rectTransform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 rotatedDifference = new Vector2(
            difference.x * Mathf.Cos(-currentRotation) - difference.y * Mathf.Sin(-currentRotation),
            difference.x * Mathf.Sin(-currentRotation) + difference.y * Mathf.Cos(-currentRotation)
        );

        Vector2 newSize = rectTransform.sizeDelta;
        Vector2 currentPosition = rectTransform.anchoredPosition;
        Vector2 newPosition = currentPosition;

        float parentHalfWidth = parentRectTransform.rect.width / 2;
        float parentHalfHeight = parentRectTransform.rect.height / 2;

        // Calculer les limites en tenant compte de la rotation
        float cos = Mathf.Abs(Mathf.Cos(currentRotation));
        float sin = Mathf.Abs(Mathf.Sin(currentRotation));
        float rotatedWidth = newSize.x * cos + newSize.y * sin;
        float rotatedHeight = newSize.x * sin + newSize.y * cos;

        switch (touchInfo.handle)
        {
            case DragHandle.Right:
                float rightDelta = rotatedDifference.x;
                float maxRightDelta = (parentHalfWidth - (currentPosition.x + rotatedWidth / 2));
                rightDelta = Mathf.Min(rightDelta, maxRightDelta);
                newSize.x = Mathf.Max(0.1f, newSize.x + rightDelta);

                // Ajuster la position en tenant compte de la rotation
                Vector2 rightOffset = RotateVector(new Vector2(rightDelta / 2, 0), currentRotation);
                newPosition += rightOffset;
                break;

            case DragHandle.Left:
                float leftDelta = -rotatedDifference.x;
                float maxLeftDelta = (parentHalfWidth + (currentPosition.x - rotatedWidth / 2));
                leftDelta = Mathf.Min(leftDelta, maxLeftDelta);
                newSize.x = Mathf.Max(0.1f, newSize.x + leftDelta);

                // Ajuster la position en tenant compte de la rotation
                Vector2 leftOffset = RotateVector(new Vector2(-leftDelta / 2, 0), currentRotation);
                newPosition += leftOffset;
                break;

            case DragHandle.Top:
                float topDelta = rotatedDifference.y;
                float maxTopDelta = (parentHalfHeight - (currentPosition.y + rotatedHeight / 2));
                topDelta = Mathf.Min(topDelta, maxTopDelta);
                newSize.y = Mathf.Max(0.1f, newSize.y + topDelta);

                // Ajuster la position en tenant compte de la rotation
                Vector2 topOffset = RotateVector(new Vector2(0, topDelta / 2), currentRotation);
                newPosition += topOffset;
                break;

            case DragHandle.Bottom:
                float bottomDelta = -rotatedDifference.y;
                float maxBottomDelta = (parentHalfHeight + (currentPosition.y - rotatedHeight / 2));
                bottomDelta = Mathf.Min(bottomDelta, maxBottomDelta);
                newSize.y = Mathf.Max(0.1f, newSize.y + bottomDelta);

                // Ajuster la position en tenant compte de la rotation
                Vector2 bottomOffset = RotateVector(new Vector2(0, -bottomDelta / 2), currentRotation);
                newPosition += bottomOffset;
                break;
        }

        // Vérifier que la nouvelle taille est valide avant d'appliquer les changements
        if (newSize.x >= 0.1f && newSize.y >= 0.1f)
        {
            rectTransform.sizeDelta = newSize;
            rectTransform.anchoredPosition = newPosition;
            touchInfo.startPosition = touchInfo.currentPosition;
        }
    }
    private Vector2 RotateVector(Vector2 vector, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void HandlePinchGesture()
    {
        if (activeTouches.Count != 2) return;

        var touches = new List<TouchInfo>(activeTouches.Values);
        float currentPinchDistance = Vector2.Distance(touches[0].currentPosition, touches[1].currentPosition);
        float startPinchDistance = Vector2.Distance(touches[0].startPosition, touches[1].startPosition);
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
                if (currentTableSize == "x4") DowngradeTable("x2");
            }
            else if (tableType == "Rectangle")
            {
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

    // Interface implementations maintenues pour la compatibilité avec les événements UI
    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    private void OnDrawGizmos()
    {
        if (rectTransform != null)
        {
            // Dessiner les zones de coin
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Gizmos.color = Color.yellow;
            foreach (var corner in corners)
            {
                Gizmos.DrawWireSphere(corner, cornerSize);
            }
        }
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) { }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    private DragHandle GetHandleAtPosition(Vector2 screenPosition)
    {
        if (rectTransform != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out localPoint))
            {
                float halfWidth = rectTransform.rect.width / 2;
                float halfHeight = rectTransform.rect.height / 2;

                // Vérification des coins avec la zone élargie (cornerSize)
                bool isNearLeft = localPoint.x < -halfWidth + cornerSize;
                bool isNearRight = localPoint.x > halfWidth - cornerSize;
                bool isNearTop = localPoint.y > halfHeight - cornerSize;
                bool isNearBottom = localPoint.y < -halfHeight + cornerSize;

                // Détection des coins
                if (isNearTop && isNearLeft) return DragHandle.TopLeft;
                if (isNearTop && isNearRight) return DragHandle.TopRight;
                if (isNearBottom && isNearLeft) return DragHandle.BottomLeft;
                if (isNearBottom && isNearRight) return DragHandle.BottomRight;

                // Détection des bords pour le redimensionnement
                if (isNearLeft && !isNearTop && !isNearBottom) return DragHandle.Left;
                if (isNearRight && !isNearTop && !isNearBottom) return DragHandle.Right;
                if (isNearTop && !isNearLeft && !isNearRight) return DragHandle.Top;
                if (isNearBottom && !isNearLeft && !isNearRight) return DragHandle.Bottom;
            }
        }

        return DragHandle.None;
    }
}
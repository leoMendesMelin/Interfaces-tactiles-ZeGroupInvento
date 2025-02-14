using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // Ajoutez cette ligne pour Image

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ZoneControllerPrefab : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler
{
    private ZoneData data;
    private ZoneManager manager;
    private GridManager gridManager;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Vector2 lastGridPosition;

    private ZoneWebSocketManager webSocketManager;

    // Variables pour le pinch-to-zoom
    private bool isPinching = false;
    private float initialPinchDistance;
    private Vector2Int initialPinchSize;

    private Material blurMaterial;
    private Image zoneImage;
    private bool isBlurred = false;
    public void Initialize(ZoneData data, ZoneManager manager, GridManager gridManager)
    {
        if (data == null || manager == null || gridManager == null)
        {
            Debug.LogError($"Null references in Initialize: data={data != null}, manager={manager != null}, gridManager={gridManager != null}");
            return;
        }

        webSocketManager = FindObjectOfType<ZoneWebSocketManager>();
        if (webSocketManager == null)
        {
            Debug.LogError("ZoneWebSocketManager not found in scene");
        }

        this.data = data;
        this.manager = manager;
        this.gridManager = gridManager;
        this.rectTransform = GetComponent<RectTransform>();

        if (this.rectTransform == null)
        {
            Debug.LogError("RectTransform component not found");
            return;
        }

        Vector2 cellSize = gridManager.GetCellSize();
        rectTransform.sizeDelta = new Vector2(
            cellSize.x * data.width,
            cellSize.y * data.height
        );

        Debug.Log($"[SPAWN] Data dimensions: width={data.width}, height={data.height}");
        Debug.Log($"[SPAWN] RectTransform dimensions: width={rectTransform.sizeDelta.x}, height={rectTransform.sizeDelta.y}");
        Debug.Log($"[SPAWN] Cell size: {cellSize}");

        ApplyColor();
    }

    private void Awake()
    {
        zoneImage = GetComponent<Image>();
        blurMaterial = new Material(Shader.Find("UI/Blur"));
        blurMaterial.SetFloat("_Size", 2.0f);
    }


    public void UpdateEditingState(bool isBeingEdited)
    {
        if (isBeingEdited && !isBlurred)
        {
            zoneImage.material = blurMaterial;
            isBlurred = true;
            StartCoroutine(AnimateBlur(0f, 2f, 0.3f));
        }
        else if (!isBeingEdited && isBlurred)
        {
            StartCoroutine(AnimateBlur(2f, 0f, 0.3f));
            isBlurred = false;
        }
    }

    private IEnumerator AnimateBlur(float startSize, float endSize, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float currentSize = Mathf.Lerp(startSize, endSize, t);
            blurMaterial.SetFloat("_Size", currentSize);
            yield return null;
        }

        blurMaterial.SetFloat("_Size", endSize);
        if (endSize == 0f)
        {
            zoneImage.material = null;
        }
    }


    private void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // V�rifier si au moins un des deux doigts est sur cet objet
            bool isTouchingThis = IsPointerOverUIObject(touchZero.position) || IsPointerOverUIObject(touchOne.position);

            if (!isTouchingThis)
            {
                isPinching = false;
                return;
            }

            // D�but du pinch
            if (!isPinching)
            {
                isPinching = true;
                isDragging = false;
                initialPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
                // Stocker les dimensions de grille actuelles
                initialPinchSize = new Vector2Int(data.width, data.height);
            }

            // Calculer le changement de taille bas� sur la grille
            float currentPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
            float scaleDelta = (currentPinchDistance - initialPinchDistance) / 20; // R�duire la sensibilit�

            // Calculer les nouvelles dimensions en unit�s de grille
            int newGridWidth = Mathf.Max(1, initialPinchSize.x + Mathf.RoundToInt(scaleDelta));
            int newGridHeight = Mathf.Max(1, initialPinchSize.y + Mathf.RoundToInt(scaleDelta));

            // Appliquer les changements uniquement si les dimensions de grille changent
            if (newGridWidth != data.width || newGridHeight != data.height)
            {
                data.width = newGridWidth;
                data.height = newGridHeight;

                // Appliquer les dimensions en pixels bas�es sur la taille des cellules
                Vector2 cellSize = gridManager.GetCellSize();
                rectTransform.sizeDelta = new Vector2(
                    cellSize.x * data.width,
                    cellSize.y * data.height
                );

                if (webSocketManager != null)
                {
                    webSocketManager.EmitZoneUpdated(data);
                }
            }

            if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
            {
                isPinching = false;
            }
        }
        else
        {
            isPinching = false;
        }
    }

    private bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == gameObject)
            {
                return true;
            }
        }

        return false;
    }

    // Ajouter ces m�thodes � la classe ZoneControllerPrefab

    public void AssignWaiter(WaiterData waiter)
    {
        if (data.assignedServers == null)
        {
            data.assignedServers = new List<WaiterData>();
        }

        if (!data.assignedServers.Exists(w => w.id == waiter.id))
        {
            data.assignedServers.Add(waiter);

            // �mettre la mise � jour via WebSocket si n�cessaire
            if (webSocketManager != null)
            {
                webSocketManager.EmitZoneUpdated(data);
            }
        }
    }

    public void UnassignWaiter(string waiterId)
    {
        if (data.assignedServers != null)
        {
            data.assignedServers.RemoveAll(w => w.id == waiterId);

            // �mettre la mise � jour via WebSocket si n�cessaire
            if (webSocketManager != null)
            {
                webSocketManager.EmitZoneUpdated(data);
            }
        }
    }



    public void OnPointerDown(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform is null in OnPointerDown");
            return;
        }

        if (!isPinching)
        {
            isDragging = true;
            lastGridPosition = rectTransform.anchoredPosition;
        }
    }

    public void UpdateColor(string newColor)
    {
        if (data != null)
        {
            data.color = newColor;
            Image image = GetComponent<Image>();
            if (image != null)
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(newColor, out color))
                {
                    image.color = new Color(color.r, color.g, color.b, 0.5f);
                }
            }
        }
    }

    private void ApplyColor()
    {
        Image image = GetComponent<Image>();
        if (image != null && data.color != null)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString(data.color, out newColor))
            {
                image.color = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && !isPinching)
        {
            HandleDrag(eventData);
        }
    }

    private void HandleDrag(PointerEventData eventData)
    {
        // V�rifier que toutes les r�f�rences n�cessaires sont pr�sentes
        if (rectTransform == null || gridManager == null)
        {
            Debug.LogError("Missing references in HandleDrag");
            return;
        }

        Vector2 localPoint;
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError("Parent RectTransform is null");
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            Vector2Int gridPos = gridManager.GetGridPosition(localPoint);
            Vector2 newWorldPos = gridManager.GetWorldPosition(gridPos);

            if (newWorldPos != lastGridPosition)
            {
                rectTransform.anchoredPosition = newWorldPos;
                lastGridPosition = newWorldPos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rectTransform == null || gridManager == null || data == null)
        {
            Debug.LogError("Missing references in OnEndDrag");
            return;
        }

        isDragging = false;

        // Snapper � la grille
        Vector2Int finalGridPos = gridManager.GetGridPosition(rectTransform.anchoredPosition);
        Vector2 finalWorldPos = gridManager.GetWorldPosition(finalGridPos);
        rectTransform.anchoredPosition = finalWorldPos;
        data.position = finalGridPos;
        if (webSocketManager != null)
        {
            webSocketManager.EmitZoneUpdated(data);
        }
    }

    public void OnDelete()
    {
        manager.DeleteZone(data.id);
    }
}
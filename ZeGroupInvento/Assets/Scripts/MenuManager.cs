using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform backgroundPanel;

    private void Start()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (backgroundPanel == null)
        {
            backgroundPanel = canvas.GetComponentInChildren<RectTransform>();
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name;
            button.onClick.AddListener(() => SpawnPrefab(buttonName));
        }
    }

    public void SpawnPrefab(string prefabName)
    {
        GameObject prefabToSpawn = System.Array.Find(prefabs, p => p.name == prefabName);
        if (prefabToSpawn != null)
        {
            GameObject spawnedObject = Instantiate(prefabToSpawn, canvas.transform);
            RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;

            // Ajouter le component UIDragAndResize avec le bon paramètre de redimensionnement
            UIDragAndResize dragAndResize = spawnedObject.AddComponent<UIDragAndResize>();
            bool isTable = prefabName.Contains("x2CircleTable") || prefabName.Contains("x2RectangleTable");
            dragAndResize.SetResizable(!isTable);

            spawnedObject.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning($"Prefab with name {prefabName} not found!");
        }
    }

    private GameObject FindPrefabByName(string name)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.name.ToLower().Contains(name.ToLower()))
            {
                return prefab;
            }
        }
        return null;
    }
}

public class UIDragAndResize : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [SerializeField] private float handleSize = 40f;
    private bool isResizable = true;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas canvas;
    private bool isDragging = false;
    private Vector2 startPointerPosition;
    private Vector2 startAnchoredPosition;
    private Vector2 startSize;
    private Vector2 originalPosition;
    private Vector2 originalSize;

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
    }

    public void SetResizable(bool resizable)
    {
        isResizable = resizable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        if (isDragging)
        {
            Vector2 difference = eventData.position - startPointerPosition;
            Vector2 proposedPosition = startAnchoredPosition + difference / canvas.scaleFactor;

            float halfWidth = rectTransform.rect.width / 2;
            float halfHeight = rectTransform.rect.height / 2;
            float parentHalfWidth = parentRectTransform.rect.width / 2;
            float parentHalfHeight = parentRectTransform.rect.height / 2;

            if (proposedPosition.x + halfWidth > parentHalfWidth ||
                proposedPosition.x - halfWidth < -parentHalfWidth)
            {
                proposedPosition.x = rectTransform.anchoredPosition.x;
            }

            if (proposedPosition.y + halfHeight > parentHalfHeight ||
                proposedPosition.y - halfHeight < -parentHalfHeight)
            {
                proposedPosition.y = rectTransform.anchoredPosition.y;
            }

            rectTransform.anchoredPosition = proposedPosition;
        }
        else if (currentHandle != DragHandle.None && isResizable)
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

            // Appliquer les changements seulement si les nouvelles dimensions sont valides
            if (newSize.x > 0.1f && newSize.y > 0.1f)
            {
                rectTransform.sizeDelta = newSize;
                rectTransform.anchoredPosition = newPosition;
            }
        }
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
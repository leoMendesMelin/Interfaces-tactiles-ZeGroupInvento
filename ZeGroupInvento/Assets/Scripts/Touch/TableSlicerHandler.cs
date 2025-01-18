using UnityEngine;
using UnityEngine.UI;

public class TableSliceHandler : MonoBehaviour
{
    private const float MINIMUM_SLICE_DISTANCE = 100f;
    private ElementDragHandler dragHandler;
    private RoomElement elementData;
    private RoomManager roomManager;
    private RoomNetworkService networkService;
    private RectTransform rectTransform;
    private Vector2 sliceStart;
    private bool isSlicing;
    private int sliceTouchId = -1;

    private void Awake()
    {
        dragHandler = GetComponent<ElementDragHandler>();
        roomManager = RoomManager.Instance;
        networkService = FindObjectOfType<RoomNetworkService>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(RoomElement element)
    {
        elementData = element;
    }

    void Update()
    {
        if (elementData == null || dragHandler == null || dragHandler.IsDragging) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (dragHandler.IsUsingTouch(touch.fingerId)) continue;

            Vector2 touchPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touch.position, Camera.main, out touchPos);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!isSlicing && sliceTouchId == -1)
                    {
                        sliceStart = touchPos;
                        isSlicing = true;
                        sliceTouchId = touch.fingerId;
                    }
                    break;

                case TouchPhase.Moved:
                    if (isSlicing && touch.fingerId == sliceTouchId)
                    {
                        float distance = Vector2.Distance(touchPos, sliceStart);
                        if (distance >= MINIMUM_SLICE_DISTANCE && IsSlicingTable(sliceStart, touchPos))
                        {
                            SplitTable();
                            isSlicing = false;
                            sliceTouchId = -1;
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == sliceTouchId)
                    {
                        isSlicing = false;
                        sliceTouchId = -1;
                    }
                    break;
            }
        }
    }

    private bool IsSlicingTable(Vector2 start, Vector2 end)
    {
        Rect tableRect = rectTransform.rect;
        return tableRect.Overlaps(new Rect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Abs(end.x - start.x),
            Mathf.Abs(end.y - start.y)
        ));
    }

    private void SplitTable()
    {
        if (elementData.type != "TABLE_RECT_4") return;

        elementData.type = "TABLE_RECT_2";
        Vector2Int currentPos = new Vector2Int(
            Mathf.RoundToInt(elementData.position.x),
            Mathf.RoundToInt(elementData.position.y)
        );

        Vector2Int newTablePos = currentPos + new Vector2Int(1, 0);
        roomManager.AddElement("TABLE_RECT_2", newTablePos, elementData.rotation);

        StartCoroutine(networkService.UpdateRoomElement(
            roomManager.GetCurrentRoom().id,
            elementData,
            (updatedRoom) => roomManager.OnRoomDataReceived(updatedRoom)
        ));
    }
}
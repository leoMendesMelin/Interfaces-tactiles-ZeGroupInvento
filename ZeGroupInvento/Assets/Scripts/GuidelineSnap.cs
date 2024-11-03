using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIDragAndResize))]
public class GuidelineSnap : MonoBehaviour
{
    [SerializeField] private float snapThreshold = 20f;
    private RectTransform rectTransform;
    private GuidelineManager guidelineManager;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        guidelineManager = FindObjectOfType<GuidelineManager>();
    }

    private void LateUpdate()
    {
        if (guidelineManager != null)
        {
            Vector2 newPos = guidelineManager.TrySnapToGuidelines(rectTransform);
            if (newPos != rectTransform.anchoredPosition)
            {
                rectTransform.anchoredPosition = newPos;
            }
        }
    }
}
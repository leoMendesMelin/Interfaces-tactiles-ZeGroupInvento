using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GuidelineManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject verticalGuidelinePrefab;
    [SerializeField] private GameObject horizontalGuidelinePrefab;

    [Header("Settings")]
    [SerializeField] private float snapThreshold = 20f;

    private RectTransform canvasRect;
    private List<RectTransform> verticalGuidelines = new List<RectTransform>();
    private List<RectTransform> horizontalGuidelines = new List<RectTransform>();

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
    }

    public void CreateVerticalGuideline()
    {
        GameObject guideline = Instantiate(verticalGuidelinePrefab, transform);
        RectTransform guidelineRect = guideline.GetComponent<RectTransform>();

        guidelineRect.anchorMin = new Vector2(0.5f, 0);
        guidelineRect.anchorMax = new Vector2(0.5f, 1);
        guidelineRect.pivot = new Vector2(0.5f, 0.5f);
        guidelineRect.sizeDelta = new Vector2(2f, 0);
        guidelineRect.anchoredPosition = Vector2.zero;

        verticalGuidelines.Add(guidelineRect);

    }

    public void CreateHorizontalGuideline()
    {
        GameObject guideline = Instantiate(horizontalGuidelinePrefab, transform);
        RectTransform guidelineRect = guideline.GetComponent<RectTransform>();

        guidelineRect.anchorMin = new Vector2(0, 0.5f);
        guidelineRect.anchorMax = new Vector2(1, 0.5f);
        guidelineRect.pivot = new Vector2(0.5f, 0.5f);
        guidelineRect.sizeDelta = new Vector2(0, 2f);
        guidelineRect.anchoredPosition = Vector2.zero;

        horizontalGuidelines.Add(guidelineRect);

    }

    public Vector2 TrySnapToGuidelines(RectTransform objectRect)
    {
        Vector2 currentPos = objectRect.anchoredPosition;
        Vector2 snappedPos = currentPos;

        // Obtenir les bords de l'objet
        float objectLeft = currentPos.x - objectRect.rect.width * 0.5f;
        float objectRight = currentPos.x + objectRect.rect.width * 0.5f;
        float objectTop = currentPos.y + objectRect.rect.height * 0.5f;
        float objectBottom = currentPos.y - objectRect.rect.height * 0.5f;
        float objectCenter = currentPos.x;
        float objectMiddle = currentPos.y;

        // Vérifier le snap vertical
        foreach (RectTransform guideline in verticalGuidelines)
        {
            if (guideline == null) continue;

            float guidelineX = guideline.anchoredPosition.x;

            // Snap au centre
            if (Mathf.Abs(objectCenter - guidelineX) < snapThreshold)
            {
                snappedPos.x = guidelineX;
            }
            // Snap aux bords
            else if (Mathf.Abs(objectLeft - guidelineX) < snapThreshold)
            {
                snappedPos.x = guidelineX + objectRect.rect.width * 0.5f;
            }
            else if (Mathf.Abs(objectRight - guidelineX) < snapThreshold)
            {
                snappedPos.x = guidelineX - objectRect.rect.width * 0.5f;
            }
        }

        // Vérifier le snap horizontal
        foreach (RectTransform guideline in horizontalGuidelines)
        {
            if (guideline == null) continue;

            float guidelineY = guideline.anchoredPosition.y;

            // Snap au milieu
            if (Mathf.Abs(objectMiddle - guidelineY) < snapThreshold)
            {
                snappedPos.y = guidelineY;
            }
            // Snap aux bords
            else if (Mathf.Abs(objectTop - guidelineY) < snapThreshold)
            {
                snappedPos.y = guidelineY - objectRect.rect.height * 0.5f;
            }
            else if (Mathf.Abs(objectBottom - guidelineY) < snapThreshold)
            {
                snappedPos.y = guidelineY + objectRect.rect.height * 0.5f;
            }
        }

        return snappedPos;
    }

    public void RemoveGuideline(RectTransform guideline)
    {
        verticalGuidelines.Remove(guideline);
        horizontalGuidelines.Remove(guideline);
    }
}
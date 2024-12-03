using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System;
using TMPro; // Utilisation de TextMeshPro au lieu de Legacy Text

public class GridManager : MonoBehaviour
{
    private RectTransform backgroundPanel;
    private Vector2 cellSize;
    private const float LINE_WIDTH = 2f;

    public void Initialize(RectTransform panel)
    {
        backgroundPanel = panel;
    }

    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        float cellWidth = backgroundPanel.rect.width / RoomManager.Instance.GetCurrentRoom().gridSize;
        float cellHeight = backgroundPanel.rect.height / RoomManager.Instance.GetCurrentRoom().gridSize;

        int gridX = Mathf.FloorToInt((screenPosition.x + backgroundPanel.rect.width / 2) / cellWidth);
        int gridY = Mathf.FloorToInt((screenPosition.y + backgroundPanel.rect.height / 2) / cellHeight);

        return new Vector2Int(gridX, gridY);
    }

    public void CreateGrid(int gridSize)
    {
        cellSize = new Vector2(
            backgroundPanel.rect.width / gridSize,
            backgroundPanel.rect.height / gridSize
        );

        float startX = -backgroundPanel.rect.width / 2;
        float startY = -backgroundPanel.rect.height / 2;

        for (int i = 0; i <= gridSize; i++)
        {
            CreateLine(true, i, startX, startY);
            CreateLine(false, i, startX, startY);
        }
    }

    private void CreateLine(bool isVertical, int index, float startX, float startY)
    {
        GameObject line = new GameObject(isVertical ? $"VerticalLine_{index}" : $"HorizontalLine_{index}");
        line.transform.SetParent(transform, false);

        Image lineImage = line.AddComponent<Image>();
        lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        RectTransform rectTransform = line.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        if (isVertical)
        {
            rectTransform.anchoredPosition = new Vector2(startX + (index * cellSize.x), 0);
            rectTransform.sizeDelta = new Vector2(LINE_WIDTH, backgroundPanel.rect.height);
        }
        else
        {
            rectTransform.anchoredPosition = new Vector2(0, startY + (index * cellSize.y));
            rectTransform.sizeDelta = new Vector2(backgroundPanel.rect.width, LINE_WIDTH);
        }
    }

    public Vector2 GetWorldPosition(Vector2Int gridPosition)
    {
        float startX = -backgroundPanel.rect.width / 2;
        float startY = -backgroundPanel.rect.height / 2;
        return new Vector2(
            startX + (gridPosition.x * cellSize.x),
            startY + (gridPosition.y * cellSize.y)
        );
    }
}
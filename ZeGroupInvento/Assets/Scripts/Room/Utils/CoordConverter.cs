using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoordConverter
{
    public static Vector2 ScreenToGridPosition(Vector2 screenPosition, RectTransform backgroundPanel, int gridSize)
    {
        float cellWidth = backgroundPanel.rect.width / gridSize;
        float cellHeight = backgroundPanel.rect.height / gridSize;

        float startX = -backgroundPanel.rect.width / 2;
        float startY = -backgroundPanel.rect.height / 2;

        float gridX = (screenPosition.x - startX) / cellWidth;
        float gridY = (screenPosition.y - startY) / cellHeight;

        return new Vector2(gridX, gridY);
    }
}
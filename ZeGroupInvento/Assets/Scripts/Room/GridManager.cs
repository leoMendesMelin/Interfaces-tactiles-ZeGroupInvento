using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System;
using TMPro; // Utilisation de TextMeshPro au lieu de Legacy Text

public class GridManager : MonoBehaviour
{
    private RectTransform backgroundPanel; // Référence au BackgroundPanel
    public GameObject elementPrefab;

    private Vector2 cellSize;
    private const float LINE_WIDTH = 2f;

    void Start()
    {
        // Récupérer le BackgroundPanel
        backgroundPanel = transform.parent.GetComponent<RectTransform>();
        if (backgroundPanel == null)
        {
            Debug.LogError("BackgroundPanel not found!");
            return;
        }

        StartCoroutine(FetchRoomData());
    }

    IEnumerator FetchRoomData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("http://localhost:9090/room"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                jsonResponse = jsonResponse.TrimStart('[').TrimEnd(']');
                Room room = JsonUtility.FromJson<Room>(jsonResponse);
                CreateGridAndElements(room);
            }
            else
            {
                Debug.LogError($"Error: {request.error}");
            }
        }
    }

    void CreateGridAndElements(Room room)
    {
        // Calculer la taille des cellules basée sur la taille du background
        cellSize = new Vector2(
            backgroundPanel.rect.width / room.gridSize,
            backgroundPanel.rect.height / room.gridSize
        );

        CreateGrid(room.gridSize);
        CreateElements(room.elements);
    }

    void CreateGrid(int gridSize)
    {
        float startX = -backgroundPanel.rect.width / 2;
        float startY = -backgroundPanel.rect.height / 2;

        // Création des lignes verticales
        for (int i = 0; i <= gridSize; i++)
        {
            CreateLine(true, i, gridSize, startX, startY);
        }

        // Création des lignes horizontales
        for (int i = 0; i <= gridSize; i++)
        {
            CreateLine(false, i, gridSize, startX, startY);
        }
    }

    void CreateLine(bool isVertical, int index, int gridSize, float startX, float startY)
    {
        GameObject line = new GameObject(isVertical ? $"VerticalLine_{index}" : $"HorizontalLine_{index}");
        line.transform.SetParent(transform, false);

        Image lineImage = line.AddComponent<Image>();
        lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris semi-transparent

        RectTransform rectTransform = line.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        if (isVertical)
        {
            float xPos = startX + (index * cellSize.x);
            rectTransform.anchoredPosition = new Vector2(xPos, 0);
            rectTransform.sizeDelta = new Vector2(LINE_WIDTH, backgroundPanel.rect.height);
        }
        else
        {
            float yPos = startY + (index * cellSize.y);
            rectTransform.anchoredPosition = new Vector2(0, yPos);
            rectTransform.sizeDelta = new Vector2(backgroundPanel.rect.width, LINE_WIDTH);
        }
    }

    void CreateElements(RoomElement[] elements)
    {
        float startX = -backgroundPanel.rect.width / 2;
        float startY = -backgroundPanel.rect.height / 2;

        foreach (var element in elements)
        {
            GameObject elementObj = Instantiate(elementPrefab, transform);
            RectTransform rectTransform = elementObj.GetComponent<RectTransform>();

            // Calculer la position en pixels
            float xPos = startX + (element.position.x * cellSize.x);
            float yPos = startY + (element.position.y * cellSize.y);

            // Positionner l'élément
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);

            // Ajouter un texte TMP pour identifier le type
            GameObject textObj = new GameObject("ElementText");
            textObj.transform.SetParent(elementObj.transform);

            // Utiliser TextMeshProUGUI au lieu de Text
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = element.type;
            tmpText.color = Color.black;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 12;

            RectTransform textRect = tmpText.GetComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(100, 30);
        }
    }
}


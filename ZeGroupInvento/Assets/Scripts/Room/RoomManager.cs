using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

// Gestionnaire principal pour synchroniser les données de la salle
public class RoomManager : MonoBehaviour
{
    private Room currentRoom;
    private const string SERVER_URL = "http://localhost:9090";
    private List<RoomElement> pendingElements = new List<RoomElement>();
    private GridManager gridManager;
    private GridUIManager gridUIManager;
    public static RoomManager Instance { get; private set; }

    private void Start()
    {
        var backgroundPanel = GameObject.Find("BackgroundPanel").GetComponent<RectTransform>();
        gridManager = FindObjectOfType<GridManager>();
        gridUIManager = FindObjectOfType<GridUIManager>();

        gridManager.Initialize(backgroundPanel);
        gridUIManager.Initialize(backgroundPanel);
        StartCoroutine(FetchRoomData());
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FetchRoomData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/room"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                jsonResponse = jsonResponse.TrimStart('[').TrimEnd(']');
                currentRoom = JsonUtility.FromJson<Room>(jsonResponse);

                // Créer la grille et afficher les éléments
                gridManager.CreateGrid(currentRoom.gridSize);
                gridUIManager.DisplayElements(currentRoom.elements);
            }
        }
    }

    public void AddElement(string type, Vector2Int gridPosition, float rotation)
    {
        RoomElement newElement = new RoomElement
        {
            id = Guid.NewGuid().ToString(),
            type = type,
            position = new Position { x = gridPosition.x, y = gridPosition.y },
            rotation = rotation
        };

        pendingElements.Add(newElement);
        StartCoroutine(UpdateRoomElements());
    }

    private IEnumerator UpdateRoomElements()
    {
        if (currentRoom == null || pendingElements.Count == 0)
            yield break;

        var allElements = new List<RoomElement>(currentRoom.elements);
        allElements.AddRange(pendingElements);

        var updateData = new Room
        {
            id = currentRoom.id,
            gridSize = currentRoom.gridSize,
            elements = allElements.ToArray()
        };

        currentRoom = updateData; // Mettre à jour la room locale
        pendingElements.Clear();

        // Mettre à jour l'UI avec tous les éléments
        gridUIManager.DisplayElements(currentRoom.elements);
    }

    public Room GetCurrentRoom() => currentRoom;
}
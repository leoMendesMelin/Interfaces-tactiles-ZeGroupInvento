using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

// Gestionnaire principal pour synchroniser les données de la salle
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    private Room currentRoom;
    private GridManager gridManager;
    private GridUIManager gridUIManager;

    [SerializeField] private RoomNetworkService networkService;

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

    private void Start()
    {
        var backgroundPanel = GameObject.Find("BackgroundPanel").GetComponent<RectTransform>();
        gridManager = FindObjectOfType<GridManager>();
        gridUIManager = FindObjectOfType<GridUIManager>();

        gridManager.Initialize(backgroundPanel);
        gridUIManager.Initialize(backgroundPanel);

        // Charger les données initiales
        StartCoroutine(networkService.FetchRoom(OnRoomDataReceived));
    }

    public void OnRoomDataReceived(Room room)
    {
        currentRoom = room;
        gridManager.CreateGrid(currentRoom.gridSize);
        gridUIManager.DisplayElements(currentRoom.elements);
    }

    public void AddElement(string type, Vector2Int gridPosition, float rotation)
    {

        //appel validatePosition, et ensuite on attribue ça a newElement
        // Créer le nouvel élément
        RoomElement newElement = new RoomElement
        {
            id = System.Guid.NewGuid().ToString(),
            type = type,
            position = new Position { x = gridPosition.x, y = gridPosition.y },
            rotation = rotation,
            isBeingEdited = false
        };

        // Au lieu de créer l'UI immédiatement, on le fait seulement après confirmation du serveur
        StartCoroutine(AddElementWithConfirmation(newElement));
    }

    private IEnumerator AddElementWithConfirmation(RoomElement newElement)
    {
        bool elementAdded = false;

        yield return StartCoroutine(networkService.AddRoomElement(
            currentRoom.id,
            newElement,
            (updatedRoom) => {
                // Si le serveur confirme l'ajout, on crée l'UI
                gridUIManager.CreateOrUpdateElementUI(newElement);
                elementAdded = true;
                OnRoomDataReceived(updatedRoom);
            }
        ));

        if (!elementAdded)
        {
            // Si l'élément n'a pas été ajouté, on peut afficher un message d'erreur
            Debug.LogWarning("L'élément n'a pas pu être ajouté au serveur");
        }
    }

    //Permet d'update l'UI d'un élément
    public void updateUIElement(RoomElement newElement)
    {
        if (newElement == null)
        {
            Debug.LogWarning("erreur element est null");
        }
        gridUIManager.CreateOrUpdateElementUI(newElement);

    }




    public bool ValidatePosition(Vector2Int position)
    {
        return gridUIManager.ValidatePosition(position) == position;
    }

    public Room GetCurrentRoom() => currentRoom;
}
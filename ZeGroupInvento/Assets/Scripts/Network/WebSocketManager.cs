using UnityEngine;
using System;
using Newtonsoft.Json;
using NativeWebSocket;
using System.Threading.Tasks;

public class WebSocketManager : MonoBehaviour
{
    private WebSocket webSocket;
    private const string WS_URL = "ws://localhost:9091";
    private RoomManager roomManager;
    private NotificationManager notificationManager;

    async void Start()
    {
        roomManager = RoomManager.Instance;
        await ConnectToServer();
        notificationManager = FindObjectOfType<NotificationManager>();
    }

    public async Task SendTableUpdateResponse(string requestId, bool approved, RoomElement[] tables)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var messageData = new
            {
                eventType = "tableUpdateResponse",
                requestId = requestId,
                approved = approved,
                tables = tables
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
        }
    }

    private async Task ConnectToServer()
    {
        webSocket = new WebSocket(WS_URL);

        webSocket.OnMessage += (bytes) =>
        {
            // Convertir les bytes en string
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to WebSocket server");
            if (roomManager.GetCurrentRoom() != null)
            {
                JoinRoom(roomManager.GetCurrentRoom().id);
            }
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError($"WebSocket Error détaillé: {e}");
            // Ajout des informations sur l'état de la connexion
            Debug.LogError($"WebSocket State: {webSocket.State}");
            Debug.LogError($"Attempting to connect to: {WS_URL}");
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Disconnected from WebSocket server");
        };

        // Connexion au serveur
        await webSocket.Connect();
    }

    public async void EmitElementDragStart(RoomElement element)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            element.isBeingEdited = true;
            var messageData = new
            {
                eventType = "elementDragStart",
                roomId = roomManager.GetCurrentRoom().id,
                element = element
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
        }
    }

    public async void EmitElementDragging(RoomElement element)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            element.isBeingEdited = true;

            var messageData = new
            {
                eventType = "elementDragging",
                roomId = roomManager.GetCurrentRoom().id,
                element = element
            
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
        }
    }

    public async void EmitElementDragEnd(RoomElement element)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            element.isBeingEdited = false;

            var messageData = new
            {
                eventType = "elementDragging",
                roomId = roomManager.GetCurrentRoom().id,
                element = element
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
        }
    }

    private async void JoinRoom(string roomId)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            var messageData = new
            {
                eventType = "joinRoom",
                roomId = roomId
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
        }
    }

    private void HandleMessage(string data)
    {
        try
        {
            // Au lieu d'utiliser dynamic, on crée une classe dédiée
            var message = JsonConvert.DeserializeObject<WebSocketMessage>(data);

            switch (message.eventType)
            {
                case "elementUpdated":
                    var elementMessage = JsonConvert.DeserializeObject<WebSocketElementMessage>(data);
                    roomManager.updateUIElement(elementMessage.element);
                    break;

                case "roomUpdated":
                    var roomMessage = JsonConvert.DeserializeObject<WebSocketRoomMessage>(data);
                    //roomManager.OnRoomDataReceived(roomMessage.room);
                    break;
                case "tableUpdateRequestBroadcast":
                    var requestMessage = JsonConvert.DeserializeObject<WebSocketTableUpdateRequestMessage>(data);
                    Debug.Log($"[Table Update Request] Received from {requestMessage.requestData.waiterName}");
                    Debug.Log($"[Table Update Request] Request ID: {requestMessage.requestData.requestId}");
                    Debug.Log($"[Table Update Request] Tables to update: {requestMessage.requestData.tables.Length}");

                    // Créer la notification
                    if (notificationManager != null)
                    {
                        notificationManager.CreateTableUpdateNotification(
                            requestMessage.requestData.waiterName,
                            requestMessage.requestData.requestId,
                            requestMessage.requestData.tables
                        );
                    }

                    break;
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling message: {e.Message}");
        }
    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (webSocket != null)
        {
            webSocket.DispatchMessageQueue();
        }
#endif
    }

    private void OnDestroy()
    {
        if (webSocket != null)
        {
            webSocket.Close();
        }
    }
}


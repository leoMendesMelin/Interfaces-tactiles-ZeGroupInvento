using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using NativeWebSocket;
using System.Threading.Tasks;

public class WaiterWebSocketManager : MonoBehaviour
{
    private WebSocket webSocket;
    private const string WS_URL = "ws://localhost:9096";
    private RoomManager roomManager;

    private void Start()
    {
        roomManager = RoomManager.Instance;
        ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        webSocket = new WebSocket(WS_URL);

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to Waiter WebSocket server");
            if (roomManager.GetCurrentRoom() != null)
            {
                JoinRoom(roomManager.GetCurrentRoom().id);
            }
        };

        webSocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError($"Waiter WebSocket Error: {e}");
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Disconnected from Waiter WebSocket server");
        };

        await webSocket.Connect();
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

    private void UpdateWaiterDisplay(Room room, List<WaiterData> nonAssignedWaiters)
    {
        // Extraire les waiters assignés des zones en évitant les doublons
        HashSet<string> processedWaiterIds = new HashSet<string>();
        List<WaiterData> assignedWaiters = new List<WaiterData>();
        Dictionary<string, Color> waiterColors = new Dictionary<string, Color>();

        if (room?.zones != null)
        {
            foreach (var zone in room.zones)
            {
                if (zone.assignedServers != null)
                {
                    Color zoneColor;
                    if (ColorUtility.TryParseHtmlString(zone.color, out zoneColor))
                    {
                        foreach (var waiter in zone.assignedServers)
                        {
                            if (!processedWaiterIds.Contains(waiter.id))
                            {
                                assignedWaiters.Add(waiter);
                                processedWaiterIds.Add(waiter.id);
                                waiterColors[waiter.id] = zoneColor;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"Found {assignedWaiters.Count} unique assigned waiters");
        Debug.Log($"Found {nonAssignedWaiters?.Count ?? 0} non-assigned waiters");

        // Mettre à jour les UI via le WaiterManager
        WaiterManager.Instance.UpdateWaiters(
            assignedWaiters,
            nonAssignedWaiters,
            waiterColors
        );
    }

    private void HandleMessage(string data)
    {
        try
        {
            var message = JsonConvert.DeserializeObject<WebSocketMessage>(data);
            Debug.Log($"Received message type: {message.eventType}");
            Debug.Log($"Raw message received: {data}");

            switch (message.eventType)
            {
                case "roomData":
                    var roomDataMessage = JsonConvert.DeserializeObject<WebSocketRoomDataMessage>(data);
                    if (roomDataMessage != null)
                    {
                        UpdateWaiterDisplay(roomDataMessage.room, roomDataMessage.nonAssignedWaiters);
                    }
                    break;

                case "roomUpdated":
                    var roomUpdatedMessage = JsonConvert.DeserializeObject<WebSocketRoomDataMessage>(data);
                    if (roomUpdatedMessage != null)
                    {
                        UpdateWaiterDisplay(roomUpdatedMessage.room, roomUpdatedMessage.nonAssignedWaiters);
                    }
                    break;

                case "waiterConnected":
                case "waiterDisconnected":
                    // Si besoin de gérer ces événements plus tard
                    var waiterMessage = JsonConvert.DeserializeObject<WebSocketWaiterMessage>(data);
                    if (waiterMessage?.waiter != null)
                    {
                        // Mettre à jour le statut du waiter spécifique
                        WaiterManager.Instance.UpdateWaiterStatus(waiterMessage.waiter.id, waiterMessage.waiter.status);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling message: {e.Message}\n{e.StackTrace}");
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

    private async void OnApplicationQuit()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }
}


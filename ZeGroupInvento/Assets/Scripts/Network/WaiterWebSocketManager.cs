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
                    if (roomDataMessage == null)
                    {
                        Debug.LogError("Failed to deserialize room data message");
                        return;
                    }

                    // Extraire les waiters assignés des zones
                    List<WaiterData> assignedWaiters = new List<WaiterData>();
                    if (roomDataMessage.room?.zones != null)
                    {
                        foreach (var zone in roomDataMessage.room.zones)
                        {
                            if (zone.assignedServers != null)
                            {
                                assignedWaiters.AddRange(zone.assignedServers);
                            }
                        }
                    }

                    Debug.Log($"Found {assignedWaiters.Count} assigned waiters");
                    Debug.Log($"Found {roomDataMessage.nonAssignedWaiters?.Count ?? 0} non-assigned waiters");

                    // Mettre à jour les UI via le WaiterManager
                    WaiterManager.Instance.UpdateWaiters(
                        assignedWaiters,
                        roomDataMessage.nonAssignedWaiters // On prend les non-assignés directement du message
                    );
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
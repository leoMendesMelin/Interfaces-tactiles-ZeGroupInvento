using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using Newtonsoft.Json;
using NativeWebSocket;
using System.Threading.Tasks;

public class ZoneWebSocketManager : MonoBehaviour
{
    private WebSocket webSocket;
    private const string WS_URL = "ws://localhost:9092";
    private RoomManager roomManager;
    private ZoneManager zoneManager;
    private HashSet<string> processingZones = new HashSet<string>();
    private WaiterManager waiterManager;


    private void Start()
    {
        roomManager = RoomManager.Instance;
        zoneManager = FindObjectOfType<ZoneManager>();
        waiterManager = FindObjectOfType<WaiterManager>();

        ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        webSocket = new WebSocket(WS_URL);

        webSocket.OnOpen += () =>
        {
            Debug.Log("Connected to Zone WebSocket server");
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
            Debug.LogError($"Zone WebSocket Error: {e}");
        };

        webSocket.OnClose += (e) =>
        {
            Debug.Log("Disconnected from Zone WebSocket server");
        };

        await webSocket.Connect();
    }

    public async void EmitZoneCreated(ZoneData zone)
    {
        if (webSocket?.State == WebSocketState.Open && !processingZones.Contains(zone.id))
        {
            processingZones.Add(zone.id);
            var messageData = new
            {
                eventType = "zoneCreated",
                roomId = roomManager.GetCurrentRoom().id,
                zone = zone
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
            processingZones.Remove(zone.id);
        }
    }

    public async void EmitZoneUpdated(ZoneData zone)
    {
        if (webSocket?.State == WebSocketState.Open && !processingZones.Contains(zone.id))
        {
            processingZones.Add(zone.id);
            var messageData = new
            {
                eventType = "zoneUpdated",
                roomId = roomManager.GetCurrentRoom().id,
                zone = zone
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
            processingZones.Remove(zone.id);
        }
    }

    public async void EmitZoneDeleted(ZoneData zone)
    {
        if (webSocket?.State == WebSocketState.Open && !processingZones.Contains(zone.id))
        {
            processingZones.Add(zone.id);
            var messageData = new
            {
                eventType = "zoneDeleted",
                roomId = roomManager.GetCurrentRoom().id,
                zone = zone
            };
            string json = JsonConvert.SerializeObject(messageData);
            await webSocket.SendText(json);
            processingZones.Remove(zone.id);
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
            var message = JsonConvert.DeserializeObject<WebSocketMessage>(data);

            switch (message.eventType)
            {
                case "zoneCreated":
                    var zoneCreatedMsg = JsonConvert.DeserializeObject<WebSocketZoneMessage>(data);
                    if (zoneCreatedMsg.zone != null && !processingZones.Contains(zoneCreatedMsg.zone.id))
                    {
                        processingZones.Add(zoneCreatedMsg.zone.id);
                        zoneManager.UpdateZone(zoneCreatedMsg.zone);
                        processingZones.Remove(zoneCreatedMsg.zone.id);
                    }
                    break;

                case "zoneUpdated":
                    var zoneUpdatedMsg = JsonConvert.DeserializeObject<WebSocketZoneMessage>(data);
                    if (zoneUpdatedMsg.zone != null && !processingZones.Contains(zoneUpdatedMsg.zone.id))
                    {
                        processingZones.Add(zoneUpdatedMsg.zone.id);
                        zoneManager.UpdateZone(zoneUpdatedMsg.zone);
                        processingZones.Remove(zoneUpdatedMsg.zone.id);
                    }
                    break;

                case "zoneDeleted":
                    var zoneDeletedMsg = JsonConvert.DeserializeObject<WebSocketZoneMessage>(data);
                    if (zoneDeletedMsg.zone != null && !processingZones.Contains(zoneDeletedMsg.zone.id))
                    {
                        processingZones.Add(zoneDeletedMsg.zone.id);
                        zoneManager.DeleteZone(zoneDeletedMsg.zone.id);
                        processingZones.Remove(zoneDeletedMsg.zone.id);
                    }
                    break;

                case "roomUpdated":
                    var roomMessage = JsonConvert.DeserializeObject<WebSocketRoomMessage>(data);
                    if (roomMessage.room?.zones != null)
                    {
                        foreach (var zone in roomMessage.room.zones)
                        {
                            if (!processingZones.Contains(zone.id))
                            {
                                processingZones.Add(zone.id);
                                zoneManager.UpdateZone(zone);
                                processingZones.Remove(zone.id);
                            }
                        }

                        // Extraire les serveurs assignés
                        List<WaiterData> assignedWaiters = new List<WaiterData>();
                        foreach (var zone in roomMessage.room.zones)
                        {
                            if (zone.assignedServers != null)
                            {
                                assignedWaiters.AddRange(zone.assignedServers);
                            }
                        }
                        if (waiterManager != null)
                        {
                            waiterManager.UpdateWaiters(
                                assignedWaiters,
                                roomMessage.nonAssignedWaiters // Les waiters non assignés viennent du serveur
                            );
                        }
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling message: {e.Message}");
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

[Serializable]
public class WebSocketZoneMessage : WebSocketMessage
{
    public ZoneData zone;
}
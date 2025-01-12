// Mettre à jour SocketObjects.cs
using System;
using System.Collections.Generic;

[Serializable]
public class WebSocketMessage
{
    public string eventType;
}

[Serializable]
public class WebSocketElementMessage : WebSocketMessage
{
    public RoomElement element;
}

[Serializable]
public class WebSocketRoomMessage : WebSocketMessage
{
    public Room room;
    public List<WaiterData> nonAssignedWaiters;
}

[Serializable]
public class WebSocketRoomDataMessage : WebSocketMessage
{
    public Room room;
    public List<WaiterData> nonAssignedWaiters;
}

[Serializable]
public class ZoneDataArray
{
    public ZoneData[] zones;
}

// Ajouter cette classe si elle n'existe pas déjà
[Serializable]
public class WebSocketWaiterMessage : WebSocketMessage
{
    public WaiterData waiter;
}

[Serializable]
public class TableUpdateRequestData
{
    public string requestId;
    public string waiterName;
    public RoomElement[] tables;
}

[Serializable]
public class WebSocketTableUpdateRequestMessage : WebSocketMessage
{
    public TableUpdateRequestData requestData;
}

[Serializable]
public class WebSocketTableUpdateResponseMessage : WebSocketMessage
{
    public bool approved;
    public string requestId;
    public RoomElement[] tables;
}
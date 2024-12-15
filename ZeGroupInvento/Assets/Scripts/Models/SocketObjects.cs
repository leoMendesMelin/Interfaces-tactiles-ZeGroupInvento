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
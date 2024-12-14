using System;
// Classes pour la d�s�rialisation des messages
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
public class ZoneDataArray
{
    public ZoneData[] zones;
}
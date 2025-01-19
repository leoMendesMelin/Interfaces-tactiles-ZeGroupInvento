using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using UnityEngine;


[Serializable]
public class Position
{
    public float x;
    public float y;
}

[Serializable]
public class RoomElement
{
    public string id;
    public string type;
    public bool isBeingEdited;
    public Position position;
    public float rotation;
    public string state;
}

[Serializable]
public class Room
{
    public string id;
    public int gridSize;
    public RoomElement[] elements;
    public ZoneData[] zones; // Ajout de la propriété zones
}




[Serializable]
public class ZoneData
{
    public string id;
    public string name;
    public string color;
    public Vector2Int position;
    public int width;
    public int height;
    public List<WaiterData> assignedServers;
    public List<Vector2Int> bounds;
    public bool isBeingEdited;
}
[Serializable]
public class WaiterData
{
    public string id;
    public string name;
    public string status;
}

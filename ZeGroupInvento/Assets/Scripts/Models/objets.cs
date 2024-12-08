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
}

[Serializable]
public class Room
{
    public string id;
    public int gridSize;
    public RoomElement[] elements;
}


[System.Serializable]
public class ZoneData
{
    public string id;
    public string name;
    public Color color;
    public Vector2Int position; // Position sur la grille
    public Vector2Int size;     // Taille en nombre de cellules (i, j)
    public List<string> assignedServers;

    public ZoneData(string id, Color color, Vector2Int position)
    {
        this.id = id;
        this.color = color;
        this.position = position;
        this.size = new Vector2Int(1, 1); // Taille initiale
        this.assignedServers = new List<string>();
        this.name = "Zone " + id;
    }
}
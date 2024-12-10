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




[Serializable]
public class ZoneData
{
    public string id;
    public string name;
    public string color;
    public Vector2Int position; // Position sur la grille
    public int width; // Taille horizontale
    public int height; // Taille verticale
}
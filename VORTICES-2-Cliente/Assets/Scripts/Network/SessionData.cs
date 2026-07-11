using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SessionData
{
    public string sessionName;
    public int userId;
    public string environmentName;
    public bool isOnlineSession;
    public string displayMode; // Opcional (Plane, Radial, etc.)
    public string browsingMode; // Siempre Online
    public bool volumetric; // Opcional
    public Vector3Int dimension; // X, Y, Z
    public List<string> elementPaths; // Rutas o URLs
    public List<string> categories; // Categor�as seleccionadas
    // Parámetros del laberinto — sincronizados desde el creador de la sesión
    public string skinName;
    public bool   noCeiling;
    public int    gridWidth;
    public int    gridHeight;
    public int    maxTotems;
}




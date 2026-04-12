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
    public List<string> categories; // Categorías seleccionadas
}

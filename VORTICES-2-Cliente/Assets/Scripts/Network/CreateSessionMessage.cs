using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct CreateSessionMessage : NetworkMessage
{
    public string sessionName;
    public int userId;
    public string environmentName;
    public bool isOnlineSession;
    public string displayMode; // Opcional
    public string browsingMode;
    public bool volumetric;
    public Vector3Int dimension;
    public List<string> elementPaths;
    public List<string> categories; // Categorías seleccionadas
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct RequestActiveSessionMessage : NetworkMessage
{
}

public struct ActiveSessionResponseMessage : NetworkMessage
{
    public bool success;
    public SessionData sessionData;
}

public struct SessionEndedMessage : NetworkMessage
{
    public string reason; // Raz�n de la finalizaci�n de la sesi�n
}

public struct ChatMessage : NetworkMessage
{
    public string content;
}


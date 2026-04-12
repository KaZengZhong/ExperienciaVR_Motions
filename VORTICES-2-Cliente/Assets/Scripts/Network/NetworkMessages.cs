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

public struct ChatMessage : NetworkMessage
{
    public string senderName;
    public string message;
}

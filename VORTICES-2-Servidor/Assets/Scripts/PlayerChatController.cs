using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;

public class PlayerChatController : NetworkBehaviour
{
    private SessionManager sessionManager;

    private void Start()
    {
        sessionManager = FindObjectOfType<SessionManager>();

        if (sessionManager == null)
        {
            Debug.LogError("[PlayerChatController] SessionManager no encontrado en la escena.");
        }

        DontDestroyOnLoad(gameObject);
    }

    [Command]
    public void CmdSendMessageToChat(string userId, string message)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[PlayerChatController] UserID no configurado o inválido.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("[PlayerChatController] Mensaje vacío, no se puede enviar.");
            return;
        }

        // Buscar el ChatCanvas global en el servidor
        GameObject chatCanvas = GameObject.Find("ChatCanvas(Clone)");
        if (chatCanvas == null)
        {
            Debug.LogError("[PlayerChatController] ChatCanvas no encontrado en el servidor.");
            return;
        }

        // Obtener el NewChatManager
        NewChatManager chatManager = chatCanvas.GetComponent<NewChatManager>();
        if (chatManager == null)
        {
            Debug.LogError("[PlayerChatController] NewChatManager no encontrado en ChatCanvas.");
            return;
        }

        // Retransmitir el mensaje a todos los clientes
        chatManager.RpcReceiveMessage(userId, message);
    }


}



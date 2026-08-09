using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;

public class PlayerChatController : NetworkBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartLocalPlayer()
    {
        VivoxVoiceManager.OnLocalSpeakingChanged += HandleSpeakingChanged;
    }

    private void OnDestroy()
    {
        VivoxVoiceManager.OnLocalSpeakingChanged -= HandleSpeakingChanged;
    }

    private void HandleSpeakingChanged(bool speaking)
    {
        CmdSetSpeaking(speaking);
    }

    [Command]
    private void CmdSetSpeaking(bool speaking)
    {
        GetComponent<PlayerAvatarController>()?.SetSpeaking(speaking);
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

    // ── Sincronización de tótems ──────────────────────────────────────────────

    [Command]
    public void CmdTotemAnswered(Vector3 totemPosition, bool answeredReal, int cellX, int cellZ)
    {
        Debug.Log($"[PlayerChatController] Servidor recibió CmdTotemAnswered — pos={totemPosition}, real={answeredReal}");
        RpcTotemAnswered(totemPosition, answeredReal, cellX, cellZ);
    }

    [ClientRpc]
    private void RpcTotemAnswered(Vector3 totemPosition, bool answeredReal, int cellX, int cellZ)
    {
        Debug.Log($"[PlayerChatController] RPC recibido — buscando tótem en pos={totemPosition}");
        InformationTotem[] totems = FindObjectsOfType<InformationTotem>();
        Debug.Log($"[PlayerChatController] Tótems en escena: {totems.Length}");
        foreach (var totem in totems)
        {
            float dist = Vector3.Distance(totem.transform.position, totemPosition);
            Debug.Log($"[PlayerChatController]   tótem en {totem.transform.position}, distancia={dist:F3}");
            if (dist < 1f)
            {
                Debug.Log($"[PlayerChatController] Tótem encontrado — aplicando respuesta.");
                totem.ApplyNetworkAnswer(answeredReal, new Vector2Int(cellX, cellZ));
                return;
            }
        }
        Debug.LogWarning($"[PlayerChatController] No se encontró ningún tótem cerca de {totemPosition}");
    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;
using Vortices;


public class NewChatManager : NetworkBehaviour
{
    [Header("Chat UI Components")]
    public GameObject chatCanvas;
    public ScrollRect scrollRect;
    public TMP_InputField chatInputField;
    public TMP_Text chatDisplay;
    public Button sendButton;

    private void Start()
    {
        // Solo desactivar el ChatCanvas si no es el servidor
        if (!isServer)
        {
            chatCanvas.SetActive(false);
        }

        Debug.Log($"[NewChatManager] Inicializado en: {gameObject.name}. Es servidor: {isServer}");

        // Asegurar que el ChatCanvas no se destruya al cambiar de escena
        DontDestroyOnLoad(gameObject);

        // Evitar duplicados: Si ya hay un ChatCanvas en la escena, eliminamos este
        NewChatManager existingChat = FindObjectOfType<NewChatManager>();
        if (existingChat != null && existingChat != this)
        {
            Debug.LogWarning("[NewChatManager] Ya existe un ChatCanvas, eliminando instancia duplicada.");
            Destroy(gameObject);
            return;
        }

        // Vincular el botón de enviar
        sendButton.onClick.AddListener(OnSendButtonPressed);
    }


    public void ToggleChat()
    {
        // Alternar visibilidad del chat
        chatCanvas.SetActive(!chatCanvas.activeSelf);
        Debug.Log($"Chat {(chatCanvas.activeSelf ? "activado" : "desactivado")}");
    }

    // Llamado cuando se presiona el botón de enviar
    public void OnSendButtonPressed()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
        {
            Debug.LogWarning("[NewChatManager] No se puede enviar un mensaje vacío.");
            return;
        }

        string message = chatInputField.text;

        // Obtener el jugador local
        GameObject playerObject = NetworkClient.localPlayer?.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[NewChatManager] Objeto jugador local no encontrado.");
            return;
        }

        // Obtener el controlador de chat del jugador
        PlayerChatController playerChatController = playerObject.GetComponent<PlayerChatController>();
        if (playerChatController == null)
        {
            Debug.LogError("[NewChatManager] PlayerChatController no encontrado en el jugador.");
            return;
        }

        // Obtener el userId desde el SessionManager
        string userId = FindObjectOfType<SessionManager>()?.userId.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[NewChatManager] UserID no configurado.");
            return;
        }

        // Enviar el mensaje al servidor
        Debug.Log($"[NewChatManager] Enviando mensaje: {message} de userId: {userId}");
        playerChatController.CmdSendMessageToChat(userId, message);

        // Limpiar el campo de texto
        chatInputField.text = "";
    }





    [ClientRpc]
    public void RpcReceiveMessage(string userId, string message)
    {
        Debug.Log($"[NewChatManager] Mensaje recibido de {userId}: {message}");

        if (chatDisplay != null)
        {
            chatDisplay.text += $"Usuario {userId}: {message}\n";
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            Debug.LogError("[NewChatManager] chatDisplay no está asignado.");
        }
    }


}

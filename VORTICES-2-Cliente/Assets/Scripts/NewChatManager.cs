using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        // Forzar Screen Space Overlay para que el chat aparezca en pantalla en desktop
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            ToggleChat();

        if (Input.GetKeyDown(KeyCode.M))
            VivoxVoiceManager.Instance?.ToggleMute();

        if (Input.GetKeyDown(KeyCode.Return) && chatCanvas != null)
        {
            Canvas c = chatCanvas.GetComponent<Canvas>();
            if (c != null && c.enabled)
                OnSendButtonPressed();
        }
    }

    public void ToggleChat()
    {
        Canvas canvas = chatCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = !canvas.enabled;
            if (canvas.enabled)
                chatInputField?.ActivateInputField();
            Debug.Log($"Chat {(canvas.enabled ? "activado" : "desactivado")}");
        }
        else
        {
            chatCanvas.SetActive(!chatCanvas.activeSelf);
        }
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

        // Obtener userId desde session.json (no depende de SessionManager)
        string userId = GetUserIdFromSession();
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





    private string GetUserIdFromSession()
    {
        string path = Path.GetDirectoryName(Application.dataPath) + "/session.json";
        if (!File.Exists(path)) return "0";
        try
        {
            SessionData data = JsonUtility.FromJson<SessionData>(File.ReadAllText(path));
            return data.userId.ToString();
        }
        catch { return "0"; }
    }

    [System.Serializable]
    private class SessionData { public int userId = 0; }

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

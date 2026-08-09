using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Vortices;


public class NewChatManager : NetworkBehaviour
{
    [Header("Chat UI Components")]
    public GameObject chatCanvas;
    public ScrollRect scrollRect;
    public TMP_InputField chatInputField;
    public TMP_Text chatDisplay;
    public Button sendButton;
    public Button toggleChatButton;

    private InputAction _toggleChatXRAction;
    private InputAction _muteXRAction;

    private GameObject _muteIndicator;
    private TMP_Text _muteText;
    private bool _isMuted = false;
    private Coroutine _muteHideCoroutine;

    private void Awake()
    {
        _toggleChatXRAction = new InputAction(
            name: "ToggleChat",
            type: InputActionType.Button,
            binding: "<XRController>{RightHand}/secondaryButton");
        _toggleChatXRAction.Enable();

        _muteXRAction = new InputAction(
            name: "Mute",
            type: InputActionType.Button,
            binding: "<XRController>{LeftHand}/secondaryButton");
        _muteXRAction.Enable();
    }

    private void OnDestroy()
    {
        _toggleChatXRAction?.Disable();
        _toggleChatXRAction?.Dispose();
        _muteXRAction?.Disable();
        _muteXRAction?.Dispose();
    }

    private void Start()
    {
        Debug.Log($"[NewChatManager] Inicializado en: {gameObject.name}. Es servidor: {isServer}");

        DontDestroyOnLoad(gameObject);

        NewChatManager existingChat = FindObjectOfType<NewChatManager>();
        if (existingChat != null && existingChat != this)
        {
            Debug.LogWarning("[NewChatManager] Ya existe un ChatCanvas, eliminando instancia duplicada.");
            Destroy(gameObject);
            return;
        }

        sendButton.onClick.AddListener(OnSendButtonPressed);
        if (toggleChatButton != null)
            toggleChatButton.onClick.AddListener(ToggleChat);

        BuildMuteIndicator();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) ||
            (_toggleChatXRAction != null && _toggleChatXRAction.WasPressedThisFrame()))
            ToggleChat();

        if (Input.GetKeyDown(KeyCode.M) ||
            (_muteXRAction != null && _muteXRAction.WasPressedThisFrame()))
        {
            VivoxVoiceManager.Instance?.ToggleMute();
            _isMuted = !_isMuted;
            ShowMuteStatus(_isMuted);
        }

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
        GraphicRaycaster raycaster = chatCanvas.GetComponent<GraphicRaycaster>();
        bool opening = canvas == null || !canvas.enabled;

        if (opening)
        {
            Camera cam = Camera.main ?? FindObjectOfType<Camera>();
            if (cam != null)
            {
                chatCanvas.transform.position = cam.transform.position + cam.transform.forward * 1.5f;
                chatCanvas.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
                chatCanvas.transform.localScale = Vector3.one * 0.004f;
            }
            if (chatInputField != null) chatInputField.gameObject.SetActive(true);
            if (canvas != null) canvas.enabled = true;
            if (raycaster != null) raycaster.enabled = true;
            chatInputField?.ActivateInputField();
        }
        else
        {
            CloseChat();
        }
        Debug.Log($"Chat {(opening ? "activado" : "desactivado")}");
    }

    private void CloseChat()
    {
        StartCoroutine(CloseChatRoutine());
    }

    private IEnumerator CloseChatRoutine()
    {
        // Desactivar el input field — Meta necesita un frame para cerrar el teclado
        if (chatInputField != null) chatInputField.gameObject.SetActive(false);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        yield return null; // esperar un frame
        Canvas canvas = chatCanvas.GetComponent<Canvas>();
        GraphicRaycaster raycaster = chatCanvas.GetComponent<GraphicRaycaster>();
        if (canvas != null) canvas.enabled = false;
        if (raycaster != null) raycaster.enabled = false;
        yield return null; // esperar otro frame antes de reactivar
        if (chatInputField != null) chatInputField.gameObject.SetActive(true);
    }

    public void OnSendButtonPressed()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
        {
            Debug.LogWarning("[NewChatManager] No se puede enviar un mensaje vacío.");
            return;
        }

        string message = chatInputField.text;

        GameObject playerObject = NetworkClient.localPlayer?.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[NewChatManager] Objeto jugador local no encontrado.");
            return;
        }

        PlayerChatController playerChatController = playerObject.GetComponent<PlayerChatController>();
        if (playerChatController == null)
        {
            Debug.LogError("[NewChatManager] PlayerChatController no encontrado en el jugador.");
            return;
        }

        string userId = GetUserIdFromSession();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[NewChatManager] UserID no configurado.");
            return;
        }

        Debug.Log($"[NewChatManager] Enviando mensaje: {message} de userId: {userId}");
        playerChatController.CmdSendMessageToChat(userId, message);

        chatInputField.text = "";
    }

    private string GetUserIdFromSession()
    {
        string path = PlatformPaths.ConfigJson;
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

    private void BuildMuteIndicator()
    {
        var root = new GameObject("MuteIndicator", typeof(RectTransform));
        DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        root.transform.localScale = Vector3.one * 0.003f;

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(panel.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        _muteText = txtGO.AddComponent<TextMeshProUGUI>();
        _muteText.fontSize = 22; _muteText.color = Color.white;
        _muteText.alignment = TextAlignmentOptions.Center;

        root.SetActive(false);
        _muteIndicator = root;
    }

    private void ShowMuteStatus(bool muted)
    {
        if (_muteIndicator == null) return;
        _muteText.text = muted ? "Microfono silenciado" : "Microfono activo";
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            _muteIndicator.transform.position = cam.transform.position
                + cam.transform.forward * 1.5f
                + cam.transform.up * -0.25f;
            _muteIndicator.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        }
        _muteIndicator.SetActive(true);
        if (_muteHideCoroutine != null) StopCoroutine(_muteHideCoroutine);
        _muteHideCoroutine = StartCoroutine(HideMuteIndicator());
    }

    private IEnumerator HideMuteIndicator()
    {
        yield return new WaitForSeconds(2f);
        if (_muteIndicator != null) _muteIndicator.SetActive(false);
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

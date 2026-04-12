using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Vivox;
using System;
using System.Threading.Tasks;
#if AUTH_PACKAGE_PRESENT
using Unity.Services.Authentication;
#endif

public class VivoxVoiceManager : MonoBehaviour
{
    public const string LobbyChannelName = "VoRTIcESVoiceChat";

    private static VivoxVoiceManager _instance;

    [SerializeField] private string _key;
    [SerializeField] private string _issuer;
    [SerializeField] private string _domain;
    [SerializeField] private string _server;

    public static VivoxVoiceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VivoxVoiceManager>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<VivoxVoiceManager>();
                    singletonObject.name = typeof(VivoxVoiceManager).ToString() + " (Singleton)";
                }
            }
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != this && _instance != null)
        {
            Destroy(this);
        }

        InitializeVivoxService();
    }

    private async void InitializeVivoxService()
    {
        var options = new InitializationOptions();
        if (!string.IsNullOrEmpty(_server) && !string.IsNullOrEmpty(_domain) && !string.IsNullOrEmpty(_issuer) && !string.IsNullOrEmpty(_key))
        {
            options.SetVivoxCredentials(_server, _domain, _issuer, _key);
        }

        try
        {
            await UnityServices.InitializeAsync(options);
            Debug.Log("Unity Services and Vivox initialized.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize Unity Services or Vivox: {ex.Message}");
        }
    }

    public async Task LoginAsync(string playerName)
    {
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            Debug.LogError("Unity Services not initialized.");
            return;
        }

#if AUTH_PACKAGE_PRESENT
        try
        {
            AuthenticationService.Instance.SwitchProfile(playerName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Logged into Unity Authentication as {playerName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unity Authentication failed: {ex.Message}");
        }
#endif

        try
        {
            await VivoxService.Instance.LoginAsync();
            Debug.Log($"Logged into Vivox as {playerName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox login failed: {ex.Message}");
        }
    }

    public async Task JoinChannelAsync(string channelName)
    {
        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            Debug.Log($"Joined Vivox channel: {channelName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to join Vivox channel: {ex.Message}");
        }
    }

    public async Task LeaveChannelAsync(string channelName)
    {
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelName);
            Debug.Log($"Left Vivox channel: {channelName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to leave Vivox channel: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await VivoxService.Instance.LogoutAsync();
            Debug.Log("Logged out of Vivox.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to logout of Vivox: {ex.Message}");
        }
    }

    public async Task EnsureVivoxInitialized()
    {
        Debug.Log("[VoiceChat] Inicializando Vivox Service...");

        var options = new InitializationOptions();
        if (CheckManualCredentials())
        {
            options.SetVivoxCredentials(_server, _domain, _issuer, _key);
        }

        await UnityServices.InitializeAsync(options);
        await VivoxService.Instance.InitializeAsync();

        Debug.Log("[VoiceChat] Vivox Service inicializado correctamente.");
    }

    bool CheckManualCredentials()
    {
        return !(string.IsNullOrEmpty(_issuer) && string.IsNullOrEmpty(_domain) && string.IsNullOrEmpty(_server));
    }

}

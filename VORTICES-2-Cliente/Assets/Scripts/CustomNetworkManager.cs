using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{

    public GameObject chatCanvasPrefab;

    public override void Awake()
    {
        base.Awake();
        Debug.Log("CustomNetworkManager - Awake");
    }

    public override void Start()
    {
        base.Start();
        Debug.Log("CustomNetworkManager - Start");
        VivoxVoiceManager vivoxManager = FindObjectOfType<VivoxVoiceManager>();
        if (vivoxManager != null)
        {
            Debug.Log("[CustomNetworkManager] Inicializando VivoxVoiceManager...");
            StartCoroutine(InitializeVivox(vivoxManager));
        }
        else
        {
            Debug.LogError("[CustomNetworkManager] No se encontró VivoxVoiceManager en la escena.");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (chatCanvasPrefab != null)
        {
            // Crear el ChatCanvas
            GameObject chatInstance = Instantiate(chatCanvasPrefab);
            
            // Activar el ChatCanvas en el servidor
            chatInstance.SetActive(true);
            
            // Marcarlo como persistente entre escenas
            DontDestroyOnLoad(chatInstance);
            
            // Sincronizar con los clientes
            NetworkServer.Spawn(chatInstance);
            Debug.Log("[ChatCanvas] ChatCanvas global creado, activado y marcado como persistente.");
        }
        else
        {
            Debug.LogError("[ChatCanvas] ChatCanvasPrefab no está asignado.");
        }
    }



    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[OnServerAddPlayer] Añadiendo jugador con connId: {conn.connectionId}...");

        // Obtener el nombre de la escena actual
        string sceneName = SceneManager.GetActiveScene().name;

        // Si la escena es "Circular", NO instanciamos el PlayerPrefab
        if (sceneName == "Circular Environment")
        {
            Debug.Log("[OnServerAddPlayer] Escena Circular detectada, NO se instancia el Player.");
            return;
        }

        // Instanciar y añadir el jugador SOLO en la escena Museo
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        player.name = $"Player [connId={conn.connectionId}]";
        Debug.Log("[OnServerAddPlayer] Jugador instanciado: " + player.name);

        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log("[OnServerAddPlayer] Jugador añadido a la conexión del cliente.");

        // Buscar el ChatCanvas global
        GameObject chatCanvas = GameObject.FindWithTag("ChatCanvas");
        if (chatCanvas != null)
        {
            Debug.Log("[ChatCanvas] Se encontró el ChatCanvas en el servidor.");
        }
        else
        {
            Debug.LogError("[ChatCanvas] No se encontró el ChatCanvas en el servidor.");
        }
    }

    private IEnumerator InitializeVivox(VivoxVoiceManager vivoxManager)
    {
        yield return vivoxManager.EnsureVivoxInitialized(); // Asegúrate de inicializar correctamente
        Debug.Log("[CustomNetworkManager] VivoxVoiceManager inicializado correctamente.");
    }

}

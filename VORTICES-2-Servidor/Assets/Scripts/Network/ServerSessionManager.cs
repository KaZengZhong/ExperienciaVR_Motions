using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;
using Vortices;

public class ServerSessionManager : NetworkBehaviour
{
    // Diccionario para almacenar las sesiones activas
    private Dictionary<string, SessionData> activeSessions = new Dictionary<string, SessionData>();

    private static ServerSessionManager _instance;

    [SerializeField]
    public GameObject museumBaseNetworkPrefab;

    [SerializeField]
    public GameObject CircularNetworkPrefab;

    public static ServerSessionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("ServerSessionManager no est� en la escena.");
            }
            return _instance;
        }
    }

    private void Awake()
    {   
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

    }

    private void Start()
    {
        // Iniciar la rutina para monitorear conexiones activas
        StartCoroutine(MonitorClientConnections());
    }

    private IEnumerator MonitorClientConnections()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Revisa cada 5 segundos

            // Si no hay conexiones activas
            if (NetworkServer.connections.Count == 0 || !NetworkServer.connections.Any(c => c.Value.isAuthenticated))
            {
                //Debug.Log("Todos los clientes est�n desconectados.");
                HandleAllClientsDisconnected();
            }
        }
    }


    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Servidor iniciado y listo para recibir conexiones.");

        // Registrar handlers
        NetworkServer.RegisterHandler<CreateSessionMessage>(HandleCreateSessionMessage);
        NetworkServer.RegisterHandler<RequestActiveSessionMessage>(HandleRequestActiveSessionMessage);

    }


    #region Manejo de Sesiones

    private void HandleCreateSessionMessage(NetworkConnectionToClient conn, CreateSessionMessage msg)
    {

        if (activeSessions.ContainsKey(msg.sessionName))
        {
            Debug.LogWarning($"Sesi�n '{msg.sessionName}' ya existe.");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        if (msg.environmentName == "Museum")
        {
            msg.environmentName = "Museum Environment";
        }
        else if (msg.environmentName == "Circular")
        {
            msg.environmentName = "Circular Environment";
        }
        else
        {
            Debug.LogError($"Nombre de escena desconocido: {msg.environmentName}");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        var sessionData = new SessionData
        {
            sessionName = msg.sessionName,
            userId = msg.userId,
            environmentName = msg.environmentName,
            isOnlineSession = msg.isOnlineSession,
            displayMode = msg.displayMode,
            browsingMode = msg.browsingMode,
            volumetric = msg.volumetric,
            dimension = msg.dimension,
            categories = msg.categories ?? new List<string>(),
            elementPaths = msg.elementPaths ?? new List<string>()
        };
        activeSessions[msg.sessionName] = sessionData;

        conn.Send(new SessionCreatedMessage
        {
            success = true,
            sessionName = sessionData.sessionName,
            userId = sessionData.userId,
            environmentName = sessionData.environmentName,
            isOnlineSession = sessionData.isOnlineSession,
            displayMode = sessionData.displayMode,
            browsingMode = sessionData.browsingMode,
            volumetric = sessionData.volumetric,
            dimension = sessionData.dimension,
            categories = sessionData.categories,
            elementPaths = sessionData.elementPaths
        });

        Debug.Log("[Servidor] Inicializando NetworkHandler...");

        if (msg.environmentName == "Museum Environment")
        {
            Debug.Log("[Servidor] Creando MuseumBaseNetworkHandler para sincronización.");
            GameObject museumBaseNetwork = Instantiate(museumBaseNetworkPrefab);
            NetworkServer.Spawn(museumBaseNetwork);
            Debug.Log($"[Servidor] MuseumBaseNetworkHandler spawneado con Net ID: {museumBaseNetwork.GetComponent<NetworkIdentity>().netId}");
        }
        else if (msg.environmentName == "Circular Environment")
        {
            Debug.Log("[Servidor] Creando CircularNetworkHandler para sincronización.");
            GameObject circularNetwork = Instantiate(CircularNetworkPrefab);
            NetworkServer.Spawn(circularNetwork);
            Debug.Log($"[Servidor] CircularNetworkHandler spawneado con Net ID: {circularNetwork.GetComponent<NetworkIdentity>().netId}");
        }
        else
        {
            Debug.LogWarning("[Servidor] No se encontró un NetworkHandler válido para esta escena.");
        }

    }



    private void HandleRequestActiveSessionMessage(NetworkConnectionToClient conn, RequestActiveSessionMessage msg)
    {
        Debug.Log($"Servidor recibi� RequestActiveSessionMessage del cliente {conn.connectionId}.");

        if (activeSessions.Count == 0)
        {
            Debug.LogWarning("No hay sesiones activas.");
            Debug.Log("[Server] Enviando mensaje: ActiveSessionResponseMessage");


            conn.Send(new ActiveSessionResponseMessage { success = false });
            return;
        }

        var sessionData = activeSessions.Values.First();
        Debug.Log($"Enviando datos de la sesi�n activa al cliente {conn.connectionId}: {sessionData.sessionName}");
        Debug.Log("[Server] Enviando mensaje: ActiveSessionResponseMessage");


        conn.Send(new ActiveSessionResponseMessage
        {
            success = true,
            sessionData = sessionData
        });
        Debug.Log($"Mensaje enviado al cliente {conn.connectionId}: {sessionData.sessionName}, {sessionData.environmentName}, {string.Join(", ", sessionData.categories)}, {sessionData.dimension}");
    }

    // Comando para unirse a una sesi�n existente
    [Command]
    public void CmdJoinSession(NetworkConnectionToClient conn)
    {
        if (activeSessions.Count == 0)
        {
            Debug.LogWarning("No hay sesiones activas en este momento.");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        // Selecciona la primera sesi�n activa (puedes cambiar esto si necesitas algo m�s espec�fico)
        var sessionData = activeSessions.Values.First();

        Debug.Log($"Cliente {conn.connectionId} unido a la sesi�n '{sessionData.sessionName}'.");

        Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


        // Enviar los datos de la sesi�n al cliente
        conn.Send(new SessionCreatedMessage
        {
            success = true,
            sessionName = sessionData.sessionName,
            userId = sessionData.userId,
            environmentName = sessionData.environmentName,
            isOnlineSession = sessionData.isOnlineSession,
            displayMode = sessionData.displayMode,
            browsingMode = sessionData.browsingMode,
            volumetric = sessionData.volumetric,
            dimension = sessionData.dimension,
            categories = sessionData.categories,
            elementPaths = sessionData.elementPaths
        });
    }

    public void HandleAllClientsDisconnected()
    {
        if (activeSessions.Count > 0)
        {
            Debug.Log("Eliminando todas las sesiones activas porque no hay clientes conectados.");
            activeSessions.Clear();
            Debug.Log("Todas las sesiones han sido eliminadas.");

            // Eliminar todos los MuseumBaseNetworkHandler y CircularNetworkHandler activos
            DestroyAllNetworkHandlers();

        }
    }

    private void DestroyAllNetworkHandlers()
    {
        Debug.Log("Buscando y eliminando todos los NetworkHandlers activos...");

        // Buscar y destruir todos los MuseumBaseNetworkHandler
        foreach (var handler in FindObjectsOfType<MuseumBaseNetworkHandler>())
        {
            Debug.Log($"Eliminando MuseumBaseNetworkHandler con Net ID: {handler.GetComponent<NetworkIdentity>().netId}");
            NetworkServer.Destroy(handler.gameObject);
        }

        // Buscar y destruir todos los CircularNetworkHandler
        foreach (var handler in FindObjectsOfType<CircularNetworkHandler>())
        {
            Debug.Log($"Eliminando CircularNetworkHandler con Net ID: {handler.GetComponent<NetworkIdentity>().netId}");
            NetworkServer.Destroy(handler.gameObject);
        }
    }

    #endregion

    #region Sincronizaci�n con el Cliente

    // Notificar al cliente que la sesi�n fue creada exitosamente
    [TargetRpc]
    private void TargetNotifySessionCreated(NetworkConnection target, SessionData sessionData)
    {
        if (sessionData.categories == null)
        {
            sessionData.categories = new List<string>(); // Asegurarse de que no sea nulo
        }

        Debug.Log($"Sesi�n '{sessionData.sessionName}' creada en el cliente.");
        Debug.Log($"Datos enviados: Nombre: {sessionData.sessionName}, Usuario ID: {sessionData.userId}, Entorno: {sessionData.environmentName}, Categor�as: {string.Join(", ", sessionData.categories)}");
    }


    // Notificar al cliente que se uni� exitosamente a una sesi�n
    [TargetRpc]
    private void TargetNotifySessionJoined(NetworkConnection target, SessionData sessionData)
    {
        Debug.Log($"Cliente unido a la sesi�n '{sessionData.sessionName}'.");
        // Aqu� puedes sincronizar datos de la sesi�n con el cliente.
    }

    // Notificar al cliente de un error
    [TargetRpc]
    private void TargetNotifyError(NetworkConnection target, string errorMessage)
    {
        Debug.LogError($"Error enviado al cliente: {errorMessage}");
        // Aqu� puedes mostrar un mensaje de error en el cliente.
    }

    #endregion
}

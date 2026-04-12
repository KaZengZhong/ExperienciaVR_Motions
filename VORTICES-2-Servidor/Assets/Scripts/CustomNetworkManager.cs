using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

        // Instanciar y añadir el jugador
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





}

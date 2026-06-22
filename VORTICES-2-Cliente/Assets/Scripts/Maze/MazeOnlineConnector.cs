using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;

namespace Vortices
{
    // Adjuntar a cualquier GameObject en la escena MazeEnvironment.
    // Lee session.json del launcher y conecta al servidor si isOnlineSession = true.
    // En modo offline no hace nada — el laberinto se genera normalmente.
    public class MazeOnlineConnector : MonoBehaviour
    {
        private LauncherSessionData sessionData;

        private void Start()
        {
            sessionData = ReadSessionJson();

            if (sessionData == null)
            {
                Debug.Log("[MazeOnlineConnector] No se encontró session.json — modo offline.");
                return;
            }

            Debug.Log($"[MazeOnlineConnector] session.json leído: sesión={sessionData.sessionName}, online={sessionData.isOnlineSession}, ip={sessionData.ipAddress}");

            if (sessionData.isOnlineSession)
                StartCoroutine(ConnectAndRegisterSession());
        }

        private IEnumerator ConnectAndRegisterSession()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("[MazeOnlineConnector] No hay NetworkManager en la escena. " +
                               "Arrastra el prefab NetworkManager a la escena MazeEnvironment.");
                yield break;
            }

            if (!NetworkClient.isConnected)
            {
                NetworkManager.singleton.networkAddress = sessionData.ipAddress;
                NetworkManager.singleton.StartClient();

                float timeout = 10f;
                while (!NetworkClient.isConnected && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (!NetworkClient.isConnected)
                {
                    Debug.LogError("[MazeOnlineConnector] No se pudo conectar al servidor en " + sessionData.ipAddress);
                    yield break;
                }
            }

            Debug.Log("[MazeOnlineConnector] Conectado al servidor. Enviando datos de sesión...");

            // Vivox: login y unirse al canal de voz
            StartCoroutine(ConnectToVoiceChat(sessionData.userId));

            // Registrar handlers
            NetworkClient.RegisterHandler<SessionCreatedMessage>(OnSessionCreated);
            NetworkClient.RegisterHandler<ActiveSessionResponseMessage>(OnActiveSessionResponse);
            NetworkClient.RegisterHandler<TotemAnsweredMessage>(OnClientTotemAnswered);
            Debug.Log("[MazeOnlineConnector] Handlers registrados (sesión + tótems).");

            // Intentar crear sesión primero
            NetworkClient.Send(new CreateSessionMessage
            {
                sessionName     = sessionData.sessionName,
                userId          = sessionData.userId,
                environmentName = sessionData.environmentName,
                isOnlineSession = true,
                browsingMode    = "Online",
                categories      = new List<string>(),
                elementPaths    = new List<string>()
            });
        }

        private IEnumerator ConnectToVoiceChat(int userId)
        {
            VivoxVoiceManager vivox = VivoxVoiceManager.Instance;
            if (vivox == null) yield break;

            // Esperar a que Vivox esté completamente inicializado
            float timeout = 15f;
            while (!vivox.IsVivoxReady && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!vivox.IsVivoxReady)
            {
                Debug.LogWarning("[MazeOnlineConnector] Vivox no se inicializó a tiempo — sin chat de voz.");
                yield break;
            }

            bool loginDone = false;
            vivox.LoginAsync(userId.ToString()).ContinueWith(_ => loginDone = true);
            yield return new WaitUntil(() => loginDone);

            bool channelDone = false;
            vivox.JoinChannelAsync(VivoxVoiceManager.LobbyChannelName).ContinueWith(_ => channelDone = true);
            yield return new WaitUntil(() => channelDone);

            Debug.Log($"[MazeOnlineConnector] Voz conectada al canal '{VivoxVoiceManager.LobbyChannelName}'.");
        }

        private void OnSessionCreated(SessionCreatedMessage msg)
        {
            if (msg.success)
            {
                Debug.Log($"[MazeOnlineConnector] Sesión '{msg.sessionName}' creada en el servidor.");
            }
            else
            {
                // La sesión ya existe — unirse a la sesión activa
                Debug.Log("[MazeOnlineConnector] Sesión ya existe. Uniéndose a sesión activa...");
                NetworkClient.Send(new RequestActiveSessionMessage());
            }
        }

        private void OnActiveSessionResponse(ActiveSessionResponseMessage msg)
        {
            if (msg.success)
                Debug.Log($"[MazeOnlineConnector] Unido a sesión activa: '{msg.sessionData.sessionName}'.");
            else
                Debug.LogError("[MazeOnlineConnector] No hay sesión activa en el servidor.");
        }

        private void OnClientTotemAnswered(TotemAnsweredMessage msg)
        {
            Debug.Log($"[MazeOnlineConnector] TotemAnswered recibido — pos={msg.totemPosition}, real={msg.answeredReal}");
            InformationTotem[] totems = UnityEngine.Object.FindObjectsOfType<InformationTotem>();
            foreach (var totem in totems)
            {
                if (Vector3.Distance(totem.transform.position, msg.totemPosition) < 1f)
                {
                    totem.ApplyNetworkAnswer(msg.answeredReal, new Vector2Int(msg.senderCellX, msg.senderCellZ));
                    return;
                }
            }
            Debug.LogWarning($"[MazeOnlineConnector] No se encontró tótem en pos={msg.totemPosition}");
        }

        private void OnDestroy()
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.UnregisterHandler<SessionCreatedMessage>();
                NetworkClient.UnregisterHandler<ActiveSessionResponseMessage>();
                NetworkClient.UnregisterHandler<TotemAnsweredMessage>();
            }
        }

        // ── Lectura de session.json ────────────────────────────────────────────

        private static LauncherSessionData ReadSessionJson()
        {
            string path = Path.GetDirectoryName(Application.dataPath) + "/session.json";
            if (!File.Exists(path)) return null;

            try
            {
                return JsonUtility.FromJson<LauncherSessionData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MazeOnlineConnector] Error leyendo session.json: " + e.Message);
                return null;
            }
        }

        // ── Datos del launcher ─────────────────────────────────────────────────

        [Serializable]
        public class LauncherSessionData
        {
            public string sessionName     = "";
            public int    userId          = -1;
            public string environmentName = "Maze";
            public bool   isOnlineSession = false;
            public string ipAddress       = "127.0.0.1";
        }
    }
}

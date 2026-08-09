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
        public static bool IsReady { get; private set; } = false;

        private LauncherSessionData sessionData;

        private void Awake()
        {
            var p = ReadMazeParams();
            PlayerPrefs.SetInt("rotationMode", p.rotationMode);
            PlayerPrefs.SetInt("movementMode", p.movementMode);
            PlayerPrefs.Save();
        }

        private void Start()
        {
            sessionData = ReadSessionJson();

            if (sessionData == null)
            {
                Debug.Log("[MazeOnlineConnector] No se encontró config.json — modo offline.");
                IsReady = true;
                return;
            }

            Debug.Log($"[MazeOnlineConnector] session.json leído: sesión={sessionData.sessionName}, online={sessionData.isOnlineSession}, ip={sessionData.ipAddress}");

            if (sessionData.isOnlineSession)
                StartCoroutine(ConnectAndRegisterSession());
            else
                IsReady = true;
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

            // Leer parámetros del laberinto para incluirlos en el mensaje
            LocalMazeParams mp = ReadMazeParams();

            // Intentar crear sesión primero
            NetworkClient.Send(new CreateSessionMessage
            {
                sessionName     = sessionData.sessionName,
                userId          = sessionData.userId,
                environmentName = sessionData.environmentName,
                isOnlineSession = true,
                browsingMode    = "Online",
                categories      = new List<string>(),
                elementPaths    = new List<string>(),
                skinName        = mp.skinName,
                noCeiling       = mp.noCeiling,
                gridWidth       = mp.gridWidth,
                gridHeight      = mp.gridHeight,
                maxTotems       = mp.maxTotems
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
                IsReady = true; // creador: usa su propio config.json local
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
            {
                Debug.Log($"[MazeOnlineConnector] Unido a sesión activa: '{msg.sessionData.sessionName}'.");
                // Sobreescribir config.json con los parámetros del creador de la sesión
                WriteMazeParams(msg.sessionData);
            }
            else
            {
                Debug.LogError("[MazeOnlineConnector] No hay sesión activa en el servidor.");
            }
            IsReady = true;
        }

        private void WriteMazeParams(SessionData sd)
        {
            string path = PlatformPaths.ConfigJson;
            try
            {
                // Leer config existente para no perder otros campos (session, locomotion, etc.)
                FullConfig cfg = File.Exists(path)
                    ? JsonUtility.FromJson<FullConfig>(File.ReadAllText(path))
                    : new FullConfig();

                cfg.skinName  = sd.skinName;
                cfg.noCeiling = sd.noCeiling;
                cfg.gridWidth  = sd.gridWidth;
                cfg.gridHeight = sd.gridHeight;
                cfg.maxTotems  = sd.maxTotems;

                File.WriteAllText(path, JsonUtility.ToJson(cfg));
                Debug.Log($"[MazeOnlineConnector] config.json actualizado desde servidor: skin={sd.skinName}, grid={sd.gridWidth}x{sd.gridHeight}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MazeOnlineConnector] Error escribiendo config.json: " + e.Message);
            }
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
            IsReady = false;
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
            string path = PlatformPaths.ConfigJson;
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

        // ── Lectura de config.json ────────────────────────────────────────────

        private static LocalMazeParams ReadMazeParams()
        {
            string path = PlatformPaths.ConfigJson;
            if (!File.Exists(path)) return new LocalMazeParams();
            try { return JsonUtility.FromJson<LocalMazeParams>(File.ReadAllText(path)); }
            catch { return new LocalMazeParams(); }
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

        [Serializable]
        private class LocalMazeParams
        {
            public string skinName     = "";
            public bool   noCeiling    = false;
            public int    gridWidth    = 0;
            public int    gridHeight   = 0;
            public int    maxTotems    = 0;
            public int    rotationMode = 0;
            public int    movementMode = 0;
        }

        [Serializable]
        private class FullConfig
        {
            public string sessionName     = "";
            public int    userId          = 0;
            public string environmentName = "Maze";
            public bool   isOnlineSession = false;
            public string ipAddress       = "";
            public string skinName        = "";
            public bool   noCeiling       = false;
            public int    gridWidth       = 0;
            public int    gridHeight      = 0;
            public int    maxTotems       = 0;
            public int    rotationMode    = 0;
            public int    movementMode    = 0;
        }
    }
}

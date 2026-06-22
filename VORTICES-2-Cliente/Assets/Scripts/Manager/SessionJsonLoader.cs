using System;
using System.IO;
using UnityEngine;

namespace Vortices
{
    // Lee session.json guardado por el Interaction_Launcher y auto-lanza la sesión.
    // Adjuntar al mismo GameObject que SessionManager en la escena Main Menu.
    public class SessionJsonLoader : MonoBehaviour
    {
        [SerializeField] private SessionManager sessionManager;

        // Devuelve la IP del session.json, o 127.0.0.1 como fallback.
        public static string GetServerIp()
        {
            LauncherSessionData data = Load();
            if (data != null && !string.IsNullOrEmpty(data.ipAddress))
                return data.ipAddress;
            return "127.0.0.1";
        }

        private void Start()
        {
            LauncherSessionData data = Load();
            if (data == null) return;

            if (sessionManager == null)
                sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager == null) return;

            sessionManager.sessionName     = data.sessionName;
            sessionManager.userId          = data.userId;
            sessionManager.environmentName = data.environmentName;
            sessionManager.isOnlineSession = data.isOnlineSession;

            Debug.Log($"[SessionJsonLoader] Sesión cargada: {data.sessionName}, env={data.environmentName}, online={data.isOnlineSession}, ip={data.ipAddress}");

            // Configurar el ambiente en AddonsController para que GoToSceneRoutine sepa qué escena cargar
            AddonsController addons = AddonsController.instance;
            if (addons != null)
            {
                addons.LoadAddonObjects();
                foreach (var envObj in addons.environmentObjects)
                {
                    if (envObj.environmentName == data.environmentName + " Environment" ||
                        envObj.environmentName == data.environmentName)
                    {
                        addons.currentEnvironmentObject = envObj;
                        break;
                    }
                }
            }

            // Auto-lanzar solo si todos los campos requeridos están presentes
            bool ready = !string.IsNullOrEmpty(data.sessionName)
                      && data.userId >= 0
                      && !string.IsNullOrEmpty(data.environmentName);

            if (ready)
            {
                Debug.Log("[SessionJsonLoader] Auto-lanzando sesión...");
                sessionManager.LaunchSession();
            }
        }

        private static LauncherSessionData Load()
        {
            string path = GetSessionJsonPath();
            if (!File.Exists(path)) return null;

            try
            {
                return JsonUtility.FromJson<LauncherSessionData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SessionJsonLoader] Error leyendo session.json: " + e.Message);
                return null;
            }
        }

        private static string GetSessionJsonPath()
        {
#if UNITY_EDITOR
            // En editor: busca en la raíz del proyecto para facilitar pruebas manuales
            return Path.GetDirectoryName(Application.dataPath) + "/session.json";
#else
            // En build: mismo directorio que el .exe
            return Path.GetDirectoryName(Application.dataPath) + "/session.json";
#endif
        }

        [Serializable]
        public class LauncherSessionData
        {
            public string sessionName     = "";
            public int    userId          = -1;
            public string environmentName = "Maze";
            public bool   isOnlineSession = false;
            public string ipAddress       = "";
        }
    }
}

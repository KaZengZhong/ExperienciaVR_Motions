using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Threading.Tasks;

using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;

namespace Vortices
{
    public class SessionManager : MonoBehaviour
    {

        // Static instance
        public static SessionManager instance;

        // Settings
        public string sessionName;
        public int userId;
        public string environmentName;
        public bool isOnlineSession = false;
        // Environment Settings
        public string displayMode;
        public string browsingMode;
        public bool volumetric;
        public Vector3Int dimension;
        public List<string> elementPaths;
        // Session Manager settings
        public float initializeTime = 2.0f;
        private bool sessionDataReceived = false;


        // Controllers
        [SerializeField] private SceneTransitionManager actualTransitionManager;
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;
        [SerializeField] public SpawnController spawnController;
        [SerializeField] public AddonsController addonsController;
        private CategorySelector categorySelector;
        [SerializeField] public LoggingController loggingController;
        public InputController inputController;
        [SerializeField] public LocalizationController localizationController;
        [SerializeField] public ErrorController errorController;
        public RighthandTools righthandTools;

        // Coroutine
        public bool sessionLaunchRunning;

        public GameObject currentlySelected;
        public GameObject lastSelected;
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lastSelected = null; // Reset lastSelected when a new scene is loaded
        }

        private void Start()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            inputController = GameObject.FindObjectOfType<InputController>(true);

            errorController.Initialize();
            inputController.Initialize();
            addonsController.Initialize();
            localizationController.Initialize();

            // Registrar handlers en el cliente
            NetworkClient.RegisterHandler<SessionCreatedMessage>(HandleSessionCreatedMessage);
            NetworkClient.RegisterHandler<ActiveSessionResponseMessage>(HandleActiveSessionResponse);

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void HandleSessionCreatedMessage(SessionCreatedMessage msg)
        {
            if (!msg.success)
            {
                return;
            }

            sessionName = msg.sessionName;
            userId = msg.userId;

            if (msg.environmentName == "Circular Environment")
            {
                environmentName = "Circular";
            }
            else if (msg.environmentName == "Museum Environment")
            {
                environmentName = "Museum";
            }
            else if (msg.environmentName == "Maze Environment")
            {
                environmentName = "Maze";
            }
            else
            {
                environmentName = msg.environmentName;
            }
            isOnlineSession = msg.isOnlineSession;
            displayMode = msg.displayMode;
            browsingMode = msg.browsingMode;
            volumetric = msg.volumetric;
            dimension = msg.dimension;
            elementPaths = msg.elementPaths;

            categoryController.UpdateCategoriesList(msg.categories);
        }


        #region UI Handling

        private void Update()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                currentlySelected = EventSystem.current.currentSelectedGameObject;
            }
            else
            {
                currentlySelected = null;
            }
            if (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
            {
                FindAndSetNextSelectable();
            }
            else
            {
                lastSelected = EventSystem.current.currentSelectedGameObject;
            }
        }

        private void FindAndSetNextSelectable()
        {
            GameObject canvasObject = GameObject.Find("Canvas"); // Find the GameObject named "Canvas"

            if (canvasObject == null)
            {
                Debug.LogError("No GameObject named 'Canvas' found in the scene.");
                return;
            }

            Canvas canvasComponent = canvasObject.GetComponent<Canvas>(); // Get the Canvas component from the GameObject

            if (canvasComponent == null)
            {
                Debug.LogError("No Canvas component found on the 'Canvas' GameObject.");
                return;
            }

            Selectable[] selectables = canvasComponent.GetComponentsInChildren<Selectable>();

            if (selectables.Length == 0)
            {
                return; // No selectables in the canvas
            }

            int startIndex = 0;
            if (lastSelected != null)
            {
                int lastIndex = System.Array.FindIndex(selectables, selectable => selectable.gameObject == lastSelected);
                if (lastIndex != -1)
                {
                    startIndex = (lastIndex + 1) % selectables.Length;
                }
            }

            for (int i = 0; i < selectables.Length; i++)
            {
                int index = (startIndex + i) % selectables.Length;
                if (selectables[index].gameObject.activeInHierarchy && selectables[index].interactable)
                {
                    EventSystem.current.SetSelectedGameObject(selectables[index].gameObject);
                    lastSelected = selectables[index].gameObject;
                    break;
                }
            }
        }

        #endregion

        #region Data Operations
        public void LaunchSession()
        {
            if (!sessionLaunchRunning)
            {
                StartCoroutine(LaunchSessionCoroutine(sessionName, userId, environmentName));
            }
        }

        public IEnumerator LaunchSessionCoroutine(string sessionName, int userId, string environmentName)
        {
            sessionLaunchRunning = true;

            GameObject keyboard = GameObject.Find("Keyboard Canvas");
            if (keyboard != null)
            {
                keyboard.GetComponent<HandKeyboard>().RemoveInputField();
            }


            // Switch to environment scene
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            actualTransitionManager.returnToMain = false;

            if (isOnlineSession)
                {
                    if (!NetworkClient.isConnected)
                    {
                        NetworkManager.singleton.networkAddress = SessionJsonLoader.GetServerIp();
                        NetworkManager.singleton.StartClient();

                        float timeout = 10f;
                        while (!NetworkClient.isConnected && timeout > 0f)
                        {
                            timeout -= Time.deltaTime;
                            yield return null;
                        }

                        if (!NetworkClient.isConnected)
                        {
                            Debug.LogError("[SessionManager] No se pudo conectar al servidor.");
                            sessionLaunchRunning = false;
                            yield break;
                        }
                    }

                    SessionData sessionData = new SessionData
                    {
                        sessionName = sessionName,
                        userId = userId,
                        environmentName = environmentName,
                        elementPaths = elementPaths,
                        categories = categoryController.GetCategories(),
                        browsingMode = "Online"
                    };

                    yield return StartCoroutine(SendSessionDataToServer(sessionData));

                    
                    if (environmentName == "Circular")
                    {
                        environmentName = "Circular Environment";
                    }
                    else if (environmentName == "Museum")
                    {
                        environmentName = "Museum Environment";
                    }
                    else if (environmentName == "Maze")
                    {
                        environmentName = "Maze Environment";
                    }

                    yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());
            
                    yield return new WaitForSeconds(initializeTime);



                    //  AHORA BUSCAMOS TODOS LOS OBJETOS NECESARIOS 
                    actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
                    categoryController = GameObject.FindObjectOfType<CategoryController>(true);
                    elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
                    loggingController = GameObject.FindObjectOfType<LoggingController>(true);
                    spawnController = GameObject.FindObjectOfType<SpawnController>(true);
                    righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

                    elementCategoryController.Initialize();
                    loggingController.Initialize();
                    spawnController.Initialize();
                    inputController.RestartInputs();

                    sessionLaunchRunning = false;

                    StartCoroutine(ConnectToVoiceChatCoroutine(userId));
                    Debug.Log("Se envió la data al servidor.");
                    yield break;
                }

            Debug.Log("Empeze GoTo Offline");
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());
            
            Debug.Log("Sali GoTo Offline");
            yield return new WaitForSeconds(initializeTime);

            // When done, configure controllers of the scene
            Debug.Log("Busco Offline");
            actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            loggingController = GameObject.FindObjectOfType<LoggingController>(true);
            spawnController = GameObject.FindObjectOfType<SpawnController>(true);
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            elementCategoryController.Initialize();
            loggingController.Initialize();
            spawnController.Initialize();

            inputController.RestartInputs();


            sessionLaunchRunning = false;
            Debug.Log("Termino Offline");
        }

        public IEnumerator SendSessionDataToServer(SessionData sessionData)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogError("El cliente no est� conectado al servidor. No se puede enviar la sesi�n.");
                yield break;
            }

            Debug.Log("Enviando datos de sesi�n al servidor...");
            NetworkClient.Send(new CreateSessionMessage
            {
                sessionName = sessionName,
                userId = userId,
                environmentName = environmentName,
                isOnlineSession = isOnlineSession,
                displayMode = displayMode,
                browsingMode = browsingMode,
                volumetric = volumetric,
                dimension = dimension,
                categories = categoryController.GetCategories(),
                elementPaths = elementPaths
            });

            yield return new WaitForSeconds(1.0f);
        }

        public IEnumerator StopSessionCoroutine()
        {
            sessionLaunchRunning = true;

            yield return StartCoroutine(DisconnectFromVivoxCoroutine());

            if (NetworkClient.isConnected)
            {
                Debug.Log("El cliente est� conectado a una sesi�n. Desconectando...");
                NetworkClient.Disconnect();

                // Esperar a que el cliente se desconecte
                float timeout = 5f; // Tiempo m�ximo para desconectar
                while (NetworkClient.isConnected && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (NetworkClient.isConnected)
                {
                    Debug.LogError("No se pudo desconectar del servidor dentro del tiempo de espera.");
                    yield break;
                }
                else
                {
                    Debug.Log("Desconectado del servidor con �xito.");
                }
            }
            else
            {
                Debug.Log("El cliente no est� conectado a ninguna sesi�n.");
            }

            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            if (actualTransitionManager == null)
            {
                Debug.LogError("No se encontro el actualTransitionManager");
            }
            else
            {
                Debug.Log("Se encontro el actualTransitionManager");
            }

            if (righthandTools == null)
            {
                Debug.LogError("righthandTools no est� asignado. Verifica su inicializaci�n.");
                yield break; // Salimos del coroutine para evitar m�s errores
            }
            else
            {
                Debug.Log("righthandTools est� asignado correctamente.");
            }

            Fade toolsFader = righthandTools.GetComponent<Fade>();
            yield return StartCoroutine(toolsFader.FadeOutCoroutine());

            actualTransitionManager.returnToMain = true;
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);

            if(categorySelector == null)
            {
                Debug.LogError("No se encontro el categorySelector");
            }
            else
            {
                Debug.Log("categoriSelector encontrado");
            }

            ResetSessionManager();

            sessionLaunchRunning = false;
        }

        private void ResetSessionManager()
        {
            Debug.Log("Reiniciando SessionManager...");

            // Restablecer valores del SessionManager
            sessionName = string.Empty;
            userId = 0;
            environmentName = string.Empty;
            displayMode = null;
            volumetric = false;
            dimension = Vector3Int.zero;
            elementPaths?.Clear();
            categoryController.UpdateCategoriesList(null);
            isOnlineSession = false;
            browsingMode = string.Empty;

            // Reiniciar controladores auxiliares si es necesario
            categoryController?.Initialize();
            elementCategoryController?.Initialize();
            loggingController?.Initialize();

            Debug.Log("SessionManager reiniciado con �xito.");
        }

        public void JoinSession(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                NetworkManager.singleton.networkAddress = ipAddress;
                NetworkManager.singleton.StartClient();
                StartCoroutine(WaitForConnectionAndJoinSession()); 

                isOnlineSession = true;

                StartCoroutine(WaitForSessionDataAndLoadScene());
            }
            else
            {
                Debug.LogWarning("La dirección IP está vacía.");
            }
        }


        private IEnumerator JoinSessionRoutine()
        {
            Debug.Log("[DEBUG] Iniciando carga de escena para sesión online...");
        
            // Cambiar a la escena correspondiente
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            Debug.Log("[DEBUG] Esperando tiempo de inicialización...");
            yield return new WaitForSeconds(initializeTime);

            //  AHORA BUSCAMOS TODOS LOS OBJETOS NECESARIOS 
            Debug.Log("[DEBUG] Buscando y configurando controladores en la escena...");

            actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            loggingController = GameObject.FindObjectOfType<LoggingController>(true);
            spawnController = GameObject.FindObjectOfType<SpawnController>(true);
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            elementCategoryController.Initialize();
            loggingController.Initialize();
            spawnController.Initialize();
            inputController.RestartInputs();

            sessionLaunchRunning = false;

            // Conectar al canal de voz
            Debug.Log("[DEBUG] Conectando al canal de voz...");
            StartCoroutine(ConnectToVoiceChatCoroutine(userId));
        }


        private IEnumerator WaitForConnectionAndJoinSession()
        {
            float timeout = 10f;
            float elapsedTime = 0f;

            while (!NetworkClient.isConnected && elapsedTime < timeout)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new RequestActiveSessionMessage());
            }

        }

        private void HandleActiveSessionResponse(ActiveSessionResponseMessage msg)
        {
            if (!msg.success)
            {
                return;
            }

            if (msg.sessionData.categories == null)
            {
                msg.sessionData.categories = new List<string>();
            }

            if (msg.sessionData.elementPaths == null)
            {
                msg.sessionData.elementPaths = new List<string>(); 
            }

            sessionName = msg.sessionData.sessionName;

            if (msg.sessionData.environmentName == "Circular Environment")
            {
                environmentName = "Circular";
            }
            else if (msg.sessionData.environmentName == "Museum Environment")
            {
                environmentName = "Museum";
            }
            else if (msg.sessionData.environmentName == "Maze Environment")
            {
                environmentName = "Maze";
            }
            else
            {
                environmentName = msg.sessionData.environmentName;
            }

            browsingMode = msg.sessionData.browsingMode;
            displayMode = msg.sessionData.displayMode;
            volumetric = msg.sessionData.volumetric;
            dimension = msg.sessionData.dimension;

            foreach (string path in msg.sessionData.elementPaths)
            {
                Debug.Log($"  - {path}");
            }
            categoryController.Initialize();
            //  Asignamos los `elementPaths` al SessionManager
            elementPaths = new List<string>(msg.sessionData.elementPaths);

            //  Actualizamos el AddonsController con el environment correcto
            AddonsController addonsController = AddonsController.instance;
            if (addonsController != null)
            {
                foreach (var envObject in addonsController.environmentObjects)
                {
                    if (envObject.environmentName == msg.sessionData.environmentName)
                    {
                        addonsController.currentEnvironmentObject = envObject;
                        break;
                    }
                }
            }

            //  Sincronizar categorías en CategoryController
            if (categoryController != null)
            {
                categoryController.UpdateCategoriesList(msg.sessionData.categories);

                //  Ahora sincronizamos las categorías seleccionadas
                if (msg.sessionData.categories.Count > 0)
                {
                    categoryController.UpdateSelectedCategoriesList(msg.sessionData.categories);
                }
            }


            //  Confirmamos que los datos han sido recibidos
            sessionDataReceived = true;
        }




        private IEnumerator WaitForSessionDataAndLoadScene()
        {

            float timeout = 10f;
            while (!sessionDataReceived && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!sessionDataReceived)
            {
                yield break;
            }

            StartCoroutine(JoinSessionRoutine());
        }




        private IEnumerator ConnectToVoiceChatCoroutine(int userId)
        {
            bool loginSuccess = false;
            bool channelJoinSuccess = false;

            // Intentar iniciar sesión en Vivox
            VivoxVoiceManager.Instance.LoginAsync(userId.ToString()).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    loginSuccess = true;
                }
            });

            // Esperar que el login termine
            yield return new WaitUntil(() => loginSuccess);

            // Intentar unirse al canal de voz
            VivoxVoiceManager.Instance.JoinChannelAsync("VoRTIcESVoiceChat").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    channelJoinSuccess = true;
                }
            });

            // Esperar que se conecte al canal
            yield return new WaitUntil(() => channelJoinSuccess);
        }

        private IEnumerator DisconnectFromVivoxCoroutine()
        {
            Debug.Log("[VoiceChat] Iniciando desconexión de Vivox...");

            if (VivoxVoiceManager.Instance != null)
            {
                // Salir del canal de voz
                if (VivoxVoiceManager.LobbyChannelName != null)
                {
                    Task leaveChannelTask = VivoxVoiceManager.Instance.LeaveChannelAsync(VivoxVoiceManager.LobbyChannelName);
                    yield return new WaitUntil(() => leaveChannelTask.IsCompleted);

                    if (leaveChannelTask.Exception != null)
                    {
                        Debug.LogError($"Error al salir del canal de voz: {leaveChannelTask.Exception.Message}");
                    }
                    else
                    {
                        Debug.Log("[VoiceChat] Desconectado del canal de voz.");
                    }
                }

                // Cerrar sesión de Vivox
                Task logoutTask = VivoxVoiceManager.Instance.LogoutAsync();
                yield return new WaitUntil(() => logoutTask.IsCompleted);

                if (logoutTask.Exception != null)
                {
                    Debug.LogError($"Error al cerrar sesión en Vivox: {logoutTask.Exception.Message}");
                }
                else
                {
                    Debug.Log("[VoiceChat] Sesión en Vivox cerrada.");
                }
            }
            else
            {
                Debug.LogError("[VoiceChat] VivoxVoiceManager.Instance es NULL. No se pudo desconectar.");
            }
        }


        #endregion
    }
}


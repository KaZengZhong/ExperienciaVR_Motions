using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;

namespace Vortices
{
    public class SessionManager : NetworkBehaviour
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
            // Registra el manejador para SessionCreatedMessage
            NetworkClient.RegisterHandler<SessionCreatedMessage>(OnSessionCreated);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lastSelected = null; // Reset lastSelected when a new scene is loaded
        }

        private void OnSessionCreated(SessionCreatedMessage msg)
        {
            if (msg.success)
            {
                Debug.Log($"Sesión creada con éxito: {msg.sessionName}");
                // Aquí puedes continuar con el flujo del cliente después de recibir la confirmación
            }
            else
            {
                Debug.LogError("Error al crear la sesión en el servidor.");
            }
        }

        private void Start()
        {
            Debug.Log("Se inicia el Session Manager");
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

            instance = this;
            DontDestroyOnLoad(gameObject);
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
                NetworkManager.singleton.ServerChangeScene("Museum Environment");
                yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());
            }
            else
            {
                yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());
            }

            yield return new WaitForSeconds(initializeTime);

            // Verificar si la sesión es online y, si es así, iniciar el host
            if (isOnlineSession)
            {
                NetworkManager.singleton.StartHost();
            }

            // When done, configure controllers of the scene
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
        }

        public IEnumerator StopSessionCoroutine()
        {
            sessionLaunchRunning = true;
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            Fade toolsFader = righthandTools.GetComponent<Fade>();
            yield return StartCoroutine(toolsFader.FadeOutCoroutine());

            actualTransitionManager.returnToMain = true;
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);

            sessionLaunchRunning = false;
        }

        #endregion
    }
}


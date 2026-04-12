using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Mirror;
using TMPro;
using System.Linq;

using UnityEngine.UI;

namespace Vortices
{
    enum MainMenuId
    {
        // Change this when order is changed or when new submenus are added
        Main = 0,
        Session = 1,
        CategorySelection = 2,
        Join = 3,
        Options = 4,
        About = 5

    }

    public class MainMenuPanel : MonoBehaviour
    {
        // Panel UI Components
        [SerializeField] public List<GameObject> optionScreenUiComponents;
        [SerializeField] public List<Toggle> panelToggles;
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private TMP_InputField userID;

        // Panel Properties
        public int actualComponentId { get; set; }

        // Other
        Color normalColor = new Color(0.2956568f, 0.3553756f, 0.4150943f, 1.0f);
        Color disabledColor = Color.black;

        // Settings
        public int currentEnvironmentId;

        // Coroutine
        private bool isChangePanelRunning;

        // Auxiliary References
        private SceneTransitionManager transitionManager;
        private SessionManager sessionManager;


        #region User Input

        private void OnEnable()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            transitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
        }

        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }

        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = optionScreenUiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            optionScreenUiComponents[actualComponentId].SetActive(false);
            // Enable new component
            optionScreenUiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // FadeIn new component
            FadeUI newComponentFader = optionScreenUiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
        }


        // Changes to panel based on environment selection
        public void ChangePanelEnvironment()
        {
            StartCoroutine(VerifyAndChangePanelEnvironment());
        }

        private IEnumerator VerifyAndChangePanelEnvironment()
        {
            //  Cambiar al panel de selección del BrowsingMode
            ChangeVisibleComponent(6 + currentEnvironmentId);

            //  Obtener el nombre del entorno actual
            string environmentName = SessionManager.instance.environmentName;

            Debug.Log($"[DEBUG] Verificando panel de navegación para el entorno: {environmentName}");

            float timeout = 5f; //  Evitar bucles infinitos con un tiempo límite

            if (environmentName == "Museum")
            {
                MuseumPanel museumPanel = null;
                while (museumPanel == null || !museumPanel.gameObject.activeInHierarchy)
                {
                    museumPanel = FindObjectsOfType<MuseumPanel>(true).FirstOrDefault();
                    if (museumPanel != null && museumPanel.gameObject.activeInHierarchy)
                    {
                        break;
                    }
                    timeout -= Time.deltaTime;
                    if (timeout <= 0)
                    {
                        Debug.LogError("[ERROR] Timeout esperando a que MuseumPanel se active.");
                        yield break;
                    }
                    yield return null;
                }

                Debug.Log("[DEBUG] MuseumPanel activado correctamente.");

                //  Verificar si la sesión es online
                if (SessionManager.instance.isOnlineSession)
                {
                    Debug.Log("[DEBUG] Sesión online detectada. Forzando Browsing Mode a Online en MuseumPanel.");
                    museumPanel.browsingMode = 1;
                    museumPanel.ChangeComponentBrowserMode();
                }
            }
            else if (environmentName == "Circular")
            {
                CircularPanel circularPanel = null;
                while (circularPanel == null || !circularPanel.gameObject.activeInHierarchy)
                {
                    circularPanel = FindObjectsOfType<CircularPanel>(true).FirstOrDefault();
                    if (circularPanel != null && circularPanel.gameObject.activeInHierarchy)
                    {
                        break;
                    }
                    timeout -= Time.deltaTime;
                    if (timeout <= 0)
                    {
                        Debug.LogError("[ERROR] Timeout esperando a que CircularPanel se active.");
                        yield break;
                    }
                    yield return null;
                }

                Debug.Log("[DEBUG] CircularPanel activado correctamente.");

                //  Verificar si la sesión es online
                if (SessionManager.instance.isOnlineSession)
                {
                    Debug.Log("[DEBUG] Sesión online detectada. Forzando Browsing Mode a Online en CircularPanel.");
                    circularPanel.browsingMode = 1;
                    circularPanel.ChangeComponentBrowserMode();
                }
            }
            else
            {
                Debug.LogWarning($"[WARNING] Environment '{environmentName}' no reconocido. No se aplicará cambio automático de Browsing Mode.");
            }
        }


        public void ChangePanelToggle(Toggle toggle)
        {
            if (!isChangePanelRunning)
            {
                StartCoroutine(ChangePanelToggleCoroutine(toggle));
            }
        }

        public IEnumerator ChangePanelToggleCoroutine(Toggle toggle)
        {
            isChangePanelRunning = true;

            // Turn all toggles uninteractable with color normal except the one thats pressed which will have color disabled
            foreach (Toggle panelToggle in panelToggles)
            {
                if (!(panelToggle == toggle))
                {
                    // They have to have color disabled normal
                    ColorBlock disabledNormal = toggle.colors;
                    disabledNormal.disabledColor = normalColor;
                    panelToggle.colors = disabledNormal;
                          
                }

                panelToggle.interactable = false;
            }

            if(optionScreenUiComponents.Count > 6)
            {
                bool environmentStatus = optionScreenUiComponents[6 + currentEnvironmentId].activeInHierarchy;

                // If a configuration panel was running, it has to be resetted
                if (environmentStatus)
                {
                    SpawnPanel environmentPanel = optionScreenUiComponents[6 + currentEnvironmentId].GetComponent<SpawnPanel>();
                    environmentPanel.RestartPanel();
                }
            }

            // Change component using toggle parent name
            if (toggle.transform.parent.name == "Start Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int) MainMenuId.Session));
            }
            else if (toggle.transform.parent.name == "Options Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)MainMenuId.Options));
            }
            else if (toggle.transform.parent.name == "About Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)MainMenuId.About));
            }
            else if (toggle.transform.parent.name == "Join Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)MainMenuId.Join));
            }

            // Turn all toggles interactable with color normal except the one that was pressed
            foreach (Toggle panelToggle in panelToggles)
            {
                if (!(panelToggle == toggle))
                {
                    // They have to have color disabled normal
                    ColorBlock disabledBlack = panelToggle.colors;
                    disabledBlack.disabledColor = disabledColor;
                    panelToggle.colors = disabledBlack;
                    panelToggle.interactable = true;
                }
            }

            isChangePanelRunning = false;
        }

        public void OnJoinSessionClicked()
        {
            string ipAddress = ipAddressInputField.text;
            string userIdText = userID.text;

            if (int.TryParse(userIdText, out int userId))
            {
                
                SessionManager.instance.userId = userId;
                Debug.Log($"[UserID] Se ha establecido el UserId en: {userId}");
            }
            else
            {
                Debug.LogError("[UserID] El valor ingresado no es un número válido.");
            }

            if (SessionManager.instance != null)
            {
                SessionManager.instance.JoinSession(ipAddress);
            }
            else
            {
                Debug.LogError("SessionManager no est� disponible.");
            }
        }

        #endregion

        #region Data Operations



        public void CloseAplication() 
        { 
            StartCoroutine(CloseApplicationCoroutine());
        }
        public IEnumerator CloseApplicationCoroutine()
        {
            yield return StartCoroutine(transitionManager.FadeScreenOut());
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
                Application.Quit();
        }


        #endregion
    }
}


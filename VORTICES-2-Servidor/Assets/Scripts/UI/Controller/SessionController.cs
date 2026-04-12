using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace Vortices
{
    public class SessionController : MonoBehaviour
    {
        // Prefabs
        [SerializeField] private GameObject environmenTogglePrefab;
        [SerializeField] private GameObject UISessionPrefab;

        // Other references
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private GameObject scrollviewContent;
        [SerializeField] private TextInputField sessionAddInputField;
        [SerializeField] private TextInputField userIdInputField;
        [SerializeField] private List<Toggle> environmentToggles;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI alertText;
        [SerializeField] private SessionManager sessionManager;
        [SerializeField] private GameObject mainMenuOptionsContent;
        [SerializeField] public GameObject environmentScrollviewContent;

        // Data
        public List<string> sessions;
        private List<UISession> UISessions;
        private UISession sessionToDelete;
        public string selectedSession;
        public int selectedUserId;
        public string selectedEnvironment;
        public bool isOnlineMode = false;

        // Settings
        private float alertDuration = 5.0f;
        private float alertFadeTime = 0.3f;

        // Coroutine
        private bool alertCoroutineRunning;


        private void OnEnable()
        {
            UnlockContinueButton();
        }

        public void Initialize()
        {
            selectedSession = "";
            sessions = new List<string>();
            UISessions = null;
            UISessions = new List<UISession>();

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();

            selectedUserId = -1;
            userIdInputField.inputfield.text = "";
            userIdInputField.placeholder.enabled = true;
            selectedEnvironment = "";
            if (mainMenuPanel.optionScreenUiComponents.Count == 6)
            {
                LoadEnvironments();
            }
            // When initialized will try to load sessions, will create a new session list otherwise
            LoadSessions();
            // Categories will be added to UI Components
            UpdateSessions(true);
        }

        #region Data Operation;

        // Session configuration
        public void AddSession()
        {
            string sessionName = sessionAddInputField.GetData();
            // Add to UI component
            AddSessionToScrollView(sessionName);
            // Save all sessions to file
            SaveSessions();
        }

        public void RemoveSession(UISession session)
        {
            // Remove from UI component
            RemoveSessionFromScrollView(session);
            // Save all sessions to file
            SaveSessions();
        }

        private void AddSessionToScrollView(string sessionName)
        {
            CreateSession(sessionName, true);
            // Updates rows
            UpdateSessions(false);
        }

        private void RemoveSessionFromScrollView(UISession session)
        {
            // Deletion guard
            if (sessionToDelete == null)
            {
                sessionToDelete = session;
                sessionToDelete.nameText.text = "Erase data? ->";
                return;
            }
            // If other session is selected, return normal name
            else if (sessionToDelete != session)
            {
                sessionToDelete.nameText.text = sessionToDelete.sessionName;
                sessionToDelete = session;
                sessionToDelete.nameText.text = "Erase data? ->";
                return;
            }

            // Searches the UIComponents for session position
            string sessionName = session.sessionName;

            // Removes from list
            sessions.Remove(sessionName);
            if (selectedSession == sessionName)
            {
                selectedSession = "";
            }
            // Remove 
            // Destroys said Component
            session.DestroySession();
            // Destroys UI Session
            UISessions.Remove(session);
            // Updates rows
            UpdateSessions(false);

            // Remove from files
            DeleteSession(session.sessionName);

            sessionToDelete = null;
        }

        private void UpdateSessions(bool clear)
        {
            // Clear past UI Categories
            if (clear)
            {
                foreach (Transform child in scrollviewContent.transform)
                {
                    Destroy(child.gameObject);
                }

            }

            // If UISessions is empty this means we create new objects to hold the sessions
            if (UISessions.Count == 0)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    CreateSession(sessions[i], false);
                }
            }
            // If UISessions is not empty it means we can reuse the ui sessions
            else
            {
                sessions = sessions.OrderBy(session => session).ToList();
                UISessions = UISessions.OrderBy(session => session.sessionName).ToList();
                for (int i = 0; i < UISessions.Count; i++)
                {
                    UISessions[i].transform.SetParent(scrollviewContent.transform);
                }
            }
        }

        private void CreateSession(string sessionName, bool addToList)
        {

            if (sessionName != "")
            {
                //Filters if session should be created by the rules specified in this function
                string result = "";

                if (addToList && sessionName != "")
                {
                    result = FilterSession(sessionName);
                }
                else
                {
                    result = "OK";
                }

                if (result == "OK")
                {
                    UISession newSession = Instantiate(UISessionPrefab, scrollviewContent.transform).GetComponent<UISession>();
                    // Initialize
                    newSession.Init(sessionName, this);

                    // Add session to session list (If its loaded, you dont add it again)
                    if (addToList)
                    {
                        sessions.Add(newSession.sessionName);
                    }
                    // Add gameobject to list for easy access
                    UISessions.Add(newSession);

                    // Sometimes the UI elements deactivate, activate if so
                    LayoutElement layoutElement = newSession.GetComponent<LayoutElement>();
                    if (!layoutElement.isActiveAndEnabled)
                    {
                        layoutElement.gameObject.SetActive(true);
                    }
                }
                else if (result == "Same")
                {
                    sessionAddInputField.SetText("");
                    sessionAddInputField.placeholder.text = "Session already exists.";
                }
            }
        }

        private string FilterSession(string sessionName)
        {
            // Check if session has been already added
            string newName = sessionName.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            if (sessions.Contains(newName))
            {
                return "Same";
            }

            return "OK";
        }

        // Id Configuration
        public void SetUserId()
        {
            string userId = userIdInputField.GetData();

            if (userId == "")
            {
                return;
            }

            // Check if Id is only a number otherwise fail the user set
            foreach (char c in userId)
            {

                if (c < '0' || c > '9')
                {
                    if (!alertCoroutineRunning)
                    {
                        StartCoroutine(SetAlert("User ID must be 0 or higher"));
                    }
                    return;
                }
            }

            int userIdInt = int.Parse(userId);

            if (userIdInt <= -1)
            {
                if (!alertCoroutineRunning)
                {
                    StartCoroutine(SetAlert("User ID must be 0 or higher"));
                }
                return;
            }

            this.selectedUserId = userIdInt;
        }

        // Environment Configuration
        // Loading of environments using addons and asset bundles
        public void LoadEnvironments()
        {
            AddonsController.instance.LoadAddonObjects();
            // Load prefab to configuration menu
            for (int i = 0; i < AddonsController.instance.environmentObjects.Count; i++)
            {
                // Set up the transition of Main Menu
                mainMenuPanel.optionScreenUiComponents.Add(Instantiate(AddonsController.instance.environmentObjects[i].panel, mainMenuOptionsContent.transform));
                // Add a Toggle to select it
                UIEnvironment environmentToggle = Instantiate(environmenTogglePrefab, environmentScrollviewContent.transform).GetComponentInChildren<UIEnvironment>();
                environmentToggle.Initialize(i, this, AddonsController.instance.environmentObjects[i]);
            }

        }
        public void SetEnvironment(Toggle environmentToggle, int environmentId)
        {
            string environmentName = environmentToggle.transform.Find("Environment Name").GetComponent<TextMeshProUGUI>().text;

            // Add other environments when created
            if (environmentName == "Circular Environment")
            {
                selectedEnvironment = "Circular";
                AddonsController.instance.SetEnvironment(0);
            }
            else if (environmentName == "Museum Environment")
            {
                selectedEnvironment = "Museum";
                AddonsController.instance.SetEnvironment(1);
            }
            mainMenuPanel.currentEnvironmentId = environmentId;
        }

        public void SetOnlineMode(bool isOnline)
        {
            if (isOnlineMode == false)
            {
                isOnlineMode = true;
                Debug.Log("Modo Online establecido en: " + isOnlineMode);
            }
            else if(isOnlineMode == true)
            {
                isOnlineMode = false;
                Debug.Log("Modo Online establecido en: " + isOnlineMode);
            }

        }

        // All Configuration
        public void UnlockContinueButton()
        {
            // Only works with session but user Id and environment has to be selected THIS
            if (selectedSession != "" && selectedUserId > -1 && selectedEnvironment != "")
            {
                continueButton.interactable = true;
            }
            else
            {
                continueButton.interactable = false;
            }
        }

        public void GoToCategoryConfig()
        {
            sessionManager.sessionName = selectedSession;
            sessionManager.userId = selectedUserId;
            sessionManager.environmentName = selectedEnvironment;
            sessionManager.isOnlineSession = isOnlineMode; // Enviar el modo online/offline
            sessionManager.categoryController.Initialize();
            mainMenuPanel.ChangeVisibleComponent((int)MainMenuId.CategorySelection);
        }


        #endregion

        #region Persistence

        // Sessions will be saved and loaded from a file in persistent data folder

        // SESSION DEPENDANT (Will be used after the controller has started)
        public void SaveSessions()
        {
            SessionSaveData newSessionSaveData = new SessionSaveData();
            newSessionSaveData.sessions = sessions;

            string json = JsonUtility.ToJson(newSessionSaveData);

            File.WriteAllText(Application.persistentDataPath + "/Sessions.json", json);
        }

        public void LoadSessions()
        {
            string path = Application.persistentDataPath + "/Sessions.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                sessions = JsonUtility.FromJson<SessionSaveData>(json).sessions;
            }
            else
            {
                // If there is no file we create one 
                List<string> sessions = new List<string>();

                this.sessions = sessions;
                SaveSessions();
            }
        }

        // SESSION INDEPENDENT (Can be used without initializing the controller)

        public void DeleteSession(string sessionName)
        {
            // Delete all categories under session 
            sessionManager.categoryController.DeleteCategoriesFromSession(sessionName);

            // Delete all elements under session
            sessionManager.elementCategoryController.DeleteElementsFromSession(sessionName);

            // Deletes the session itself
            string path = Application.persistentDataPath + "/Sessions.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all sessions to eventually edit
                List<string> allSessions = JsonUtility.FromJson<SessionSaveData>(json).sessions;
                // Creates new SessionSaveData with all items but the specified sessionName
                SessionSaveData newSessionSaveData = new SessionSaveData();
                newSessionSaveData.sessions = new List<string>();

                foreach (string session in allSessions)
                {
                    // Add only the sessions that are not the specified one
                    bool addEntry = true;
                    if (session == sessionName)
                    {
                        addEntry = false;
                    }
                    if (addEntry)
                    {
                        newSessionSaveData.sessions.Add(session);
                    }
                }

                json = JsonUtility.ToJson(newSessionSaveData);

                File.WriteAllText(Application.persistentDataPath + "/Sessions.json", json);

                // Delete Results folder
                string filename = Path.Combine(Application.dataPath + "/Results");

                filename = Path.Combine(filename, sessionName);

                if (Directory.Exists(filename))
                {
                    Directory.Delete(filename, true);
                }

            }
        }
        #endregion

        #region UI Alert

        private IEnumerator SetAlert(string alertMessage)
        {
            alertCoroutineRunning = true;
            // Set message to alert
            alertText.text = alertMessage;

            // Initiate operation to change its opacity to 1 then 0
            CanvasGroup alertTextCanvasGroup = alertText.gameObject.GetComponent<CanvasGroup>();

            float timer = 0;
            while (timer <= alertFadeTime)
            {
                float newAlpha = Mathf.Lerp(0, 1, timer / alertFadeTime);
                alertTextCanvasGroup.alpha = newAlpha;

                timer += Time.deltaTime;
                yield return null;
            }
            alertTextCanvasGroup.alpha = 1;

            yield return new WaitForSeconds(alertDuration);

            timer = 0;
            while (timer <= alertFadeTime)
            {
                float newAlpha = Mathf.Lerp(1, 0, timer / alertFadeTime);
                alertTextCanvasGroup.alpha = newAlpha;

                timer += Time.deltaTime;
                yield return null;
            }
            alertTextCanvasGroup.alpha = 0;
            alertCoroutineRunning = false;
        }

        #endregion


    }

    #region Persistance classes

    [System.Serializable]
    public class SessionSaveData
    {
        public List<string> sessions;
    }

    #endregion
}


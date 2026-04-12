using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

using System.Linq;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vortices
{
    enum Tools
    {
        // Change this when order is changed or when new submenus are added
        Base = 0,   
        CategorizeEmpty = 1,
        Categorize = 2,
        Sort = 3,
        Chat = 4
    }

    public class RighthandTools : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject UIElementCategoryPrefab;
        [SerializeField] private GameObject UISortCategoryPrefab;
        [SerializeField] private GameObject categorySelectorContent;
        [SerializeField] private GameObject sortCategoryContent;


        // Panel UI Components
        [SerializeField] public List<GameObject> toolsUiComponents;
        [SerializeField] public List<Toggle> panelToggles;


        public List<UIElementCategory> UIElementCategories;
        public List<Toggle> UISortCategories;

        // Panel Properties
        public int actualComponentId { get; set; }

        // Data
        public List<List<string>> sessionCategoriesCount;
        public string actualSortCategory;

        // Selection
        public Element actualSelectedElement;
        public List<Element> allElements;
        // Selection Data
        public ElementCategory elementCategory; // This element categories object
        public List<string> elementSelectedCategories;
        public bool hadElement;

        // Settings
        public float sortingTime = 2.5f;

        // Other
        Color normalColor = new Color(0.2956568f, 0.3553756f, 0.4150943f, 1.0f);
        Color disabledColor = Color.black;

        // Coroutine
        private bool isChangePanelRunning;
        private bool isUpdateRunning;
        private bool isSortingRunning;

        // Auxiliary References
        private SessionManager sessionManager;
        public NewChatManager chatManager;


        private void Update()
        {
            if (hadElement && actualSelectedElement == null && actualComponentId == (int)Tools.Categorize)
            {
                StartCoroutine(ChangePanelSelectedCoroutine());
            }
        }

        // Called by Session Manager
        public void Initialize()
        {
            // Canvas has to have main camera
            Canvas canvas = GetComponent<Canvas>();
            canvas.worldCamera = Camera.main;

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();

            // Make visible
            Fade toolsFader = GetComponent<Fade>();
            StartCoroutine(toolsFader.FadeInCoroutine());

            // Initialize tools
            InitializeToolsCategories();

            // Initialze teleportation if environment needs it
            if (sessionManager.environmentName == "Museum")
            {
                // For each floor item subscribe teleportation to logging
                TeleportationArea area1 = GameObject.Find("Floor_01").GetComponent<TeleportationArea>();
                TeleportationArea area2 = GameObject.Find("Floor_Hall_01").GetComponent<TeleportationArea>();
                TeleportationArea area3 = GameObject.Find("Ground_01").GetComponent<TeleportationArea>();

                area1.teleporting.AddListener(delegate { StartCoroutine(sessionManager.loggingController.LogTeleportation()); });
                area2.teleporting.AddListener(delegate { StartCoroutine(sessionManager.loggingController.LogTeleportation()); });
                area3.teleporting.AddListener(delegate { StartCoroutine(sessionManager.loggingController.LogTeleportation()); });
            }

        }

        #region User Input

        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }

        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = toolsUiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            toolsUiComponents[actualComponentId].SetActive(false);
            // Enable new component
            toolsUiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // FadeIn new component
            FadeUI newComponentFader = toolsUiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
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

            // Change component using toggle parent name
            if (toggle.transform.parent.name == "Categorize Toggle" && actualSelectedElement != null)
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.Categorize));
            }
            else if (toggle.transform.parent.name == "Categorize Toggle" && actualSelectedElement == null)
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.CategorizeEmpty));
            }
            else if (toggle.transform.parent.name == "Sort Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.Sort));
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

        public void ApplySort(Toggle toggle)
        {
            // If a sort is applied, unapply
            if (actualSortCategory == toggle.transform.parent.name)
            {
                if (!isSortingRunning)
                {
                    StartCoroutine(UnapplySortCoroutine(toggle));
                }
            }
            else
            {
                if (!isSortingRunning)
                {
                    StartCoroutine(ApplySortCoroutine(toggle));
                }
            }
        }

        public IEnumerator ApplySortCoroutine(Toggle toggle)
        {
            isSortingRunning = true;

            // Turn all toggles uninteractable with color disabled 
            foreach (Toggle panelToggle in UISortCategories)
            {
                // They have to have color disabled
                ColorBlock disabled = toggle.colors;
                disabled.disabledColor = disabledColor;
                panelToggle.colors = disabled;

                panelToggle.interactable = false;
            }

            // DO STUFF REGARDING SORTING
            // You get the category name
            string categoryName = toggle.transform.parent.name;
            // You ask elementCategoryController for all the urls that are in that category within that user
            List<string> categoryUrls = sessionManager.elementCategoryController.GetCategoryUrls(categoryName);
            // You disable the main base
            sessionManager.spawnController.placementBase.gameObject.SetActive(false);
            // You create sorting base
            sessionManager.spawnController.UpdateSortBase(categoryUrls);

            actualSortCategory = toggle.transform.parent.name;
            // After the sort is done, to unsort you click again the toggle
            yield return new WaitForSeconds(sortingTime);
            toggle.interactable = true;

            isSortingRunning = false;
        }

        public IEnumerator UnapplySortCoroutine(Toggle toggle)
        {
            isSortingRunning = true;

            // DO STUFF REGARDING SORTING
            sessionManager.spawnController.DestroySortBase();
            sessionManager.spawnController.placementBase.gameObject.SetActive(true);
            // You return the gizmo control to the placementBase in the enviroments that use the gizmo
            if(sessionManager.environmentName == "Circular")
            {
                GameObject.Find("LeftHand Controller").GetComponent<MoveGizmo>().Initialize(sessionManager.spawnController.placementBase.GetComponent<CircularSpawnBase>());
                GameObject.Find("RightHand Controller").GetComponent<MoveGizmo>().Initialize(sessionManager.spawnController.placementBase.GetComponent<CircularSpawnBase>());
            }

            actualSortCategory = "None";
            yield return new WaitForSeconds(1.0f);
            // Turn all toggles interactable with color normal
            foreach (Toggle panelToggle in UISortCategories)
            {
                // They have to have color disabled normal
                ColorBlock disabledBlack = panelToggle.colors;
                disabledBlack.disabledColor = disabledColor;
                panelToggle.colors = disabledBlack;
                panelToggle.interactable = true;
            }

            isSortingRunning = false;
        }

        public IEnumerator ChangePanelSelectedCoroutine()
        {
            bool categorizeStatus = toolsUiComponents[(int)Tools.Categorize].activeInHierarchy;
            bool categorizeEmptyStatus = toolsUiComponents[(int)Tools.CategorizeEmpty].activeInHierarchy;
            if (categorizeStatus || categorizeEmptyStatus)
            {
                isChangePanelRunning = true;

                // Turn all toggles uninteractable with color normal
                foreach (Toggle panelToggle in panelToggles)
                {

                    // They have to have color disabled normal
                    ColorBlock disabledNormal = panelToggle.colors;
                    disabledNormal.disabledColor = normalColor;
                    panelToggle.colors = disabledNormal;

                    panelToggle.interactable = false;
                }

                // Change component using toggle parent name
                if (actualSelectedElement == null)
                {
                    yield return StartCoroutine(ChangeComponent((int)Tools.CategorizeEmpty));
                }
                else 
                {
                    yield return StartCoroutine(ChangeComponent((int)Tools.Categorize));
                }

                // Turn all toggles interactable with color normal except the one that was pressed
                foreach (Toggle panelToggle in panelToggles)
                {
                    // They have to have color disabled normal
                    ColorBlock disabledBlack = panelToggle.colors;
                    disabledBlack.disabledColor = disabledColor;
                    panelToggle.colors = disabledBlack;
                    panelToggle.interactable = true;
                }

                isChangePanelRunning = false;
            }
        }


        #endregion

        #region Data Operations

        public void UpdateCategorizeSubMenu(Element selectedElement)
        {
            if (!hadElement)
            {
                hadElement = true;
            }

            if (actualSelectedElement != null && selectedElement != actualSelectedElement)
            {
                actualSelectedElement.selected = false;

                Renderer selectionBoxRenderer = actualSelectedElement.headInteractor.GetComponent<Renderer>();
                selectionBoxRenderer.material.color = Color.yellow;
                Color selectionRendererColor = selectionBoxRenderer.material.color;
                // If out of hover it becomes invisible
                selectionBoxRenderer.material.color = new Color(selectionRendererColor.r,
                                                                selectionRendererColor.g,
                                                                selectionRendererColor.b, 0f);

                if (sessionManager.environmentName == "Museum")
                {
                    Renderer frameRenderer = actualSelectedElement.transform.parent.GetComponent<Renderer>();
                    Color frameRendererColor = frameRenderer.material.color;
                    frameRenderer.material.color = new Color(selectionRendererColor.r,
                                                            selectionRendererColor.g,
                                                            selectionRendererColor.b, 1f);
                }
            }

            elementSelectedCategories = new List<string>();

            // Element will search for its categories in ElementCategoryController and will apply them to the categorySelector UI object
            GetSelectedCategories(selectedElement);
            actualSelectedElement = selectedElement;
            // Update Categorized
            UpdateCategorized(false);
            if (actualComponentId != (int)Tools.Categorize)
            {
                StartCoroutine(ChangePanelSelectedCoroutine());
            }
        }

        public void InitializeToolsCategories()
        {
            UIElementCategories = new List<UIElementCategory>();
            UISortCategories = new List<Toggle>();
            // Search for the categories to be used in CategoryController and it will add them to categorySelector UI object
            AddUICategories();
            SortUICategories();
            AddUISortingCategories();
        }

        public void AddUICategories()
        {
            // Clear past UI Categories
            foreach (Transform child in categorySelectorContent.transform)
            {
                Destroy(child.gameObject);
            }

            // Ask CategoryController for every category to add
            List<string> categories = sessionManager.categoryController.GetCategories();
            foreach (string category in categories)
            {
                // Add to UI component
                AddCategoryToScrollView(category);
            }
        }

        private void AddCategoryToScrollView(string categoryName)
        {
            CreateCategory(categoryName);
            // Updates rows
            SortUICategories();
        }

        private void SortUICategories()
        {
            UIElementCategories = UIElementCategories.OrderBy(category => category.categoryName).ToList();
            for (int i = 0; i < UIElementCategories.Count; i++)
            {
                UIElementCategories[i].transform.SetSiblingIndex(i);
            }
        }


        private void CreateCategory(string categoryName)
        {
            UIElementCategory newCategory = Instantiate(UIElementCategoryPrefab, categorySelectorContent.transform).GetComponent<UIElementCategory>();
            // Initialize
            newCategory.Init(categoryName, this);

            // Add gameobject to list for easy access
            UIElementCategories.Add(newCategory);

            // Sometimes the UI elements deactivate, activate if so
            HorizontalLayoutGroup horizontalLayoutGroup = newCategory.GetComponent<HorizontalLayoutGroup>();
            LayoutElement layoutElement = newCategory.GetComponent<LayoutElement>();
            if (!horizontalLayoutGroup.isActiveAndEnabled)
            {
                horizontalLayoutGroup.gameObject.SetActive(true);
            }
            if (!layoutElement.isActiveAndEnabled)
            {
                layoutElement.gameObject.SetActive(true);
            }
        }

        public void AddUISortingCategories()
        {
            // Clear past UI Categories
            foreach (Transform child in sortCategoryContent.transform)
            {
                Destroy(child.gameObject);
            }
            UISortCategories.Clear();
            // Get actual category count
            GetAllCategoryCount();
            // Add category that has elements within it to sorting menu
            foreach (List<string> categoryCount in sessionCategoriesCount)
            {
                if (int.Parse(categoryCount[1]) > 0)
                {
                    GameObject UISortingToggle = Instantiate(UISortCategoryPrefab, sortCategoryContent.transform);
                    Toggle sortingToggle = UISortingToggle.transform.GetComponentInChildren<Toggle>();
                    sortingToggle.transform.parent.gameObject.name = categoryCount[0];
                    UISortCategories.Add(sortingToggle);
                    List<TextMeshProUGUI> sortingToggleTexts = UISortingToggle.transform.GetComponentsInChildren<TextMeshProUGUI>().ToList();
                    sortingToggleTexts[0].text = categoryCount[0];
                    sortingToggleTexts[1].text = categoryCount[1];

                    // Subscribe to applysort
                    sortingToggle.onValueChanged.AddListener(delegate { ApplySort(sortingToggle); });
                }
            }
        }


        // Extracts categories from elementCategoryController of an element
        public void GetSelectedCategories(Element selectedElement)
        {
            // Get selectedCategories if there is any, if there isnt, create a blank entry of Element Category
            elementCategory = sessionManager.elementCategoryController.GetSelectedCategories(selectedElement.url);
            elementSelectedCategories = elementCategory.elementCategories;
            // Update UI Elements with said selected categories
            // Set all categories to false
            foreach (UIElementCategory category in UIElementCategories)
            {
                category.changeSelection = false;
                category.SetToggle(false);
                category.changeSelection = true;
            }
            // Set found ones to true
            foreach (string category in elementSelectedCategories)
            {
                UIElementCategory categoryToSelect = UIElementCategories.FirstOrDefault<UIElementCategory>(searchCategory => searchCategory.categoryName == category);

                if(categoryToSelect != null)
                {
                    categoryToSelect.changeSelection = false;
                    categoryToSelect.SetToggle(true);
                    categoryToSelect.changeSelection = true;
                }
            }
        }

        // Extracts categories from elementCategoryController of all elements and counts them
        public void GetAllCategoryCount()
        {
            // Get categories from elementCategoryController
            sessionCategoriesCount = sessionManager.elementCategoryController.GetSessionCategoriesCount();
        }

        public void AddToSelectedCategories(string categoryName)
        {
            if (actualSelectedElement != null)
            {
                // Log category addition
                sessionManager.loggingController.LogCategory(actualSelectedElement.url, true, categoryName);

                // Add to selected categories
                elementSelectedCategories.Add(categoryName);
                elementSelectedCategories.Sort();
                // Update Categorized
                UpdateCategorized(true);
                // Add to element categories
                elementCategory.elementCategories = elementSelectedCategories;
                // Send element categories back to category controller
                sessionManager.elementCategoryController.UpdateElementCategoriesList(actualSelectedElement.url, elementCategory);
                // Update Sorting counters
                AddUISortingCategories();

                if (sessionManager.isOnlineSession)
                {
                    if(sessionManager.environmentName == "Museum"){
                        MuseumBaseNetworkHandler.Instance.CmdUpdateCategory(actualSelectedElement.url, categoryName, true, actualSelectedElement.circularIndex);
                    }
                    else{
                        CircularNetworkHandler.Instance.CmdUpdateCategory(actualSelectedElement.url, categoryName, true, actualSelectedElement.circularIndex);
                    }
                }
            }
        }

        public void RemoveFromSelectedCategories(string categoryName)
        {
            if (actualSelectedElement != null)
            {
                // Log category addition
                sessionManager.loggingController.LogCategory(actualSelectedElement.url, false, categoryName);

                // Remove from selected categories
                elementSelectedCategories.Remove(categoryName);
                elementSelectedCategories.Sort();
                // Update Categorized
                UpdateCategorized(true);
                // Add to element categories
                elementCategory.elementCategories = elementSelectedCategories;
                // Send element categories back to category controller
                sessionManager.elementCategoryController.UpdateElementCategoriesList(actualSelectedElement.url, elementCategory);
                // Update Sorting counters
                AddUISortingCategories();

                if (sessionManager.isOnlineSession)
                {
                    if(sessionManager.environmentName == "Museum"){
                        MuseumBaseNetworkHandler.Instance.CmdUpdateCategory(actualSelectedElement.url, categoryName, false, actualSelectedElement.circularIndex);
                    }
                    else{
                        CircularNetworkHandler.Instance.CmdUpdateCategory(actualSelectedElement.url, categoryName, false, actualSelectedElement.circularIndex);
                    }
                }
            }
        }

        public void UpdateCategorized(bool all)
        {
            if (all)
            {
                allElements = sessionManager.elementCategoryController.elementGameObjects;
                foreach (Element element in allElements)
                {
                    List<string> elementCategories = sessionManager.elementCategoryController.GetSelectedCategories(element.url).elementCategories;

                    if (elementCategories.Count > 0)
                    {
                        element.SetCategorized(true);
                    }
                    else
                    {
                        element.SetCategorized(false);
                    }
                }
            }
            else
            {
                if (elementSelectedCategories.Count > 0)
                {
                    actualSelectedElement.SetCategorized(true);
                }
                else
                {
                    actualSelectedElement.SetCategorized(false);
                }
            }
        }

        public void OnChatToggleChanged(bool isOn)
        {
            Debug.Log("[OnChatToggleChanged] Iniciando búsqueda de NewChatManager...");

            if (chatManager == null)
            {
                GameObject chatCanvas = null;

                // Buscar entre objetos desactivados
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "ChatCanvas(Clone)" && obj.hideFlags == HideFlags.None)
                    {
                        chatCanvas = obj;
                        Debug.Log("[OnChatToggleChanged] ChatCanvas encontrado entre objetos desactivados.");
                        break;
                    }
                }

                if (chatCanvas == null)
                {
                    Debug.LogError("[OnChatToggleChanged] ChatCanvas global no encontrado.");
                    return;
                }

                // Obtener el NewChatManager del ChatCanvas
                chatManager = chatCanvas.GetComponent<NewChatManager>();
                if (chatManager == null)
                {
                    Debug.LogError("[OnChatToggleChanged] NewChatManager no encontrado en el ChatCanvas.");
                    return;
                }

                Debug.Log("[OnChatToggleChanged] NewChatManager asignado correctamente.");
            }

            // Alternar el chat
            chatManager.ToggleChat();
        }


        


        #endregion
    }
}


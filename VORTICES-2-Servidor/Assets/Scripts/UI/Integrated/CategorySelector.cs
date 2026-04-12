using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using TMPro;
using System.Linq;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;


namespace Vortices
{
    public class CategorySelector : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject scrollviewContent;
        [SerializeField] private TextInputField categoryAddInputField;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject horizontalLayoutPrefab;
        [SerializeField] private GameObject UICategoryPrefab;
        private CategoryController categoryController;
        private SessionController sessionController;

        // Data
        public List<string> categories;
        public List<string> selectedCategories;
        private List<UICategory> UICategories;
        private UICategory categoryToDelete;

        // Auxiliary References
        SessionManager sessionManager;

        private GameObject lastHorizontalGroup;

        private void OnEnable()
        {
            UnlockContinueButton();
        }

        public void Initialize()
        {
            selectedCategories = new List<string>();
            UICategories = new List<UICategory>();
            categories = new List<string>();

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            categoryController = GameObject.FindObjectOfType<CategoryController>();
            sessionController = GameObject.FindObjectOfType<SessionController>();

            // When initialized will try to load categories, will create a new category list otherwise
            categories = categoryController.GetCategories();
            // Categories will be added to UI Components
            UpdateCategories();
            // Update UI with selected categories
            GetSelectedCategories();
        }

        #region Data Operation;

        public void AddUICategory()
        {
            string categoryName = categoryAddInputField.GetData();
            // Add to UI component
            AddCategoryToScrollView(categoryName);
            // Save all categories to file
            categoryController.UpdateCategoriesList(categories);
        }

        public void RemoveUICategory(UICategory category)
        {
            // Remove from UI component
            RemoveCategoryFromScrollView(category);
            // Save all categories to file
            categoryController.UpdateCategoriesList(categories);
        }

        private void UpdateCategories()
        {
            // Identify Old horizontal rows
            foreach (Transform child in scrollviewContent.transform)
            {
                child.name = "Old Row";
            }

            // If UICategories is empty this means we create new objects to hold the categories
            if (UICategories.Count == 0)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                        lastHorizontalGroup = newHorizontalRow;
                    }

                    CreateCategory(categories[i], lastHorizontalGroup, false);
                }
            }
            // If UICategories is not empty it means we can reuse the ui categories
            else
            {
                categories = categories.OrderBy(category => category).ToList();
                UICategories = UICategories.OrderBy(category => category.categoryName).ToList();
                for (int i = 0; i < UICategories.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                        lastHorizontalGroup = newHorizontalRow;
                    }

                    UICategories[i].horizontalGroup = lastHorizontalGroup;
                    UICategories[i].transform.SetParent(lastHorizontalGroup.transform);
                }
            }
            // Delete old horizontal Rows

            foreach (Transform child in scrollviewContent.transform)
            {
                if (child.name == "Old Row")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void AddCategoryToScrollView(string categoryName)
        {
            // If total number of categories is 0 when mod 3, this means a new row has to be added
            if (categories.Count % 3 == 0)
            {
                GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                lastHorizontalGroup = newHorizontalRow;
            }

            CreateCategory(categoryName, lastHorizontalGroup, true);
            // Updates rows
            UpdateCategories();
        }

        private void RemoveCategoryFromScrollView(UICategory category)
        {
            // Deletion guard
            if (categoryToDelete == null)
            {
                categoryToDelete = category;
                categoryToDelete.nameText.text = "Erase data? ->";
                return;
            }
            // If other session is selected, return normal name
            else if (categoryToDelete != category)
            {
                categoryToDelete.nameText.text = categoryToDelete.categoryName;
                categoryToDelete = category;
                categoryToDelete.nameText.text = "Erase data? ->";
                return;
            }

            // Searches the UIComponents for category position
            GameObject horizontalGroup = category.horizontalGroup;
            string categoryName = category.categoryName;

            // Removes from list
            categories.Remove(categoryName);
            if (selectedCategories.Contains(categoryName))
            {
                selectedCategories.Remove(categoryName);
            }
            // Destroys said Component
            category.DestroyCategory();
            // Destroys UI Category
            UICategories.Remove(category);
            // Updates rows
            UpdateCategories();

            // Removes category from data
            sessionManager.categoryController.DeleteCategory(sessionManager.sessionName, category.categoryName);

            categoryToDelete = null;
        }

        private void CreateCategory(string categoryName, GameObject horizontalGroup, bool addToList)
        {
            
            if (categoryName != "")
            {
                //Filters if category should be created by the rules specified in this function
                string result = "";

                if (addToList && categoryName != "")
                {
                    result = FilterCategory(categoryName);
                }
                else
                {
                    result = "OK";
                }

                if (result == "OK")
                {
                    UICategory newCategory = Instantiate(UICategoryPrefab, horizontalGroup.transform).GetComponent<UICategory>();
                    // Initialize
                    newCategory.Init(categoryName, this, horizontalGroup);

                    // Add category to category list (If its loaded, you dont add it again)
                    if (addToList)
                    {
                        categories.Add(newCategory.categoryName);
                    }
                    // Add gameobject to list for easy access
                    UICategories.Add(newCategory);

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
                else if (result == "Same")
                {
                    categoryAddInputField.SetText("");
                    categoryAddInputField.placeholder.text = "Category already exists.";
                }
            }
        }

        public void AddToSelectedCategories(string categoryName)
        {
            // Add to selected categories
            selectedCategories.Add(categoryName);
            selectedCategories.Sort();
            // Send categories back to category controller
            categoryController.UpdateSelectedCategoriesList(selectedCategories);
        }

        public void RemoveFromSelectedCategories(string categoryName)
        {
            // Remove from selected categories
            selectedCategories.Remove(categoryName);
            selectedCategories.Sort();
            // Send element categories back to category controller
            categoryController.UpdateSelectedCategoriesList(selectedCategories);
        }

        private void GetSelectedCategories()
        {
            // Get selectedCategories if there is any, if there isnt, create a blank entry of Element Category
            selectedCategories = categoryController.GetSelectedCategories();
            // Update UI Elements with said selected categories
            // Set all categories to false
            foreach (UICategory category in UICategories)
            {
                category.changeSelection = false;
                category.SetToggle(false);
                category.changeSelection = true;
            }
            // Set found ones to true
            foreach (string category in selectedCategories)
            {
                UICategory categoryToSelect = UICategories.FirstOrDefault<UICategory>(searchCategory => searchCategory.categoryName == category);

                if (categoryToSelect != null)
                {
                    categoryToSelect.changeSelection = false;
                    categoryToSelect.SetToggle(true);
                    categoryToSelect.changeSelection = true;
                }
            }
        }

        private string FilterCategory(string categoryName)
        {
            // Check if category has been already added
            string newName = categoryName.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            if (categories.Contains(newName))
            {
                return "Same";
            }

            return "OK";
        }

        public void UnlockContinueButton()
        {
            if(selectedCategories != null)
            {
                if (selectedCategories.Count > 0)
                {
                    continueButton.interactable = true;
                }
                else
                {
                    continueButton.interactable = false;
                }
            }
        }

        public void ClearSelection()
        {
            foreach (UICategory category in UICategories)
            {
                category.selectToggle.isOn = false;
            }
            selectedCategories = new List<string>();
        }

        #endregion

    }
}

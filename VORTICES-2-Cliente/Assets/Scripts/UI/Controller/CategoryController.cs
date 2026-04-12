using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using UnityEngine.UIElements;

namespace Vortices
{
    public class CategoryController : MonoBehaviour
    {
        // This class will load the categories of every session to be chosen
        // Data
        private List<SessionCategory> allSessionCategory; // All sessions
        public List<string> categoriesList; // For this session
        public List<string> selectedCategoriesList;
        private CategorySelector categorySelector;

        // Settings
        private string sessionName { get; set; }
        private int userId { get; set; }

        // Auxiliary references
        private SessionManager sessionManager;

        private void Start()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
        }

        public void Initialize()
        {
            allSessionCategory = new List<SessionCategory>(); // All sessions
            categoriesList = new List<string>();
            selectedCategoriesList = new List<string>();

            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);

            this.sessionName = sessionManager.sessionName;
            this.userId = sessionManager.userId;

            LoadAllSessionCategories();
            categorySelector.Initialize();
        }

        #region Data Operations

        public List<string> GetCategories()
        {
            List<string> categories = categoriesList;
            if (categories != null)
            {
                return categories;
            }
            else
            {
                categories = new List<string>();
                UpdateSessionCategoriesList(categories);
                return categories;
            }
        }

        public List<string> GetSelectedCategories()
        {
            List<string> selectedcategories = selectedCategoriesList;
            if (selectedcategories != null)
            {
                return selectedcategories;
            }
            else
            {
                selectedcategories = new List<string>();
                UpdateSessionSelectedCategoriesList(selectedcategories);
                return selectedcategories;
            }
        }

        public void UpdateCategoriesList(List<string> updatedCategories)
        {
            categoriesList = updatedCategories;

            UpdateSessionCategoriesList(updatedCategories);
        }

        public void UpdateSelectedCategoriesList(List<string> updatedSelectedCategories)
        {
            selectedCategoriesList = updatedSelectedCategories;

            UpdateSessionSelectedCategoriesList(updatedSelectedCategories);
        }

        private void UpdateSessionCategoriesList(List<string> updatedCategoriesList)
        {
            SessionCategory oldSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
            if (oldSessionCategory != null)
            {
                int oldSessionCategoryIndex = allSessionCategory.IndexOf(oldSessionCategory);
                allSessionCategory[oldSessionCategoryIndex].categoriesList = updatedCategoriesList;
            }
            allSessionCategory = allSessionCategory.OrderBy(session => session.sessionName).ThenBy(session => session.userId).ToList();

            SaveAllSessionCategories();
        }

        private void UpdateSessionSelectedCategoriesList(List<string> updatedSelectedCategoriesList)
        {
            SessionCategory oldSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
            if (oldSessionCategory != null)
            {
                int oldSessionCategoryIndex = allSessionCategory.IndexOf(oldSessionCategory);
                allSessionCategory[oldSessionCategoryIndex].selectedCategoriesList = updatedSelectedCategoriesList;
            }
            allSessionCategory = allSessionCategory.OrderBy(session => session.sessionName).ThenBy(session => session.userId).ToList();

            SaveAllSessionCategories();
        }

        #endregion

        #region Persistence

        // All sessions category data will be saved and loaded from a file in persistent data folder
       
        // SESSION DEPENDANT (Will be used after a session has started)
        public void SaveAllSessionCategories()
        {
            CategorySaveData newCategorySaveData = new CategorySaveData();
            newCategorySaveData.allSessionCategory = allSessionCategory;

            string json = JsonUtility.ToJson(newCategorySaveData);

            File.WriteAllText(Application.persistentDataPath + "/Session categories.json", json);
            SaveSessionCategoriesToRootFolder();
        }

        public void LoadAllSessionCategories()
        {
            string path = Application.persistentDataPath + "/Session categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                allSessionCategory = JsonUtility.FromJson<CategorySaveData>(json).allSessionCategory;

                // Loads data relevant to session to use in the program
                SessionCategory thisSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName);
                if (thisSessionCategory != null)
                {
                    categoriesList = thisSessionCategory.categoriesList;
                    selectedCategoriesList = thisSessionCategory.selectedCategoriesList;
                    SaveAllSessionCategories();
                }
                // No data found, create one
                else
                {
                    SessionCategory newSessionCategory = new SessionCategory();
                    newSessionCategory.sessionName = this.sessionName;
                    newSessionCategory.userId = this.userId;
                    newSessionCategory.categoriesList = new List<string>();
                    newSessionCategory.selectedCategoriesList = new List<string>();

                    allSessionCategory.Add(newSessionCategory);
                    SaveAllSessionCategories();
                }

            }
            else
            {
                // If there is no file we create one and apply an empty all Session categories for it to be filled
                SessionCategory newSessionCategory = new SessionCategory();
                newSessionCategory.sessionName = this.sessionName;
                newSessionCategory.userId = this.userId;
                newSessionCategory.categoriesList = new List<string>();
                newSessionCategory.selectedCategoriesList = new List<string>();

                allSessionCategory.Add(newSessionCategory);
                SaveAllSessionCategories();
            }
        }

        public void SaveSessionCategoriesToRootFolder()
        {
            string filename = Path.Combine(Application.dataPath + "/Results");
            // File path depends on session name and user Id
            filename = Path.Combine(filename, sessionName);

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            filename = Path.Combine(filename, "Session Categories.csv");

            TextWriter tw = new StreamWriter(filename, false);
            tw.WriteLine("Categories");
            tw.Close();

            tw = new StreamWriter(filename, true);

            for (int i = 0; i < categoriesList.Count; i++)
            {
                if(!(i == categoriesList.Count - 1))
                {
                    tw.Write(categoriesList[i] + ";");
                }
                else
                {
                    tw.Write(categoriesList[i]);
                }
            }
            tw.WriteLine();
            tw.WriteLine("Selected Categories");
            for (int i = 0; i < selectedCategoriesList.Count; i++)
            {
                if(!(i == selectedCategoriesList.Count - 1))
                {
                    tw.Write(selectedCategoriesList[i] + ";");
                }
                else
                {
                    tw.Write(selectedCategoriesList[i]);
                }

            }
            tw.WriteLine();
            tw.Close();
        }

        // SESSION INDEPENDENT (Can be used without initializing a session)

        public void DeleteCategory(string sessionName, string categoryName)
        {
            // Deletes all elements under this category
            sessionManager.elementCategoryController.DeleteElementsFromCategory(sessionName, categoryName);
            // Deletes the category itself
            string path = Application.persistentDataPath + "/Session categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                List<SessionCategory> allSessionCategory = JsonUtility.FromJson<CategorySaveData>(json).allSessionCategory;
                // Creates new CategorySaveData with all items but the ones with categoryName in their categoriesList/selectedCategoriesList in specific sessionName
                CategorySaveData newCategorySaveData = new CategorySaveData();
                newCategorySaveData.allSessionCategory = new List<SessionCategory>();

                // Filter their SessionCategory
                foreach(SessionCategory sessionData in allSessionCategory)
                {
                    if(sessionData.sessionName == sessionName)
                    {
                        SessionCategory newSessionCategory = new SessionCategory();
                        newSessionCategory.sessionName = sessionData.sessionName;
                        newSessionCategory.userId = sessionData.userId;
                        newSessionCategory.categoriesList = new List<string>();
                        newSessionCategory.selectedCategoriesList = new List<string>();

                        foreach(string categoryEntry in sessionData.categoriesList)
                        {
                            // Add only the categories that are not from specified category
                            bool addEntry = true;
                            if (categoryEntry == categoryName)
                            {
                                addEntry = false;
                            }
                            if (addEntry)
                            {
                                newSessionCategory.categoriesList.Add(categoryEntry);
                            }
                        }

                        foreach (string selectedCategoryEntry in sessionData.selectedCategoriesList)
                        {
                            // Add only the categories that are not from specified category
                            bool addEntry = true;
                            if (selectedCategoryEntry == categoryName)
                            {
                                addEntry = false;
                            }
                            if (addEntry)
                            {
                                newSessionCategory.categoriesList.Add(selectedCategoryEntry);
                            }
                        }
                        newCategorySaveData.allSessionCategory.Add(newSessionCategory);
                    }
                    else
                    {
                        newCategorySaveData.allSessionCategory.Add(sessionData);
                    }
                }

                json = JsonUtility.ToJson(newCategorySaveData);

                File.WriteAllText(Application.persistentDataPath + "/Session categories.json", json);
                SaveSessionCategoriesToRootFolderGlobal(newCategorySaveData);
            }
        }

        public void DeleteCategoriesFromSession(string sessionName)
        {
            string path = Application.persistentDataPath + "/Session categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                List<SessionCategory> allSessionCategory = JsonUtility.FromJson<CategorySaveData>(json).allSessionCategory;
                // Creates new CategorySaveData with all items but the ones with categoryName in their categoriesList/selectedCategoriesList in specific sessionName
                CategorySaveData newCategorySaveData = new CategorySaveData();
                newCategorySaveData.allSessionCategory = new List<SessionCategory>();

                foreach(SessionCategory sessionData in allSessionCategory)
                {
                    // Add only the sessions with categories that are not sessionName
                    bool addEntry = true;
                    if(sessionData.sessionName == sessionName)
                    {
                        addEntry = false;
                    }
                    
                    if(addEntry)
                    {
                        newCategorySaveData.allSessionCategory.Add(sessionData);
                    }
                }

                json = JsonUtility.ToJson(newCategorySaveData);

                File.WriteAllText(Application.persistentDataPath + "/Session categories.json", json);
                SaveSessionCategoriesToRootFolderGlobal(newCategorySaveData);
            }
        }

        public void SaveSessionCategoriesToRootFolderGlobal(CategorySaveData newCategorySaveData)
        {
            // Saves all 

            // Saves newElementCategorySaveData which is created without starting a session
            string filename = "";

            foreach (SessionCategory sessionData in newCategorySaveData.allSessionCategory)
            {
                filename = Path.Combine(Application.dataPath + "/Results");

                filename = Path.Combine(filename, sessionData.sessionName);

                if (!Directory.Exists(filename))
                {
                    Directory.CreateDirectory(filename);
                }

                filename = Path.Combine(filename, "Session Categories.csv");

                TextWriter tw = new StreamWriter(filename, false);
                tw.WriteLine("Categories");
                tw.Close();

                tw = new StreamWriter(filename, true);

                for (int i = 0; i < sessionData.categoriesList.Count; i++)
                {
                    if (!(i == sessionData.categoriesList.Count - 1))
                    {
                        tw.Write(sessionData.categoriesList[i] + ";");
                    }
                    else
                    {
                        tw.Write(sessionData.categoriesList[i]);
                    }
                }
                tw.WriteLine();
                tw.WriteLine("Selected Categories");
                for (int i = 0; i < sessionData.selectedCategoriesList.Count; i++)
                {
                    if (!(i == sessionData.selectedCategoriesList.Count - 1))
                    {
                        tw.Write(sessionData.selectedCategoriesList[i] + ";");
                    }
                    else
                    {
                        tw.Write(sessionData.selectedCategoriesList[i]);
                    }

                }
                tw.WriteLine();
                tw.Close();
            }
        }



        #endregion


        #region Persistance classes

        // Deals with all the  categories from all sessions and user Ids, it has to be filtered into the correct session and user Id for use

        [System.Serializable]
        public class CategorySaveData
        {
            public List<SessionCategory> allSessionCategory;
        }

        [System.Serializable]
        public class SessionCategory
        {
            public string sessionName;
            public int userId;
            public List<string> categoriesList;  // Saves a list of categories to be selected
            public List<string> selectedCategoriesList;
        }

        #endregion
    }
}







using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using Unity.VisualScripting;

namespace Vortices
{ 
    // This class will load the categories of every element in a SpawnBase
    public class ElementCategoryController : MonoBehaviour
    {
        // Data
        private List<SessionElementCategory> allSessionElementCategory; // All sessions
        public List<ElementCategory> elementCategoriesList; // For this session
        public List<Element> elementGameObjects;

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
            allSessionElementCategory = new List<SessionElementCategory>(); // All sessions
            Debug.Log("ElementCategoryController inicializado.");
            elementCategoriesList = new List<ElementCategory>();
            elementGameObjects = new List<Element>();

            this.sessionName = sessionManager.sessionName;
            this.userId = sessionManager.userId;

            LoadAllSessionElementCategories();
        }

        #region Data Operations

        // Get operations
        public ElementCategory GetSelectedCategories(string url)
        {
            ElementCategory elementCategory = elementCategoriesList.FirstOrDefault<ElementCategory>(elementCategories => elementCategories.elementUrl == url);
            if(elementCategory != null)
            {
                return elementCategory;
            }
            else
            {
                elementCategory = new ElementCategory();
                elementCategory.elementCategories = new List<string>();
                elementCategory.elementUrl = url;
                UpdateElementCategoriesList(url, elementCategory);
                return elementCategory;
            }
        }

        public Element GetElementByUrl(string url)
        {
            return elementGameObjects.FirstOrDefault(e => e.url == url);
        }

        public List<List<string>> GetSessionCategoriesCount()
        {
            List<List<string>> listOfCategoriesCount = new List<List<string>>();
            // Ask categoryController for all the session categories
            List<string> sessionCategories = sessionManager.categoryController.categoriesList;
            // Search for categories in elementCategoriesList
            foreach (string sessionCategory in sessionCategories)
            {
                List<string> sessionCategoryList = new List<string>();
                sessionCategoryList.Add(sessionCategory);
                int sessionCategoryFound = 0;
                foreach (ElementCategory elementCategory in elementCategoriesList)
                {
                    foreach (string category in elementCategory.elementCategories)
                    {
                        if (category == sessionCategory)
                        {
                            sessionCategoryFound++;
                        }
                    }
                }
                sessionCategoryList.Add(sessionCategoryFound.ToString());
                listOfCategoriesCount.Add(sessionCategoryList);
            }

            return listOfCategoriesCount;
        }

        public List<string> GetCategoryUrls(string categoryName)
        {
            List<string> categoryUrls = new List<string>();
            bool hasCategory = false;
            foreach (ElementCategory elementCategory in elementCategoriesList)
            {
                hasCategory = false;
                foreach (string category in elementCategory.elementCategories)
                {
                    if (category == categoryName)
                    {
                        hasCategory = true;
                    }
                }

                if (hasCategory)
                {
                    string cut = @"file://";
                    string finalUrl = elementCategory.elementUrl.Replace(cut,"");
                    categoryUrls.Add(finalUrl);
                }
            }

            return categoryUrls;
        }


        //

        public void UpdateElementCategoriesList(string url, ElementCategory updatedElementCategory)
        {
            Debug.Log($"[DEBUG] Intentando actualizar categorías para URL: {url}");

            ElementCategory oldElementCategory = elementCategoriesList.FirstOrDefault(e => e.elementUrl == url);

            if (oldElementCategory != null)
            {
                int oldElementCategoryIndex = elementCategoriesList.IndexOf(oldElementCategory);

                //  Verificamos si hay cambios reales antes de actualizar
                if (!Enumerable.SequenceEqual(oldElementCategory.elementCategories.OrderBy(x => x), updatedElementCategory.elementCategories.OrderBy(x => x)))
                {
                    Debug.Log($"[DEBUG] Cambios detectados en categorías. Actualizando en índice {oldElementCategoryIndex}.");
                    elementCategoriesList[oldElementCategoryIndex] = updatedElementCategory;
                    
                    // Solo actualizamos si hubo un cambio real
                    UpdateSessionCategoryList(elementCategoriesList);
                }
                else
                {
                    Debug.Log("[DEBUG] No se detectaron cambios en las categorías. No se actualiza para evitar bucles.");
                }
            }
            else
            {
                Debug.Log($"[DEBUG] Nueva entrada para URL {url}. Agregando.");
                elementCategoriesList.Add(updatedElementCategory);

                // Solo guardamos si hay cambios reales
                UpdateSessionCategoryList(elementCategoriesList);
            }
        }

        private void UpdateSessionCategoryList(List<ElementCategory> updatedElementCategoryList)
        {
            if (allSessionElementCategory == null)
            {
                Initialize();
            }

            SessionElementCategory oldSessionElementCategory = allSessionElementCategory.FirstOrDefault(session => session.sessionName == this.sessionName && session.userId == this.userId);

            if (oldSessionElementCategory != null)
            {
                int oldSessionElementCategoryIndex = allSessionElementCategory.IndexOf(oldSessionElementCategory);

                //  Verificamos si hay cambios antes de sobrescribir
                if (!Enumerable.SequenceEqual(allSessionElementCategory[oldSessionElementCategoryIndex].elementCategoriesList.Select(e => e.elementCategories).SelectMany(x => x).OrderBy(x => x), 
                                            updatedElementCategoryList.Select(e => e.elementCategories).SelectMany(x => x).OrderBy(x => x)))
                {
                    Debug.Log($"[DEBUG] Cambios detectados en sesión. Guardando actualización.");
                    allSessionElementCategory[oldSessionElementCategoryIndex].elementCategoriesList = updatedElementCategoryList;
                    SaveAllSessionElementCategories();
                }
                else
                {
                    Debug.Log("[DEBUG] No se detectaron cambios en la sesión. No se guarda para evitar escrituras innecesarias.");
                }
            }
            else
            {
                Debug.Log($"[DEBUG] Nueva sesión detectada. Agregando nueva entrada.");
                SessionElementCategory newSessionElementCategory = new SessionElementCategory
                {
                    sessionName = this.sessionName,
                    userId = this.userId,
                    elementCategoriesList = updatedElementCategoryList
                };

                allSessionElementCategory.Add(newSessionElementCategory);
                SaveAllSessionElementCategories();
            }
        }

        #endregion

        #region Persistence

        // All sessions data will be saved and loaded from a file in persistent data folder, also will have a function to save as a csv in the program's folder but as separate files in different folders
        // with structure /Session/userId/categories.csv

        // SESSION DEPENDANT (Will be used after a session has started)
        public void SaveAllSessionElementCategories()
        {
            ElementCategorySaveData newElementCategorySaveData = new ElementCategorySaveData();
            newElementCategorySaveData.allSessionElementCategory = allSessionElementCategory;

            string json = JsonUtility.ToJson(newElementCategorySaveData);

            File.WriteAllText(Application.persistentDataPath + "/Session element categories.json", json);
            SaveSessionElementCategoriesToRootFolder();
        }

        public void LoadAllSessionElementCategories()
        {
            string path = Application.persistentDataPath + "/Session element categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                allSessionElementCategory = JsonUtility.FromJson<ElementCategorySaveData>(json).allSessionElementCategory;

                // Loads data relevant to session to use in the program
                SessionElementCategory thisSessionElementCategory = allSessionElementCategory.FirstOrDefault<SessionElementCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
                if(thisSessionElementCategory != null)
                {
                    elementCategoriesList = thisSessionElementCategory.elementCategoriesList;
                    SaveAllSessionElementCategories();
                }
                // No data found, create one
                else
                {
                    SessionElementCategory newSessionElementCategory = new SessionElementCategory();
                    newSessionElementCategory.sessionName = this.sessionName;
                    newSessionElementCategory.userId = this.userId;
                    newSessionElementCategory.elementCategoriesList = new List<ElementCategory>();

                    allSessionElementCategory.Add(newSessionElementCategory);
                    SaveAllSessionElementCategories();
                }
                
            }
            else
            {
                // If there is no file we create one and apply an empty all Session Element Categories for it to be filled
                SessionElementCategory newSessionElementCategory = new SessionElementCategory();
                newSessionElementCategory.sessionName = this.sessionName;
                newSessionElementCategory.userId = this.userId;
                newSessionElementCategory.elementCategoriesList = new List<ElementCategory>();

                allSessionElementCategory.Add(newSessionElementCategory);
                SaveAllSessionElementCategories();
            }
        }

        public void SaveSessionElementCategoriesToRootFolder()
        {
            string filename = Path.Combine(Application.dataPath + "/Results");

            // Saves only current session and id

            // File path depends on session name and user Id
            filename = Path.Combine(filename, sessionName);
            filename = Path.Combine(filename, userId.ToString());

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            filename = Path.Combine(filename, "Session Element Categories.csv");

            TextWriter tw = new StreamWriter(filename, false);
            tw.WriteLine("Url;Categories");
            tw.Close();

            tw = new StreamWriter(filename, true);

            for (int i = 0; i < elementCategoriesList.Count; i++)
            {
                tw.Write(elementCategoriesList[i].elementUrl + ";");
                for (int j = 0; j < elementCategoriesList[i].elementCategories.Count; j++)
                {
                    if (!(j == elementCategoriesList[i].elementCategories.Count - 1))
                    {
                        tw.Write(elementCategoriesList[i].elementCategories[j] + ";");
                    }
                    else
                    {
                        tw.Write(elementCategoriesList[i].elementCategories[j]);
                    }
                }
                tw.WriteLine();
            }
            tw.Close();
        }

        // SESSION INDEPENDENT (Can be used without initializing a session as they will use their own session independent file loaders)

        public void DeleteElementsFromCategory(string sessionName, string categoryName)
        {
            string path = Application.persistentDataPath + "/Session element categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data 
                List<SessionElementCategory> allSessionElementCategoryToDelete = JsonUtility.FromJson<ElementCategorySaveData>(json).allSessionElementCategory;

                // Creates new ElementCategorySaveData with all items but the ones with categoryName in their elementList in sessionName
                ElementCategorySaveData newElementCategorySaveData = new ElementCategorySaveData();
                newElementCategorySaveData.allSessionElementCategory = new List<SessionElementCategory>();
                // This list will contain all the current sessions and we deal with every elementCategoriesList which comes from sessionName, deleting categoryName entries


                foreach (SessionElementCategory sessionData in allSessionElementCategoryToDelete)
                {
                    if(sessionData.sessionName == sessionName)
                    {
                        SessionElementCategory newSessionElementCategory = new SessionElementCategory();
                        newSessionElementCategory.sessionName = sessionData.sessionName;
                        newSessionElementCategory.userId = sessionData.userId;
                        newSessionElementCategory.elementCategoriesList = new List<ElementCategory>();

                        // Filter their ElementCategory
                        foreach (ElementCategory elementEntry in sessionData.elementCategoriesList)
                        {
                            // Add only the elementEntries that are not from the specified category
                            bool addEntry = true;
                            foreach (string category in elementEntry.elementCategories)
                            {
                                if (category == categoryName)
                                {
                                    addEntry = false;
                                }
                            }
                            if (addEntry)
                            {
                                newSessionElementCategory.elementCategoriesList.Add(elementEntry);
                            }
                        }
                        newElementCategorySaveData.allSessionElementCategory.Add(newSessionElementCategory);
                    }
                    else
                    {
                        newElementCategorySaveData.allSessionElementCategory.Add(sessionData);
                    }
                }

                json = JsonUtility.ToJson(newElementCategorySaveData);

                File.WriteAllText(Application.persistentDataPath + "/Session element categories.json", json);
                SaveSessionElementCategoriesToRootFolderGlobal(newElementCategorySaveData);
            }
        }

        public void DeleteElementsFromSession(string sessionName)
        {
            string path = Application.persistentDataPath + "/Session element categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data 
                List<SessionElementCategory> allSessionElementCategoryToDelete = JsonUtility.FromJson<ElementCategorySaveData>(json).allSessionElementCategory;

                // Creates new ElementCategorySaveData with all items but the ones in sessionName
                ElementCategorySaveData newElementCategorySaveData = new ElementCategorySaveData();
                newElementCategorySaveData.allSessionElementCategory = new List<SessionElementCategory>();

                foreach (SessionElementCategory sessionData in allSessionElementCategoryToDelete)
                {
                    // Add only the sessions which are not sessionName
                    bool addEntry = true;
                    if (sessionData.sessionName == sessionName)
                    {
                        addEntry = false;
                    }
                    
                    if (addEntry)
                    {
                        newElementCategorySaveData.allSessionElementCategory.Add(sessionData);
                    }
                }

                json = JsonUtility.ToJson(newElementCategorySaveData);

                File.WriteAllText(Application.persistentDataPath + "/Session element categories.json", json);
                SaveSessionElementCategoriesToRootFolderGlobal(newElementCategorySaveData);
            }
        }

        public void SaveSessionElementCategoriesToRootFolderGlobal (ElementCategorySaveData newElementCategorySaveData)
        {
            // Saves all 

            // Saves newElementCategorySaveData which is created without starting a session
            string filename = "";

            foreach (SessionElementCategory sessionData in newElementCategorySaveData.allSessionElementCategory)
            {
                filename = Path.Combine(Application.dataPath + "/Results");

                filename = Path.Combine(filename, sessionData.sessionName);
                filename = Path.Combine(filename, sessionData.userId.ToString());

                if (!Directory.Exists(filename))
                {
                    Directory.CreateDirectory(filename);
                }

                filename = Path.Combine(filename, "Session Element Categories.csv");

                TextWriter tw = new StreamWriter(filename, false);
                tw.WriteLine("Url;Categories");
                tw.Close();

                tw = new StreamWriter(filename, true);

                for (int i = 0; i < sessionData.elementCategoriesList.Count; i++)
                {
                    tw.Write(sessionData.elementCategoriesList[i].elementUrl + ";");
                    for (int j = 0; j < sessionData.elementCategoriesList[i].elementCategories.Count; j++)
                    {
                        if (!(j == sessionData.elementCategoriesList[i].elementCategories.Count - 1))
                        {
                            tw.Write(sessionData.elementCategoriesList[i].elementCategories[j] + ";");
                        }
                        else
                        {
                            tw.Write(sessionData.elementCategoriesList[i].elementCategories[j]);
                        }
                    }
                    tw.WriteLine();
                }
                tw.Close();
            }
        }
    }
    #endregion


    #region Persistance classes

    // Deals with all the element categories from all sessions and user Ids, it has to be filtered into the correct session and user Id for use
    [System.Serializable]
    public class ElementCategorySaveData
    {
        public List<SessionElementCategory> allSessionElementCategory;
    }

    [System.Serializable]
    public class SessionElementCategory
    {
        public string sessionName;
        public int userId;
        public List<ElementCategory> elementCategoriesList;  // Saves a list of every element categories
    }

    [System.Serializable]
    public class ElementCategory
    {
        public string elementUrl; // This connects a file or a online url to its categories (If the name of file or page changes, its categories are lost with it)
        public List<string> elementCategories;
    }
    
    #endregion
}


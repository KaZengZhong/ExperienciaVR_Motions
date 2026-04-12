using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

using System;
using Newtonsoft.Json;
using System.Linq;

// Localization system v2 (Loads from all folders, general use, not bound to folder format)
// To fetch a string you inherit LocaleComponent and use GetString
// First key is string group
// Second key is string element
public class LocalizationController : MonoBehaviour
{
    [Header("Settings")]
    // Components
    [Tooltip("Set this dropdown to the one that will control language, it will be filled with options automatically if the language files are present.")]
    [SerializeField] public TMP_Dropdown languageDropdown;

    // Events
    public static event Action OnLanguageChange;

    // Settings
    private string defaultLocale = "en";
    [Header("Info")]
    public string currentLocale;

    // Data
    public List<string> locales;
    public List<Dictionary<string, string>> groupStrings;
    public bool initialized;

    public static LocalizationController instance;

    #region Initialize
    public void Initialize()
    {
        // Instance initializing
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Load all possible locales
        locales = LoadLocales();

        // Set default language
        currentLocale = defaultLocale;
        ApplyLocaleDropdown(null);

        // Get text for all locales
        LoadStrings();

        // Apply
        ApplyLocale();
    }

    public void ApplyLocale()
    {
        // Apply locale to textboxes
        OnLanguageChange?.Invoke();
    }

    #endregion

    #region Data Operations

    // Aplies locale to dropdown (Has parameter if needed to add more language selectors over the application)
    public void ApplyLocaleDropdown(TMP_Dropdown dropdown)
    {
        if (languageDropdown == null)
        {
            languageDropdown = dropdown;
        }

        languageDropdown.onValueChanged.AddListener(UpdateLocale);

        // Connect 
        languageDropdown.ClearOptions();
        int currentOptionIndex = 0;

        for (int i = 0; i < locales.Count; i++)
        {
            if (defaultLocale == locales[i])
            {
                currentOptionIndex = i;
            }
        }

        languageDropdown.AddOptions(locales);
        languageDropdown.value = currentOptionIndex;
        languageDropdown.RefreshShownValue();
    }
    // Fetches string from currentLocaleStrings
    public string FetchString(string groupKey, string stringKey)
    {
        if (stringKey == "")
        {
            return "";
        }

        // Get the dictionaries belonging to one group
        List<Dictionary<string, string>> groupDictionaries = new List<Dictionary<string, string>>();
        foreach (Dictionary<string, string> group in groupStrings)
        {
            group.TryGetValue("groupKey", out var dictionaryGroupKey);
            if (dictionaryGroupKey == groupKey)
            {
                groupDictionaries.Add(group);
            }
        }
        // Get the dictionary that belongs to the current language
        foreach (Dictionary<string, string> languageDictionary in groupDictionaries)
        {
            languageDictionary.TryGetValue("languageKey", out var dictionaryLanguageKey);
            if (dictionaryLanguageKey == currentLocale)
            {
                // Load String
                if (languageDictionary.TryGetValue(stringKey, out var localizedString))
                {
                    return localizedString;
                }
            }
        }

        ErrorController.instance.ShowError("One or more strings were not found (Task: " + groupKey + ", " + stringKey + " )", 5);

        return "";
    }

    public void UpdateLocale(int localeIndex)
    {
        // Dropdown changed, this means the baseStrings and the experience string must be fetched again
        currentLocale = languageDropdown.options[localeIndex].text;
        ApplyLocale();
    }

    #endregion

    #region Persistence

    // Locale files are part of the application streamingAssets and they will only be loaded on start and when using the language dropdown
    // NOTE: They will only be added to the dropdown list if ALL the folders contain said language file, if a language is missing, it will probably be because a language file is missing in some language folder
    public List<string> LoadLocales()
    {
        string path = Application.streamingAssetsPath + "/Localization";

        if (!Directory.Exists(path))
        {
            ErrorController.instance.ShowError("No language folder", 5);
            return null;
        }

        locales = new List<string>();
        HashSet<string> hashLocales = new HashSet<string>();

        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        DirectoryInfo[] folders = directoryInfo.GetDirectories();

        if (folders.Length > 0)
        {
            string[] filesInFirstFolder = FolderHelper.GetFileNames(folders[0], "json");

            // Check rest of folders
            if (folders.Length > 1)
            {
                for (int i = 1; i < folders.Length; i++)
                {
                    string[] filesInCurrentFolder = FolderHelper.GetFileNames(folders[i], "json");

                    // Find the common files
                    string[] commonFilesInFolders = filesInFirstFolder.Intersect(filesInCurrentFolder).ToArray();

                    foreach (string commonName in commonFilesInFolders)
                    {
                        hashLocales.Add(commonName);
                    }
                }
            }
            else
            {
                foreach (string fileName in filesInFirstFolder)
                {
                    hashLocales.Add(fileName);
                }
            }

            locales = hashLocales.ToList<string>();
            if (locales.Count == 0)
            {
                ErrorController.instance.ShowError("No language found, check that all groups have locale files", 5);
                return null;
            }

            // Count the common files, if the number is less than the files in first folder, show error that a file is missing
            if (locales.Count != filesInFirstFolder.Length)
            {
                ErrorController.instance.ShowError("One or more language group folders are missing a locale file", 5);
            }
            // Filter the locales

            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i].Contains("locale-"))
                {
                    locales[i] = locales[i].Replace("locale-", "");
                    locales[i] = locales[i].Remove(2);
                }
                else
                {
                    locales.RemoveAt(i);
                }
            }
        }
        else
        {
            ErrorController.instance.ShowError("No language groups exist inside Localization folder", 5);
            return null;
        }

        return locales;
    }

    public void LoadStrings()
    {
        groupStrings = new List<Dictionary<string, string>>();

        // Load from json strings
        string path = Application.streamingAssetsPath + "/Localization";

        if (!Directory.Exists(path))
        {
            ErrorController.instance.ShowError("No language folder", 5);
            return;
        }

        // For each group
        foreach (string subDirectoryPath in Directory.GetDirectories(path))
        {
            // Add every language for easy access
            foreach (string filePath in Directory.GetFiles(subDirectoryPath))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.Contains("locale-") && Path.GetExtension(filePath) == ".json")
                {
                    string json = File.ReadAllText(filePath);
                    Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    
                    // Add metadata directory entries for identification
                    dictionary.Add("groupKey", Path.GetFileName(subDirectoryPath));

                    string languageKey = fileName.Replace("locale-", "").Remove(2);
                    dictionary.Add("languageKey", languageKey);

                    groupStrings.Add(dictionary);
                }
            }
        }
        initialized = true;
    }

    #endregion
}

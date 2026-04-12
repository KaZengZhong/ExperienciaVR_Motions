using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using Vortices;

// Loads addons as Asset bundles in Unity
public class AddonsController : MonoBehaviour
{
    // Data
    public List<Addon> allAddonsData;

    public string filePath;
    public string addonsPath;
    public List<EnvironmentObject> environmentObjects;

    public EnvironmentObject currentEnvironmentObject;

    // Coroutine

    // Properties

    public static AddonsController instance;

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

        // Create JSON
        filePath = Path.GetDirectoryName(Application.dataPath) + "/addons.json";
        addonsPath = Path.GetDirectoryName(Application.dataPath) + "/Addons";

        // Create file to communicate with launcher if there is none
        SaveAddonData();
        // Load Addons Data
        LoadAddonData();

        // Test bundle environment scene transition
        /*LoadAddonObjects();
        currentEnvironmentObject = environmentObjects[0];
        SceneTransitionManager.instance.GoToScene();
        */
    }
    #endregion

    #region Data operations

    private AddonsData GenerateBaseAddonData()
    {
        AddonsData addonsData = new AddonsData();

        addonsData.addonsData = new AddonData();
        addonsData.addonsData.addons = new List<Addon>();

        // For each folder in addons it will add every addon of every type

        string[] folderPaths = Directory.GetDirectories(addonsPath);
        List<string> folderNames = new List<string>(); 

        foreach (string folderPath in folderPaths)
        {
            folderNames.Add(Path.GetFileName(folderPath));
        }

        foreach (string folderName in folderNames)
        {
            string addonFolderPath = addonsPath + "/" + folderName;
            int id = 0;
            if (folderName == "Environment")
            {
                // Add environment addon for files with the same starting part, for environment it should be two files
                List<List<string>> environmentAddonGroups = FileUtilities.GetGroupedFilenamesInDirectory(addonFolderPath, ".bundle");

                foreach (List<string> environmentAddonGroup in environmentAddonGroups)
                {
                    // Fill environment addon
                    Addon environmentAddon = new Addon();
                    environmentAddon.addonFileNames = new List<string>();
                    environmentAddon.addonType = "Environment";
                    environmentAddon.addonId = id++;

                    long totalAddonSize = 0;
                    // Panel
                    string bundlePath = Path.Combine(addonFolderPath, environmentAddonGroup[0]);
                    AssetBundle panelBundle = AssetBundle.LoadFromFile(bundlePath);
                    environmentAddon.addonFileNames.Add(Path.GetFileName(environmentAddonGroup[0]));
                    if (panelBundle != null)
                    {
                        panelBundle.Unload(false);
                    }
                    totalAddonSize += SizeFormatter.GetFileSize(bundlePath);
                    // Scene
                    bundlePath = Path.Combine(addonFolderPath, environmentAddonGroup[1]);
                    AssetBundle sceneBundle = AssetBundle.LoadFromFile(bundlePath);
                    environmentAddon.addonFileNames.Add(Path.GetFileName(environmentAddonGroup[1]));
                    environmentAddon.enabled = false;
                    totalAddonSize += SizeFormatter.GetFileSize(bundlePath);
                    // Name
                    string[] parts = sceneBundle.GetAllScenePaths()[0].Split('/');
                    string assetNameWithExtension = parts[parts.Length - 1];
                    string assetNameWithoutExtension = assetNameWithExtension.Split('.')[0];
                    if (sceneBundle != null)
                    {
                        sceneBundle.Unload(false);
                    }

                    environmentAddon.addonName = assetNameWithoutExtension;
                    environmentAddon.addonSize = SizeFormatter.FormatSize(totalAddonSize);

                    addonsData.addonsData.addons.Add(environmentAddon);
                }
            }
        }
        return addonsData;
    }

    #endregion

    #region Environment Addons
    public void SetEnvironment(int index)
    {
        currentEnvironmentObject = environmentObjects[index];
    }

    public void ClearEnvironment()
    {
        foreach (EnvironmentObject environmentObject in environmentObjects)
        {
            environmentObject.sceneBundle.Unload(false);
        }
    }

    #endregion

    #region Addon Loading

    // Load Addons into the application using <AssetBundles> (Only prefabs)
    public void LoadAddonObjects()
    {
        environmentObjects = new List<EnvironmentObject>();
        // Search for all the Addons in addonsData
        
        foreach (Addon addon in allAddonsData)
        {
            if(!addon.enabled)
            {
                continue;
            }

            if (addon.addonType == "Environment")
            {
                // Found Addon is an Environment, load objects as such
                EnvironmentObject environmentObject = new EnvironmentObject();
                string environmentBundlePaths = addonsPath + "/Environment";
                string bundleName = "";
                string bundlePath = "";
                environmentObject.environmentName = addon.addonName;

                // Panel
                bundleName = addon.addonFileNames[0];
                bundlePath = Path.Combine(environmentBundlePaths, bundleName);
                AssetBundle panelBundle = AssetBundle.LoadFromFile(bundlePath);
                string panelName = panelBundle.GetAllAssetNames()[0];
                environmentObject.panel = panelBundle.LoadAsset<GameObject>(panelName);
                if(panelBundle != null)
                {
                    panelBundle.Unload(false);
                }
                environmentObjects.Add(environmentObject);
            }
        }
    }

    public void LoadEnvironmentScene()
    {
        foreach (Addon addon in allAddonsData)
        {
            if (!addon.enabled)
            {
                continue;
            }

            if (addon.addonType == "Environment" && addon.addonName == currentEnvironmentObject.environmentName)
            {
                string environmentBundlePaths = addonsPath + "/Environment";
                string bundleName = "";
                string bundlePath = "";

                // Scene
                bundleName = addon.addonFileNames[1];
                bundlePath = Path.Combine(environmentBundlePaths, bundleName);
                currentEnvironmentObject.sceneBundle = AssetBundle.LoadFromFile(bundlePath);
            }
        }
    }


    #endregion

    #region Persistence

    private void SaveAddonData()
    {
        if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
        {
            // If there is no file, create a dummy with one addon of each type supported
            AddonsData addonsData = GenerateBaseAddonData();
            string json = JsonConvert.SerializeObject(addonsData);

            File.WriteAllText(filePath, json);
        }
    }

    private void LoadAddonData()
    {
        if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
        {
            return;
        }

        string json = File.ReadAllText(filePath);

        // Load all data to overwrite
        allAddonsData = JsonConvert.DeserializeObject<AddonsData>(json).addonsData.addons;
    }

    #endregion
}

#region Persistence Classes

[Serializable]
public class AddonsData
{
    public AddonData addonsData;
}

[Serializable]
public class AddonData
{
    public List<Addon> addons;
}

[Serializable]
public class Addon
{
    public int addonId;
    public string addonName;
    public string addonType;
    public string addonSize;
    public bool enabled;
    public List<string> addonFileNames;
}

[Serializable]
public class EnvironmentObject
{
    public string environmentName;
    public GameObject panel;
    public AssetBundle sceneBundle;
}

#endregion

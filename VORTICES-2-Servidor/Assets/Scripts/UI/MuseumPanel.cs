using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

enum MuseumId
{
    // Change this when order is changed or when new panels are added
    BrowsingMode = 0,
    BrowsingLocal = 1,
    FileBrowser = 2,
    BrowsingOnline = 3,
    Postload = 4

}

namespace Vortices
{
    public class MuseumPanel : SpawnPanel
    {
        #region Variables and properties

        // Museum Panel Properties
        public int browsingMode { get; set; }
        public string rootUrl { get; set; }

        // Display
        [SerializeField] List<GameObject> placementBasePrefabs;
        private GameObject placementBase;

        private void Start()
        {
            // Default configs to properties
            rootUrl = "https://www.google.com";

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
        }

        #endregion

        #region User Input

        public void AddBrowserToComponents()
        {
            uiComponents[(int)MuseumId.FileBrowser] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
            FileBrowser fileBrowser = uiComponents[(int)MuseumId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(false);
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Browsing mode has to be selected
                case (int)MuseumId.BrowsingMode:
                    Toggle localToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Local Toggle").GetComponentInChildren<Toggle>();
                    Toggle onlineToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Online Toggle").GetComponentInChildren<Toggle>();
                    if (!localToggle.interactable || !onlineToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Local mode has to have a correct set to load
                case (int)MuseumId.BrowsingLocal:
                    if (optionFilePath.filePaths != null && optionFilePath.filePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    else if (optionFilePath.filePaths != null && optionFilePath.filePaths.Count == 0)
                    {
                        if (!alertCoroutineRunning)
                        {
                            StartCoroutine(SetAlert("File path chosen has no compatible extension files"));
                        }
                    }
                    break;
                // Online mode has to have a correct set to load
                case (int)MuseumId.BrowsingOnline:
                    if (optionOnlinePath.onlinePaths != null && optionOnlinePath.onlinePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    else if (optionOnlinePath.onlinePaths != null && optionOnlinePath.onlinePaths.Count == 0)
                    {
                        if (!alertCoroutineRunning)
                        {
                            StartCoroutine(SetAlert("File path chosen has no compatible extension files"));
                        }
                    }
                    break;
                // Category Controller unlocks button by its own
            }

            // Insert here panels that dont need block function
            if (componentId != (int)MuseumId.FileBrowser && 
                componentId != (int)MuseumId.Postload)
            {
                Button nextButton = uiComponents[componentId].transform.Find("Footer").transform.GetComponentInChildren<Button>();
                if (hasToBlock)
                {
                    nextButton.interactable = false;
                }
                else
                {
                    nextButton.interactable = true;
                }
            }
        }

        // Changes component based on settings fork browsingMode
        public void ChangeComponentBrowserMode()
        {
            // Browsing mode fork
            if (browsingMode == 0)
            {
                ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
            }
            else if (browsingMode == 1)
            {
                ChangeVisibleComponent((int)MuseumId.BrowsingOnline);
            }
        }

        // Changes component and starts the base operation
        public void ChangeComponentBase()
        {
            ChangeVisibleComponent((int)MuseumId.Postload);
        }
        public void RemoveBrowserFromComponents()
        {
            FileBrowser fileBrowser = uiComponents[(int)CircularId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(true);
        }

        // Uses SimpleFileBrowser to obtain a list of paths and apply them to the property filePaths so other components can use them
        public void OpenFileBrowserLocal()
        {
            FileBrowser.ShowLoadDialog((paths) =>
                {
                    optionFilePath.ClearPaths();
                    optionFilePath.GetFilePaths(paths);
                    optionFilePath.SetUIText();
                    ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
                },
                () => {/* Handle closing*/
                    ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
                },
                FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

        }

        // Uses SimpleFileBrowser to obtain a list of url txt paths, extracts urls and applies them to the property onlinePaths so other components can use them
        public void OpenFileBrowserOnline()
        {
            FileBrowser.ShowLoadDialog((paths) =>
            {
                optionOnlinePath.ClearPaths();
                optionOnlinePath.GetFilePaths(paths);
                optionOnlinePath.SetUIText();
                ChangeVisibleComponent((int)CircularId.BrowsingOnline);
            },
                () => {/* Handle closing*/
                    ChangeVisibleComponent((int)CircularId.BrowsingOnline);
                },
                FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

        }

        public void SendDataToSessionManager()
        {
            // Sends Museum Data setting variables
            // Browsing Mode
            if (browsingMode == 0)
            {
                sessionManager.browsingMode = "Local";
                sessionManager.elementPaths = optionFilePath.filePaths;
            }
            else
            {
                sessionManager.browsingMode = "Online";
                sessionManager.elementPaths = optionOnlinePath.onlinePaths;
            }
            sessionManager.displayMode = "Museum";

            sessionManager.LaunchSession();
        }

        #endregion

    }
}

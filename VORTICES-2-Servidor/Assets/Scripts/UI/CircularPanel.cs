using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

enum CircularId
{
    // Change this when order is changed or when new panels are added
    BrowsingMode = 0,
    BrowsingLocal = 1,
    FileBrowser = 2,
    BrowsingOnline = 3,
    DisplayMode = 4,
    DistributionPlane1 = 5,
    DistributionPlane2 = 6,
    DistributionPlane3 = 7,
    DistributionRadial1 = 8,
    DistributionRadial2 = 9,
    DistributionRadial3 = 10,
    Postload = 11

}

namespace Vortices
{
    public class CircularPanel : SpawnPanel
    {
        #region Variables and properties
        // Circular Panel Properties
        public int displayMode { get; set; }
        public int browsingMode { get; set; }
        public bool volumetric { get; set; }
        public Vector3Int dimension;

        private void OnEnable()
        {
            // Default configs to properties

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
        }

        #endregion

        #region User Input

        public void AddBrowserToComponents()
        {
            uiComponents[(int)CircularId.FileBrowser] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
            FileBrowser fileBrowser = uiComponents[(int)CircularId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(false);
        }

        public void RemoveBrowserFromComponents()
        { 
            FileBrowser fileBrowser = uiComponents[(int)CircularId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(true);
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Browsing mode has to be selected
                case (int)CircularId.BrowsingMode:
                    Toggle localToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Local Toggle").GetComponentInChildren<Toggle>();
                    Toggle onlineToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Online Toggle").GetComponentInChildren<Toggle>();
                    if (!localToggle.interactable || !onlineToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Local mode has to have a correct set to load
                case (int)CircularId.BrowsingLocal:
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
                case (int)CircularId.BrowsingOnline:
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
                // Display mode has to be selected
                case (int)CircularId.DisplayMode:
                    Toggle planeToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Plane Toggle").GetComponentInChildren<Toggle>();
                    Toggle radialToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Radial Toggle").GetComponentInChildren<Toggle>();
                    if (!planeToggle.interactable || !radialToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Height, Width and layers have to be bigger than 0...
                case (int)CircularId.DistributionPlane1:
                case (int)CircularId.DistributionPlane2:
                case (int)CircularId.DistributionPlane3:
                case (int)CircularId.DistributionRadial2:
                case (int)CircularId.DistributionRadial3:
                    string input = uiComponents[componentId].GetComponentInChildren<TMP_InputField>().text;

                    // Check if Input is only a number otherwise block the button
                    if (input == "")
                    {
                        break;
                    }

                    foreach (char c in input)
                    {

                        if (c < '0' || c > '9')
                        {
                            if (!alertCoroutineRunning)
                            {
                                StartCoroutine(SetAlert("Value must be a number"));
                            }
                            break;
                        }
                    }

                    int value = 0;
                    try
                    {
                        value = int.Parse(input);
                    }
                    catch { }
                    if (value > 0)
                    {
                        hasToBlock = false;
                    }
                    else
                    {
                        if (!alertCoroutineRunning)
                        {
                            StartCoroutine(SetAlert("Value has to be higher than 0"));
                        }
                    }
                    break;
                // ...Except DistributionRadial1 which needs at least 3 elements in each ring
                case (int)CircularId.DistributionRadial1:
                    string radial1input = uiComponents[componentId].GetComponentInChildren<TMP_InputField>().text;

                    if (radial1input == "")
                    {
                        break;
                    }

                    // Check if Input is only a number otherwise block the button
                    foreach (char c in radial1input)
                    {

                        if (c < '0' || c > '9')
                        {
                            if (!alertCoroutineRunning)
                            {
                                StartCoroutine(SetAlert("Value must be a number"));
                            }
                            break;
                        }
                    }

                    int radial1value = 0;
                    try
                    {
                        radial1value = int.Parse(radial1input);
                    }
                    catch { }
                    if (radial1value > 2)
                    {
                        hasToBlock = false;
                    }
                    else
                    {
                        if (!alertCoroutineRunning)
                        {
                            StartCoroutine(SetAlert("Envolving, every ring has to have at least 3 elements"));
                        }
                    }
                    break;
            }

            // Insert here panels that dont need block function
            if (componentId != (int)CircularId.FileBrowser && 
                componentId != (int)CircularId.Postload)
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
                ChangeVisibleComponent((int)CircularId.BrowsingLocal);
            }
            else if (browsingMode == 1)
            {
                ChangeVisibleComponent((int)CircularId.BrowsingOnline);
            }
        }
        // Changes component based on settings fork displayMode
        public void ChangeComponentDisplayMode(int option)
        {
            // Plane mode
            if(displayMode == 0)
            {
                switch (option)
                {
                    // Plane 1
                    case 0:
                        ChangeVisibleComponent((int)CircularId.DistributionPlane1);
                        break;
                    // Plane 2
                    case 1:
                        ChangeVisibleComponent((int)CircularId.DistributionPlane2);
                        break;
                    // Plane 3
                    case 2:
                        if (volumetric)
                        {
                            ChangeVisibleComponent((int)CircularId.DistributionPlane3);
                        }
                        else
                        {
                            ChangeVisibleComponent((int)CircularId.Postload);
                        }
                        break;
                    case 3:
                        ChangeVisibleComponent((int)CircularId.Postload);
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (displayMode == 1)
            {
                switch (option)
                {
                    // Radial 1
                    case 0:
                        ChangeVisibleComponent((int)CircularId.DistributionRadial1);
                        break;
                    // Radial 2
                    case 1:
                        ChangeVisibleComponent((int)CircularId.DistributionRadial2);
                        break;
                    // Radial 3
                    case 2:
                        if(volumetric)
                        {
                            ChangeVisibleComponent((int)CircularId.DistributionRadial3);
                        }
                        else
                        {
                            ChangeVisibleComponent((int)CircularId.Postload);
                        }
                        break;
                    case 3:
                        ChangeVisibleComponent((int)CircularId.Postload);
                        break;
                    default:
                        break;
                }
            }
        }

        // Toggles volumetric bool
        public void VolumetricToggle()
        {
            if(volumetric)
            {
                volumetric = false;
            }
            else
            {
                volumetric = true;
            }
        }

        // Sets vector dimensions for multimedia display (Representing X as quantity or width, Y as height and Z as layers)
        public void SetDimension(int option)
        {
            // Plane mode
            if (displayMode == 0)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField planeXInput = uiComponents[(int)CircularId.DistributionPlane1].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(planeXInput.text);
                        break;
                    case 1:
                        TMP_InputField planeYInput = uiComponents[(int)CircularId.DistributionPlane2].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(planeYInput.text);
                        break;
                    case 2:
                        TMP_InputField planeZInput = uiComponents[(int)CircularId.DistributionPlane3].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(planeZInput.text);
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (displayMode == 1)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField radialXInput = uiComponents[(int)CircularId.DistributionRadial1].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(radialXInput.text);
                        break;
                    case 1:
                        TMP_InputField radialYInput = uiComponents[(int)CircularId.DistributionRadial2].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(radialYInput.text);
                        break;
                    case 2:
                        TMP_InputField radialZInput = uiComponents[(int)CircularId.DistributionRadial3].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(radialZInput.text);
                        break;
                    default:
                        break;
                }
            }
        }

        // Uses SimpleFileBrowser to obtain a list of paths and applies them to the property filePaths so other components can use them
        public void OpenFileBrowserLocal()
        {
            FileBrowser.ShowLoadDialog((paths) =>
                {
                    optionFilePath.ClearPaths();
                    optionFilePath.GetFilePaths(paths);
                    optionFilePath.SetUIText();
                    ChangeVisibleComponent((int)CircularId.BrowsingLocal);
                },
                () => {/* Handle closing*/
                    ChangeVisibleComponent((int)CircularId.BrowsingLocal);
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
            // Sends Circular Data setting variables
            // Display Mode
            if (displayMode == 0)
            {
                sessionManager.displayMode = "Plane";
            }
            else
            {
                sessionManager.displayMode = "Radial";
            }
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
            // Dimension (If needed)
            if (volumetric == false || dimension.z == 0)
            {
                dimension.z = 1;
            }
            sessionManager.dimension = dimension;
            // Volumetric (if needed)
            sessionManager.volumetric = volumetric;

            sessionManager.LaunchSession();
        }

        #endregion




    }
}

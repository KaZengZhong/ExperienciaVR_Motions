using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace Vortices
{
    public class OnlinePathController : MonoBehaviour
    {
        #region Variables and properties
        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI selectionText;

        [SerializeField] private List<TextMeshProUGUI> dataCounters;

        // Filter
        private List<string> onlinePathsRaw;
        private List<string> supportedExtensions;
        private List<string> onlinePathsFiltered;

        // Data
        public List<string> onlinePaths { get; private set; }
        public int numberOfUrlFiles { get; private set; }
        public int numberOfSupported { get; private set; }
        public int numberOfUnsupported { get; private set; }
        public int numberOfFolders { get; private set; }
        public int numberOfUrlLoaded { get; private set; }
        #endregion

        private void Start()
        {
            supportedExtensions = new List<string>();
            //Add values to filter
            AddSupportedExtensions();
        }

        #region Path extraction
        // Filters paths obtained from SimpleFileBrowser turning text files into urls
        public void GetFilePaths(string[] originPaths)
        {
            // Get files from each folder
            foreach (string path in originPaths)
            {
                if (Directory.Exists(path))
                {
                    numberOfFolders++;
                    onlinePathsRaw.AddRange(Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories));
                }
                else
                {
                    onlinePathsRaw.Add(path);
                }

            }

            // Filter by extension
            foreach (string path in onlinePathsRaw)
            {
                if (CheckExtensionSupport(path))
                {
                    onlinePathsFiltered.Add(path);
                    numberOfUrlFiles++;
                }
            }

            // After getting all .txt files, expand them into urls
            foreach (string path in onlinePathsFiltered)
            {
                StreamReader inp_stm = new StreamReader(path);

                while (!inp_stm.EndOfStream)
                {
                    string inp_ln = inp_stm.ReadLine();
                    onlinePaths.Add(inp_ln);
                    numberOfUrlLoaded++;
                }

                inp_stm.Close();
            }

        }

        // Updates the UI texts after getting the paths
        public void SetUIText()
        {
            // Selection Text
            placeholderText.gameObject.SetActive(false);
            selectionText.gameObject.SetActive(true);

            string selection;
            if (onlinePaths.Count > 1 || onlinePaths.Count == 0)
            {
                selection = onlinePaths.Count + " urls extracted from ";
            }
            else
            {
                selection = onlinePaths.Count + " url extracted from ";
            }

            if (numberOfUrlFiles > 1 || numberOfUrlFiles == 0)
            {
                selection += numberOfUrlFiles + " files in ";
            }
            else
            {
                selection += numberOfUrlFiles + " file in ";
            }

            if (numberOfFolders > 1)
            {
                selection += numberOfFolders + " folders.";
            }
            else
            {
                selection += "1 folder.";
            }

            selectionText.text = selection;

            // Counters
            // Supported
            dataCounters[0].text = "" + numberOfSupported;
            if (numberOfSupported > 0)
            {
                dataCounters[0].color = Color.green;
            }
            // Unsupported
            dataCounters[1].text = "" + numberOfUnsupported;
            if (numberOfUnsupported > 0)
            {
                dataCounters[1].color = Color.red;
            }
        }

        // Clears the information everytime the list of folders or files is changed
        public void ClearPaths()
        {
            onlinePathsRaw = new List<string>();
            onlinePaths = new List<string>();
            onlinePathsFiltered = new List<string>();
            numberOfFolders = 0;
            numberOfSupported = 0;
            numberOfUnsupported = 0;
            numberOfUrlFiles = 0;
            numberOfUrlLoaded = 0;
        }

        #endregion

        #region Path filter
        private bool CheckExtensionSupport(string path)
        {
            bool addToFilePaths = false;
            string pathExtension = Path.GetExtension(path);
            pathExtension = pathExtension.ToLower();
            //Check if its supported
            if (supportedExtensions.Contains(pathExtension))
            {
                numberOfSupported++;
                addToFilePaths = true;
            }
            //Else its unsupported
            else
            {
                numberOfUnsupported++;
            }
            return addToFilePaths;
        }

        private void AddSupportedExtensions()
        {
            // Add here supported extensions
            supportedExtensions.Add(".txt");
        }
        #endregion
    }
}

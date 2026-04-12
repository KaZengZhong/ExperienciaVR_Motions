using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace Vortices
{
    public class FilePathController : MonoBehaviour
    {
        #region Variables and properties
        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI selectionText;

        [SerializeField] private List<TextMeshProUGUI> dataCounters;

        // Filter
        private bool hasH264Codec = true;
        private List<string> supportedExtensions;
        private List<string> filePathsRaw;

        // Data
        public List<string> filePaths { get; private set; }
        public int numberOfFolders { get; private set; }
        public int numberOfSupported { get; private set; }
        public int numberOfUnsupported { get; private set; }
        #endregion

        private void Start()
        {
            supportedExtensions = new List<string>();
            //Add values to filter
            AddSupportedExtensions();
        }

        #region Path extraction
        // Filters paths obtained from SimpleFileBrowser turning folders into files
        public void GetFilePaths(string[] originPaths)
        {
            // Get files from each folder
            foreach (string path in originPaths)
            {
                if (Directory.Exists(path))
                {
                    numberOfFolders++;
                    filePathsRaw.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                }
                else
                {
                    filePathsRaw.Add(path);
                }

            }
            // Filter by extension
            foreach (string path in filePathsRaw)
            {
                if (CheckExtensionSupport(path))
                {
                    filePaths.Add(path);
                }
            }
        }

        // Updates the UI texts after getting the paths
        public void SetUIText()
        {
            // Selection Text
            placeholderText.gameObject.SetActive(false);
            selectionText.gameObject.SetActive(true);

            string selection;
            if (filePaths.Count > 1 || filePaths.Count == 0)
            {
                selection = filePaths.Count + LocalizationController.instance.FetchString("baseStrings", "fileSelectMultiple");
            }
            else
            {
                selection = filePaths.Count + LocalizationController.instance.FetchString("baseStrings", "fileSelectSingle");
            }

            if (numberOfFolders > 1)
            {
                selection += numberOfFolders + LocalizationController.instance.FetchString("baseStrings", "fileFolderMultiple");
            }
            else
            {
                selection += LocalizationController.instance.FetchString("baseStrings", "fileFolderSingle");
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
            filePathsRaw = new List<string>();
            filePaths = new List<string>();
            numberOfFolders = 0;
            numberOfSupported = 0;
            numberOfUnsupported = 0;
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
            supportedExtensions.Add(".mp3");
            supportedExtensions.Add(".ogv");
            supportedExtensions.Add(".ogg");
            supportedExtensions.Add(".oga");
            supportedExtensions.Add(".webm");
            supportedExtensions.Add(".wav");
            supportedExtensions.Add(".txt");
            supportedExtensions.Add(".pdf");
            supportedExtensions.Add(".bmp");
            supportedExtensions.Add(".gif");
            supportedExtensions.Add(".jpg");
            supportedExtensions.Add(".jpeg");
            supportedExtensions.Add(".png");
            supportedExtensions.Add(".webp");
            supportedExtensions.Add(".ico");
            supportedExtensions.Add(".webp");
            supportedExtensions.Add(".json");

            if (hasH264Codec)
            {
                supportedExtensions.Add(".3gp");
                supportedExtensions.Add(".mp4");
                supportedExtensions.Add(".m4a");
                supportedExtensions.Add(".m4v");
            }

        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Collections.Specialized.BitVector32;


namespace Vortices
{
    public class LoggingController : MonoBehaviour
    {
        // Settings
        private string filePath;

        // Auxiliary References
        [SerializeField] private SessionManager sessionManager;

        public void Initialize()
        {
            InitializeLog();
        }

        #region Data Operations

        // A function for every logging type
        public void LogSessionStatus(string status)
        {
            // Check if input is valid
            // Status check
            if(!(status == "Start") && !(status == "Stop"))
            {
                Debug.Log("Status input is not valid");
                return;
            }

            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "Session Status";
            newEntry.detail = status + ";";
            // Starting details
            if (status == "Start")
            {
                newEntry.detail += "Environment: " + sessionManager.environmentName + ";" +
                                   "Display Mode: " + sessionManager.displayMode + ";";
                if (sessionManager.displayMode != "Museum")
                {
                    newEntry.detail += "Dimension: " + sessionManager.dimension.x + "," + sessionManager.dimension.y + "," + sessionManager.dimension.z + ";";
                }
                newEntry.detail += "Browsing Mode: " + sessionManager.browsingMode + ";";
            }

            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }

        public void LogMovement(string movementDir)
        {
            // Check if input is valid
            // movementDir check
            if (!(movementDir == "Up") && 
                !(movementDir == "Down") &&
                !(movementDir == "Left") &&
                !(movementDir == "Right") &&
                !(movementDir == "Pull") &&
                !(movementDir == "Push"))
            {
                Debug.Log("Movement direction input is not valid");
                return;
            }
            // controller

            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "Element Movement";
            newEntry.detail = movementDir + ";";
            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }

        public IEnumerator LogTeleportation()
        {
            yield return new WaitForSeconds(0.5f);
            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "User Movement";
            Vector3 newPosition = GameObject.Find("XR Origin").transform.position;
            newEntry.detail = "New position: " + "(" + newPosition.x + "," + newPosition.y + "," + newPosition.z + ")" + ";";
            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }

        public void LogSelection(string selectedUrl, bool wasSelected)
        {
            // Check if input is valid
            // No checks

            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "Selection";

            if (wasSelected)
            {
                newEntry.detail = "Selected" + ";";
            }
            else
            {
                newEntry.detail = "Unselected" + ";";
            }

            newEntry.detail += "Url: " + selectedUrl;

            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }

        public void LogCategory(string categorizedUrl, bool categoryAdded, string categoryName)
        {
            // Check if input is valid
            // No checks

            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "Category";

            if (categoryAdded)
            {
                newEntry.detail = "Added" + ";";
            }
            else
            {
                newEntry.detail = "Removed" + ";";
            }
            newEntry.detail += "Url: " + categorizedUrl + ";";
            newEntry.detail += "Name: " + categoryName;


            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }

        public void LogUrlChanged(string finalUrl)
        {
            // Check if input is valid
            // No checks

            LogEntry newEntry = new LogEntry();
            // Initialize entry
            newEntry = LogEntryInitialize(newEntry);
            // Set type and detail
            newEntry.type = "URL Change";

            newEntry.detail += "Url: " + finalUrl;

            // Entry is ready, write it to file
            WriteToLog(newEntry);
        }



        // Configures global variables of a LogEntry
        private LogEntry LogEntryInitialize(LogEntry entry)
        {
            entry.environmentName = sessionManager.environmentName;
            DateTime date = DateTime.Now;
            entry.timestamp = date.TimeOfDay.ToString();
            entry.date = date.Date.ToString("d");
            return entry;
        }

        #endregion

        #region Persistence

        private void InitializeLog()
        {
            string filename = Path.Combine(Application.dataPath + "/Results");
            // File path depends on session name and user Id
            filename = Path.Combine(filename, sessionManager.sessionName);
            filename = Path.Combine(filename, sessionManager.userId.ToString());

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            string baseFilename = Path.Combine(filename, "Session Log Output");

            filename = Path.Combine(filename, "Session Log Output.csv");

            // If by any case the same id is entered, create another file so the previous data is not lost
            if (File.Exists(filename))
            {
                int copy = 0;
                while (File.Exists(filename))
                {
                    filename = baseFilename + " (" + copy + ")" + ".csv";
                    copy++;
                }
            }

            filePath = filename;

            // Write headers
            TextWriter tw = new StreamWriter(filePath, false);

            tw.WriteLine("Timestamp;Date;Type;Detail");
            tw.Close();
        }

        private void WriteToLog(LogEntry entry)
        {
            if(File.Exists(filePath))
            {
                TextWriter tw = new StreamWriter(filePath, true);

                // Write all parts of the entry (If detail uses more than one cell, it will come separated by ;
                tw.WriteLine(entry.timestamp + ";" +
                             entry.date + ";" +
                             entry.type + ";" +
                             entry.detail + ";");

                tw.Close();
            }
            else
            {
                Debug.Log("Logger was not initialized correctly");
            }
        }

        #endregion


    }

    public class LogEntry
    {
        public string environmentName;
        public string timestamp;
        public string date;
        public string type;
        public string detail;
    }
}

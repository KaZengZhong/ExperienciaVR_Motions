using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class JsonChecker : MonoBehaviour
{

    #region Data Operations
    public static bool IsJsonEmpty(string filePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            jsonContent = jsonContent.Trim(); // Remove leading and trailing whitespace

            // Check if the JSON content is empty (only curly braces)
            return jsonContent == "{}";
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading JSON file: " + e.Message);
            return false; // Return false in case of errors
        }
    }

    #endregion
}

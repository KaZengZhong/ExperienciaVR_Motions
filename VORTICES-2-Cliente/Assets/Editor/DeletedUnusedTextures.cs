using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DeleteUnusedTextures
{
    private static string texturesPath = "Assets/Thirdparty/Blue Dot Studios/Art Gallery/Textures/"; // Carpeta donde buscar
    private static string backupPath = "Assets/UnusedTexturesBackup/"; // Carpeta de respaldo

    [MenuItem("Tools/Find and Move Unused Textures")]
    public static void FindAndMoveUnusedTextures()
    {
        if (!Directory.Exists(texturesPath))
        {
            Debug.LogError("¡La carpeta de texturas no existe! Verifica la ruta en el script.");
            return;
        }

        if (!Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath); // Crea la carpeta de respaldo si no existe
        }

        string[] allTextures = Directory.GetFiles(texturesPath, "*.*", SearchOption.AllDirectories);
        List<string> usedTextures = new List<string>(AssetDatabase.GetDependencies("Assets")); // Obtiene todos los assets usados en el proyecto

        int movedCount = 0;

        foreach (string texturePath in allTextures)
        {
            string relativePath = texturePath.Replace("\\", "/"); // Ajusta la ruta para Unity
            if (!usedTextures.Contains(relativePath)) // Si la textura no está en uso
            {
                string newFilePath = backupPath + Path.GetFileName(relativePath);
                File.Move(relativePath, newFilePath);
                movedCount++;
                Debug.Log("Movida: " + relativePath + " → " + newFilePath);
            }
        }

        AssetDatabase.Refresh(); // Refresca Unity para actualizar la jerarquía de archivos

        Debug.Log($"Proceso terminado. {movedCount} texturas movidas a la carpeta de respaldo.");
    }
}

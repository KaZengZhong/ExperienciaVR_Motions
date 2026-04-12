using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FindUsedTexturesInScenes
{
    [MenuItem("Tools/Find Used Textures in All Scenes")]
    static void FindUsedTextures()
    {
        string[] allScenes = AssetDatabase.FindAssets("t:Scene"); // Encuentra todas las escenas
        HashSet<string> usedTextures = new HashSet<string>();

        foreach (string sceneGUID in allScenes)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            string[] dependencies = AssetDatabase.GetDependencies(scenePath); // Encuentra todos los assets usados en la escena

            foreach (string dependency in dependencies)
            {
                if (dependency.EndsWith(".png") || dependency.EndsWith(".jpg") || dependency.EndsWith(".tga") || dependency.EndsWith(".psd"))
                {
                    usedTextures.Add(dependency);
                }
            }
        }

        Debug.Log("Used Textures in all Scenes:\n" + string.Join("\n", usedTextures));
    }
}

using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildAssetBundles
{
    [MenuItem("Tools/Build Environment AssetBundles")]
    public static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Addons/Environment";

        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        Debug.Log(" AssetBundles generados y guardados en: " + assetBundleDirectory);
    }
}

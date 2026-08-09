using System.IO;
using UnityEngine;

public static class PlatformPaths
{
    public static string ConfigJson
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Application.persistentDataPath + "/config.json";
#else
            return Path.GetDirectoryName(Application.dataPath) + "/config.json";
#endif
        }
    }

    public static string LauncherSessions =>
        Application.persistentDataPath + "/launcher_sessions.json";
}

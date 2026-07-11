using System.IO;
using UnityEngine;

public static class PlatformPaths
{
    public static string SessionJson
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Application.persistentDataPath + "/session.json";
#else
            return Path.GetDirectoryName(Application.dataPath) + "/session.json";
#endif
        }
    }

    public static string ParametersJson
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Application.persistentDataPath + "/parameters.json";
#else
            return Path.Combine(Application.dataPath, "../parameters.json");
#endif
        }
    }

    public static string LauncherSessions =>
        Application.persistentDataPath + "/launcher_sessions.json";
}

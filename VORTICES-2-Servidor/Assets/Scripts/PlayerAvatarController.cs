using System.IO;
using Mirror;
using UnityEngine;

public class PlayerAvatarController : NetworkBehaviour
{
    [Header("Avatares (uno por userId, cíclico)")]
    [SerializeField] private GameObject[] avatarPrefabs;

    [SyncVar(hook = nameof(OnAvatarIndexChanged))]
    private int avatarIndex = -1;

    private GameObject currentAvatar;

    void Awake()
    {
        // Destruir todos los hijos existentes del prefab al iniciar
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[Avatar] OnStartLocalPlayer — prefabs={avatarPrefabs?.Length}");
        if (avatarPrefabs == null || avatarPrefabs.Length == 0) return;
        int userId = ReadUserId();
        Debug.Log($"[Avatar] userId={userId} → index={userId % avatarPrefabs.Length}");
        CmdSetAvatar(userId % avatarPrefabs.Length);
    }

    public override void OnStartClient()
    {
        Debug.Log($"[Avatar] OnStartClient — avatarIndex={avatarIndex}, isLocalPlayer={isLocalPlayer}");
        if (avatarIndex >= 0)
            SpawnAvatar(avatarIndex);
    }

    [Command]
    private void CmdSetAvatar(int index)
    {
        Debug.Log($"[Avatar] CmdSetAvatar({index}) en servidor");
        avatarIndex = index;
    }

    private void OnAvatarIndexChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"[Avatar] OnAvatarIndexChanged {oldIndex}→{newIndex}");
        if (newIndex >= 0)
            SpawnAvatar(newIndex);
    }

    private void SpawnAvatar(int index)
    {
        if (avatarPrefabs == null || avatarPrefabs.Length == 0) return;
        Debug.Log($"[Avatar] SpawnAvatar({index}), isLocalPlayer={isLocalPlayer}, netId={netId}");

        if (currentAvatar != null)
            Destroy(currentAvatar);

        currentAvatar = Instantiate(avatarPrefabs[index % avatarPrefabs.Length], transform);
        currentAvatar.transform.localPosition = Vector3.zero;
        currentAvatar.transform.localRotation = Quaternion.identity;

        if (isLocalPlayer)
        {
            foreach (Renderer r in currentAvatar.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }
    }

    private int ReadUserId()
    {
        string path = Path.GetDirectoryName(Application.dataPath) + "/session.json";
        try
        {
            var data = JsonUtility.FromJson<SessionData>(File.ReadAllText(path));
            return Mathf.Abs(data.userId);
        }
        catch { return 0; }
    }

    [System.Serializable]
    private class SessionData { public int userId = 0; }
}

using System.IO;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vortices;

public class PlayerAvatarController : NetworkBehaviour
{
    [Header("Avatares (uno por userId, cíclico)")]
    [SerializeField] private GameObject[] avatarPrefabs;
    [SerializeField] private float avatarGroundY = 0f;

    [SyncVar(hook = nameof(OnAvatarIndexChanged))]
    private int avatarIndex = -1;

    [SyncVar(hook = nameof(OnSpeakingChanged))]
    private bool _isSpeaking;

    private GameObject currentAvatar;
    private GameObject _speakingIndicator;

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
            StartCoroutine(SpawnNextFrame(avatarIndex));
    }

    public void SetSpeaking(bool speaking)
    {
        if (!isServer) return;
        _isSpeaking = speaking;
    }

    private void OnSpeakingChanged(bool _, bool newVal)
    {
        if (_speakingIndicator != null)
            _speakingIndicator.SetActive(newVal && !isLocalPlayer);
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
            StartCoroutine(SpawnNextFrame(newIndex));
    }

    private System.Collections.IEnumerator SpawnNextFrame(int index)
    {
        yield return null;
        SpawnAvatar(index);
    }

    private void SpawnAvatar(int index)
    {
        if (avatarPrefabs == null || avatarPrefabs.Length == 0) { Debug.LogWarning("[Avatar] avatarPrefabs vacío"); return; }
        Debug.Log($"[Avatar] SpawnAvatar({index}), isLocalPlayer={isLocalPlayer}, netId={netId}");

        if (currentAvatar != null)
            Destroy(currentAvatar);

        currentAvatar = Instantiate(avatarPrefabs[index % avatarPrefabs.Length], transform);
        currentAvatar.transform.localPosition = Vector3.zero;
        currentAvatar.transform.localRotation = Quaternion.identity;

        // El jugador local no ve su propio cuerpo
        if (isLocalPlayer)
        {
            foreach (Renderer r in currentAvatar.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        _speakingIndicator = BuildSpeakingIndicator();
        _speakingIndicator.SetActive(_isSpeaking && !isLocalPlayer);
    }

    private GameObject BuildSpeakingIndicator()
    {
        var root = new GameObject("SpeakingIndicator", typeof(RectTransform));
        root.transform.SetParent(currentAvatar.transform, false);
        root.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        root.transform.localScale = Vector3.one * 0.005f;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        root.AddComponent<CanvasScaler>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 25f);

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(root.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.1f, 0.75f, 0.1f, 0.88f);

        var txtGO = new GameObject("Txt", typeof(RectTransform));
        txtGO.transform.SetParent(bg.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Hablando";
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        root.SetActive(false);
        return root;
    }

    private void Update()
    {
        if (currentAvatar == null) return;

        Vector3 p = currentAvatar.transform.position;
        if (p.y != avatarGroundY)
            currentAvatar.transform.position = new Vector3(p.x, avatarGroundY, p.z);

        if (_speakingIndicator != null && _speakingIndicator.activeSelf)
        {
            Camera cam = Camera.main;
            if (cam != null)
                _speakingIndicator.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        }
    }

    private int ReadUserId()
    {
        try
        {
            var data = JsonUtility.FromJson<SessionData>(File.ReadAllText(PlatformPaths.ConfigJson));
            return Mathf.Abs(data.userId);
        }
        catch { return 0; }
    }

    [System.Serializable]
    private class SessionData { public int userId = 0; }
}

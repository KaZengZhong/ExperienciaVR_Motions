using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Vortices
{
    /// <summary>
    /// Controla la pantalla de configuración in-app para Quest standalone.
    /// Escribe session.json y parameters.json en Application.persistentDataPath
    /// antes de cargar la escena MazeEnvironment.
    /// </summary>
    public class QuestSetupManager : MonoBehaviour
    {
        // ── Sesión ─────────────────────────────────────────────────────────────
        [Header("Sesión")]
        [SerializeField] private TMP_Text sessionLabel;
        [SerializeField] private Button prevSessionBtn;
        [SerializeField] private Button nextSessionBtn;
        [SerializeField] private Button addSessionBtn;
        [SerializeField] private Button deleteSessionBtn;

        // ── Usuario ────────────────────────────────────────────────────────────
        [Header("Usuario")]
        [SerializeField] private TMP_Text userIdLabel;
        [SerializeField] private Button userIdMinusBtn;
        [SerializeField] private Button userIdPlusBtn;

        // ── Red ────────────────────────────────────────────────────────────────
        [Header("Red")]
        [SerializeField] private Toggle onlineToggle;
        [SerializeField] private GameObject ipRow;
        [SerializeField] private TMP_Text ipLabel;
        [SerializeField] private Button editIpBtn;

        // ── Skin ───────────────────────────────────────────────────────────────
        [Header("Skin")]
        [SerializeField] private TMP_Text skinLabel;
        [SerializeField] private Button prevSkinBtn;
        [SerializeField] private Button nextSkinBtn;

        // ── No ceiling ─────────────────────────────────────────────────────────
        [Header("No Ceiling")]
        [SerializeField] private Toggle noCeilingToggle;

        // ── Grilla ─────────────────────────────────────────────────────────────
        [Header("Grilla")]
        [SerializeField] private TMP_Text gridWidthLabel;
        [SerializeField] private Button gridWidthMinusBtn;
        [SerializeField] private Button gridWidthPlusBtn;

        [SerializeField] private TMP_Text gridHeightLabel;
        [SerializeField] private Button gridHeightMinusBtn;
        [SerializeField] private Button gridHeightPlusBtn;

        // ── Max Tótems ─────────────────────────────────────────────────────────
        [Header("Tótems")]
        [SerializeField] private TMP_Text maxTotemsLabel;
        [SerializeField] private Button maxTotemsMinusBtn;
        [SerializeField] private Button maxTotemsPlusBtn;

        // ── Teclado propio con botones Unity ──────────────────────────────────
        [Header("Teclado")]
        [SerializeField] private GameObject keyboardPanel;
        [SerializeField] private TMP_Text keyboardDisplay;
        [SerializeField] private Transform keyboardButtonsGrid;
        [SerializeField] private Button keyboardBackspaceBtn;
        [SerializeField] private Button keyboardConfirmBtn;
        [SerializeField] private Button keyboardCancelBtn;

        // ── Inicio ─────────────────────────────────────────────────────────────
        [Header("Iniciar")]
        [SerializeField] private Button startBtn;
        [SerializeField] private TMP_Text alertLabel;

        // ── Constantes ─────────────────────────────────────────────────────────
        private static readonly string[] SkinNames =
            { "Castillo", "Madera", "Granito", "Artistico", "Verde", "Transparente" };
        private const string MazeScene = "MazeEnvironment";

        // ── Estado interno ─────────────────────────────────────────────────────
        private List<string> sessions = new List<string>();
        private int sessionIdx = 0;
        private int userId = 0;
        private bool isOnline = false;
        private string ipAddress = "192.168.0.100";
        private int skinIdx = 0;
        private bool noCeiling = false;
        private int gridWidth = 8;
        private int gridHeight = 8;
        private int maxTotems = 0;   // 0 = sin límite

        private enum KeyboardTarget { None, SessionName, IP }
        private KeyboardTarget kbTarget = KeyboardTarget.None;
        private string kbInput = "";

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            Debug.Log("[QuestSetup] Awake. startBtn=" + (startBtn != null ? startBtn.name : "NULL"));
            LoadSavedData();
            WireButtons();
            BuildKeyboard();
        }

        private void Start()
        {
            RefreshAll();
        }

        // ── Cableado de botones ────────────────────────────────────────────────

        private void WireButtons()
        {
            prevSessionBtn?.onClick.AddListener(() => ShiftSession(-1));
            nextSessionBtn?.onClick.AddListener(() => ShiftSession(+1));
            addSessionBtn?.onClick.AddListener(OpenKeyboardForNewSession);
            deleteSessionBtn?.onClick.AddListener(DeleteCurrentSession);

            userIdMinusBtn?.onClick.AddListener(() => { userId = Mathf.Max(0, userId - 1); RefreshUserId(); });
            userIdPlusBtn?.onClick.AddListener(() => { userId++; RefreshUserId(); });

            onlineToggle?.onValueChanged.AddListener(v => { isOnline = v; RefreshOnline(); });
            editIpBtn?.onClick.AddListener(OpenKeyboardForIP);

            prevSkinBtn?.onClick.AddListener(() => ShiftSkin(-1));
            nextSkinBtn?.onClick.AddListener(() => ShiftSkin(+1));

            noCeilingToggle?.onValueChanged.AddListener(v => noCeiling = v);

            gridWidthMinusBtn?.onClick.AddListener(() => { gridWidth = Mathf.Max(3, gridWidth - 1); RefreshGrid(); });
            gridWidthPlusBtn?.onClick.AddListener(() => { gridWidth = Mathf.Min(30, gridWidth + 1); RefreshGrid(); });
            gridHeightMinusBtn?.onClick.AddListener(() => { gridHeight = Mathf.Max(3, gridHeight - 1); RefreshGrid(); });
            gridHeightPlusBtn?.onClick.AddListener(() => { gridHeight = Mathf.Min(30, gridHeight + 1); RefreshGrid(); });

            maxTotemsMinusBtn?.onClick.AddListener(() => { maxTotems = Mathf.Max(0, maxTotems - 1); RefreshTotems(); });
            maxTotemsPlusBtn?.onClick.AddListener(() => { maxTotems++; RefreshTotems(); });

            startBtn?.onClick.AddListener(OnStart);

            keyboardBackspaceBtn?.onClick.AddListener(() => {
                if (kbInput.Length > 0) kbInput = kbInput[..^1];
                if (keyboardDisplay != null) keyboardDisplay.text = kbInput;
            });
            keyboardConfirmBtn?.onClick.AddListener(ConfirmKeyboard);
            keyboardCancelBtn?.onClick.AddListener(CloseKeyboard);
        }

        // ── Teclado del sistema (Quest VR keyboard) ────────────────────────────

        // ── Generación del teclado ─────────────────────────────────────────────

        private void BuildKeyboard()
        {
            if (keyboardButtonsGrid == null) return;

            string[] keys = {
                "1","2","3","4","5","6","7","8","9","0",".",
                "Q","W","E","R","T","Y","U","I","O","P",
                "A","S","D","F","G","H","J","K","L","-",
                "Z","X","C","V","B","N","M"," "
            };

            foreach (string key in keys)
                CreateKey(key);
        }

        private void CreateKey(string key)
        {
            var go = new GameObject("Key_" + key.Trim(), typeof(RectTransform));
            go.transform.SetParent(keyboardButtonsGrid, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.8f);
            btn.colors = colors;

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var rt = txtGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = key == " " ? "SP" : key;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 28;
            txt.color = Color.white;

            string captured = key;
            btn.onClick.AddListener(() => AppendKey(captured));
        }

        private void AppendKey(string key)
        {
            kbInput += key;
            if (keyboardDisplay != null) keyboardDisplay.text = kbInput;
        }

        private void OpenKeyboardForNewSession()
        {
            kbTarget = KeyboardTarget.SessionName;
            kbInput = "";
            if (keyboardDisplay != null) keyboardDisplay.text = "";
            if (keyboardPanel != null) keyboardPanel.SetActive(true);
        }

        private void OpenKeyboardForIP()
        {
            kbTarget = KeyboardTarget.IP;
            kbInput = ipAddress;
            if (keyboardDisplay != null) keyboardDisplay.text = kbInput;
            if (keyboardPanel != null) keyboardPanel.SetActive(true);
        }

        private void ConfirmKeyboard()
        {
            string value = kbInput.Trim();
            switch (kbTarget)
            {
                case KeyboardTarget.SessionName:
                    if (value.Length > 0 && !sessions.Contains(value))
                    {
                        sessions.Add(value);
                        sessionIdx = sessions.Count - 1;
                        RefreshSession();
                    }
                    break;
                case KeyboardTarget.IP:
                    if (value.Length > 0) ipAddress = value;
                    RefreshOnline();
                    break;
            }
            CloseKeyboard();
        }

        private void CloseKeyboard()
        {
            kbTarget = KeyboardTarget.None;
            kbInput = "";
            if (keyboardPanel != null) keyboardPanel.SetActive(false);
        }

        // ── Acciones de sesión ─────────────────────────────────────────────────

        private void ShiftSession(int delta)
        {
            if (sessions.Count == 0) return;
            sessionIdx = (sessionIdx + delta + sessions.Count) % sessions.Count;
            RefreshSession();
        }

        private void DeleteCurrentSession()
        {
            if (sessions.Count == 0) return;
            sessions.RemoveAt(sessionIdx);
            sessionIdx = Mathf.Clamp(sessionIdx, 0, sessions.Count - 1);
            RefreshSession();
        }

        private void ShiftSkin(int delta)
        {
            skinIdx = (skinIdx + delta + SkinNames.Length) % SkinNames.Length;
            RefreshSkin();
        }

        // ── Inicio de la experiencia ───────────────────────────────────────────

        private void OnStart()
        {
            Debug.Log("[QuestSetup] OnStart llamado. Sesiones: " + sessions.Count);
            if (sessions.Count == 0)
            {
                SetAlert("Creá al menos una sesión.");
                return;
            }
            if (isOnline && string.IsNullOrWhiteSpace(ipAddress))
            {
                SetAlert("Ingresá la IP del servidor.");
                return;
            }

            SaveData();
            SceneManager.LoadScene(MazeScene);
        }

        private void SetAlert(string msg)
        {
            if (alertLabel != null) alertLabel.text = msg;
        }

        // ── Persistencia ───────────────────────────────────────────────────────

        private void LoadSavedData()
        {
            // Cargar lista de sesiones
            if (File.Exists(PlatformPaths.LauncherSessions))
            {
                try
                {
                    var wrap = JsonUtility.FromJson<SessionListWrapper>(
                        File.ReadAllText(PlatformPaths.LauncherSessions));
                    if (wrap?.sessions != null) sessions = wrap.sessions;
                }
                catch { }
            }

            // Cargar session.json para userId / online / ip
            if (File.Exists(PlatformPaths.SessionJson))
            {
                try
                {
                    var sd = JsonUtility.FromJson<SavedSession>(
                        File.ReadAllText(PlatformPaths.SessionJson));
                    userId    = sd.userId;
                    isOnline  = sd.isOnlineSession;
                    ipAddress = sd.ipAddress;

                    int idx = sessions.IndexOf(sd.sessionName);
                    if (idx >= 0) sessionIdx = idx;
                }
                catch { }
            }

            // Cargar parameters.json para skin / noCeiling / grilla / tótems
            if (File.Exists(PlatformPaths.ParametersJson))
            {
                try
                {
                    var p = JsonUtility.FromJson<SavedParams>(
                        File.ReadAllText(PlatformPaths.ParametersJson));
                    int sIdx = Array.IndexOf(SkinNames, p.skinName);
                    skinIdx    = sIdx >= 0 ? sIdx : 0;
                    noCeiling  = p.noCeiling;
                    gridWidth  = p.gridWidth  > 0 ? p.gridWidth  : gridWidth;
                    gridHeight = p.gridHeight > 0 ? p.gridHeight : gridHeight;
                    maxTotems  = p.maxTotems;
                }
                catch { }
            }
        }

        private void SaveData()
        {
            // launcher_sessions.json
            File.WriteAllText(PlatformPaths.LauncherSessions,
                JsonUtility.ToJson(new SessionListWrapper { sessions = sessions }));

            // session.json — lo lee MazeOnlineConnector
            string sessName = sessions.Count > 0 ? sessions[sessionIdx] : "Sesion1";
            File.WriteAllText(PlatformPaths.SessionJson, JsonUtility.ToJson(new SavedSession
            {
                sessionName     = sessName,
                userId          = userId,
                environmentName = "Maze",
                isOnlineSession = isOnline,
                ipAddress       = ipAddress
            }));

            // parameters.json — lo lee ProceduralMapGenerator
            File.WriteAllText(PlatformPaths.ParametersJson, JsonUtility.ToJson(new SavedParams
            {
                skinName   = SkinNames[skinIdx],
                noCeiling  = noCeiling,
                gridWidth  = gridWidth,
                gridHeight = gridHeight,
                maxTotems  = maxTotems
            }));
        }

        // ── Refresh de UI ──────────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshSession();
            RefreshUserId();
            RefreshOnline();
            RefreshSkin();
            if (noCeilingToggle != null) noCeilingToggle.isOn = noCeiling;
            RefreshGrid();
            RefreshTotems();
            if (alertLabel != null) alertLabel.text = "";
        }

        private void RefreshSession()
        {
            if (sessionLabel != null)
                sessionLabel.text = sessions.Count > 0 ? sessions[sessionIdx] : "— (sin sesiones) —";
        }

        private void RefreshUserId()
        {
            if (userIdLabel != null) userIdLabel.text = userId.ToString();
        }

        private void RefreshOnline()
        {
            if (onlineToggle != null) onlineToggle.SetIsOnWithoutNotify(isOnline);
            if (ipRow != null) ipRow.SetActive(isOnline);
            if (ipLabel != null) ipLabel.text = ipAddress;
        }

        private void RefreshSkin()
        {
            if (skinLabel != null) skinLabel.text = SkinNames[skinIdx];
        }

        private void RefreshGrid()
        {
            if (gridWidthLabel  != null) gridWidthLabel.text  = gridWidth.ToString();
            if (gridHeightLabel != null) gridHeightLabel.text = gridHeight.ToString();
        }

        private void RefreshTotems()
        {
            if (maxTotemsLabel != null)
                maxTotemsLabel.text = maxTotems == 0 ? "Sin límite" : maxTotems.ToString();
        }

        // ── Clases de serialización ────────────────────────────────────────────

        [Serializable] private class SessionListWrapper { public List<string> sessions; }

        [Serializable]
        private class SavedSession
        {
            public string sessionName     = "";
            public int    userId          = 0;
            public string environmentName = "Maze";
            public bool   isOnlineSession = false;
            public string ipAddress       = "";
        }

        [Serializable]
        private class SavedParams
        {
            public string skinName   = "Dungeon";
            public bool   noCeiling  = false;
            public int    gridWidth  = 8;
            public int    gridHeight = 8;
            public int    maxTotems  = 0;
        }
    }
}

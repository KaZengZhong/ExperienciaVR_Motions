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

        // ── Rotación ───────────────────────────────────────────────────────────
        [Header("Rotación")]
        [SerializeField] private TMP_Text rotationModeLabel;
        [SerializeField] private Button prevRotationBtn;
        [SerializeField] private Button nextRotationBtn;

        // ── Movimiento ─────────────────────────────────────────────────────────
        [Header("Movimiento")]
        [SerializeField] private TMP_Text movementModeLabel;
        [SerializeField] private Button prevMovementBtn;
        [SerializeField] private Button nextMovementBtn;

        // ── Grilla ─────────────────────────────────────────────────────────────
        [Header("Grilla")]
        [SerializeField] private TMP_Text gridSizeLabel;
        [SerializeField] private Button gridSizeMinusBtn;
        [SerializeField] private Button gridSizePlusBtn;

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
        private int rotationMode = 0;  // 0=Controller, 1=Head
        private int movementMode = 0;  // 0=Joystick, 1=Teleportación
        private int gridSize = 8;

        private static readonly string[] RotationModeNames = { "Joystick", "Head" };
        private static readonly string[] MovementModeNames = { "Joystick", "Teleport", "Walk" };
        private int maxTotems = -1;   // -1 = sin límite

        private enum KeyboardTarget { None, SessionName, IP }
        private KeyboardTarget kbTarget = KeyboardTarget.None;
        private string kbInput = "";

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
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

            prevRotationBtn?.onClick.AddListener(() => { rotationMode = (rotationMode - 1 + RotationModeNames.Length) % RotationModeNames.Length; RefreshRotation(); });
            nextRotationBtn?.onClick.AddListener(() => { rotationMode = (rotationMode + 1) % RotationModeNames.Length; RefreshRotation(); });

            prevMovementBtn?.onClick.AddListener(() => { movementMode = (movementMode - 1 + MovementModeNames.Length) % MovementModeNames.Length; RefreshMovement(); });
            nextMovementBtn?.onClick.AddListener(() => { movementMode = (movementMode + 1) % MovementModeNames.Length; RefreshMovement(); });

            gridSizeMinusBtn?.onClick.AddListener(() => { gridSize = Mathf.Max(3, gridSize - 1); RefreshGrid(); });
            gridSizePlusBtn?.onClick.AddListener(() => { gridSize = Mathf.Min(30, gridSize + 1); RefreshGrid(); });

            maxTotemsMinusBtn?.onClick.AddListener(() => { maxTotems = Mathf.Max(-1, maxTotems - 1); RefreshTotems(); });
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
            if (keyboardPanel != null) keyboardPanel.SetActive(false);

            // Backspace: ancho fijo; el display toma el espacio restante
            if (keyboardBackspaceBtn != null)
            {
                var le = keyboardBackspaceBtn.GetComponent<LayoutElement>()
                         ?? keyboardBackspaceBtn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 120f;
                le.flexibleWidth  = 0f;

                var hlg = keyboardBackspaceBtn.transform.parent
                                              ?.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.childForceExpandWidth = false;
            }

            // Más espacio entre la fila de display y la grilla de teclas
            var vlg = keyboardPanel?.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.spacing = 20f;

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

            PlayerPrefs.SetInt("rotationMode", rotationMode);
            PlayerPrefs.SetInt("movementMode", movementMode);
            PlayerPrefs.Save();
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

            // Cargar config.json
            if (File.Exists(PlatformPaths.ConfigJson))
            {
                try
                {
                    var cfg = JsonUtility.FromJson<SavedConfig>(
                        File.ReadAllText(PlatformPaths.ConfigJson));
                    userId    = cfg.userId;
                    isOnline  = cfg.isOnlineSession;
                    ipAddress = cfg.ipAddress;

                    int idx = sessions.IndexOf(cfg.sessionName);
                    if (idx >= 0) sessionIdx = idx;

                    int sIdx = Array.IndexOf(SkinNames, cfg.skinName);
                    skinIdx   = sIdx >= 0 ? sIdx : 0;
                    noCeiling = cfg.noCeiling;
                    gridSize  = cfg.gridWidth > 0 ? cfg.gridWidth : gridSize;
                    maxTotems = cfg.maxTotems >= -1 ? cfg.maxTotems : -1;
                    rotationMode = cfg.rotationMode;
                    movementMode = cfg.movementMode;
                }
                catch { }
            }
            else
            {
                rotationMode = PlayerPrefs.GetInt("rotationMode", 0);
                movementMode = PlayerPrefs.GetInt("movementMode", 0);
            }
        }

        private void SaveData()
        {
            // launcher_sessions.json
            File.WriteAllText(PlatformPaths.LauncherSessions,
                JsonUtility.ToJson(new SessionListWrapper { sessions = sessions }));

            // config.json — contiene todos los campos (session + params)
            string sessName = sessions.Count > 0 ? sessions[sessionIdx] : "Sesion1";
            File.WriteAllText(PlatformPaths.ConfigJson, JsonUtility.ToJson(new SavedConfig
            {
                sessionName     = sessName,
                userId          = userId,
                environmentName = "Maze",
                isOnlineSession = isOnline,
                ipAddress       = ipAddress,
                skinName        = SkinNames[skinIdx],
                noCeiling       = noCeiling,
                gridWidth       = gridSize,
                gridHeight      = gridSize,
                maxTotems       = maxTotems,
                rotationMode    = rotationMode,
                movementMode    = movementMode
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
            RefreshRotation();
            RefreshMovement();
            RefreshGrid();
            RefreshTotems();
            if (alertLabel != null) alertLabel.text = "";
        }

        private void RefreshSession()
        {
            if (sessionLabel != null)
                sessionLabel.text = sessions.Count > 0 ? sessions[sessionIdx] : "no sessions";
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

        private void RefreshRotation()
        {
            if (rotationModeLabel != null)
                rotationModeLabel.text = RotationModeNames[rotationMode];
        }

        private void RefreshMovement()
        {
            if (movementModeLabel != null)
                movementModeLabel.text = MovementModeNames[movementMode];
        }

        private void RefreshGrid()
        {
            if (gridSizeLabel != null) gridSizeLabel.text = gridSize.ToString();
        }

        private void RefreshTotems()
        {
            if (maxTotemsLabel != null)
                maxTotemsLabel.text = maxTotems == -1 ? "No limits" : maxTotems.ToString();
        }

        // ── Clases de serialización ────────────────────────────────────────────

        [Serializable] private class SessionListWrapper { public List<string> sessions; }

        [Serializable]
        private class SavedConfig
        {
            public string sessionName     = "";
            public int    userId          = 0;
            public string environmentName = "Maze";
            public bool   isOnlineSession = false;
            public string ipAddress       = "";
            public string skinName        = "Dungeon";
            public bool   noCeiling       = false;
            public int    gridWidth       = 8;
            public int    gridHeight      = 8;
            public int    maxTotems       = 0;
            public int    rotationMode    = 0;
            public int    movementMode    = 0;
        }
    }
}

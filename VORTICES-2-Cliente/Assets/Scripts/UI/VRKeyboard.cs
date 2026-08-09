using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    /// <summary>
    /// Teclado de botones Unity para VR (Quest standalone).
    /// Añadí este componente al panel del teclado dentro del canvas.
    /// Llamá Open() con un callback para recibir el texto confirmado.
    /// </summary>
    public class VRKeyboard : MonoBehaviour
    {
        [SerializeField] private TMP_Text    displayText;
        [SerializeField] private Transform   keysGrid;
        [SerializeField] private Button      backspaceBtn;
        [SerializeField] private Button      confirmBtn;
        [SerializeField] private Button      cancelBtn;

        private string _input = "";
        private Action<string> _onConfirm;
        private Action         _onCancel;

        private static readonly string[] Keys =
        {
            "1","2","3","4","5","6","7","8","9","0",".",
            "Q","W","E","R","T","Y","U","I","O","P",
            "A","S","D","F","G","H","J","K","L","-",
            "Z","X","C","V","B","N","M"," "
        };

        private bool _initialized = false;

        private void Awake()
        {
            gameObject.SetActive(false);
            if (keysGrid != null)
                Initialize();
        }

        // Llamar cuando se crean las referencias por código (sin Inspector)
        public void Init(TMP_Text display, Transform grid, Button bksp, Button confirm, Button cancel)
        {
            displayText  = display;
            keysGrid     = grid;
            backspaceBtn = bksp;
            confirmBtn   = confirm;
            cancelBtn    = cancel;
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            BuildKeys();
            backspaceBtn?.onClick.AddListener(() => { if (_input.Length > 0) _input = _input[..^1]; Refresh(); });
            confirmBtn?.onClick.AddListener(Confirm);
            cancelBtn?.onClick.AddListener(Cancel);
        }

        public void Open(string initial, Action<string> onConfirm, Action onCancel = null)
        {
            _input     = initial ?? "";
            _onConfirm = onConfirm;
            _onCancel  = onCancel;
            Refresh();
            gameObject.SetActive(true);
        }

        private void Confirm()
        {
            string result = _input.Trim();
            gameObject.SetActive(false);
            _onConfirm?.Invoke(result);
        }

        private void Cancel()
        {
            gameObject.SetActive(false);
            _onCancel?.Invoke();
        }

        private void Refresh()
        {
            if (displayText != null) displayText.text = _input;
        }

        private void BuildKeys()
        {
            if (keysGrid == null) return;

            // Configurar backspace con ancho fijo
            if (backspaceBtn != null)
            {
                var le = backspaceBtn.GetComponent<LayoutElement>()
                         ?? backspaceBtn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 120f;
                le.flexibleWidth  = 0f;

                var hlg = backspaceBtn.transform.parent?.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.childForceExpandWidth = false;
            }

            foreach (string key in Keys)
                CreateKey(key);
        }

        private void CreateKey(string key)
        {
            var go = new GameObject("Key_" + key.Trim(), typeof(RectTransform));
            go.transform.SetParent(keysGrid, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.35f, 0.45f);

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
            txt.text      = key == " " ? "SP" : key;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize  = 28;
            txt.color     = Color.white;

            string captured = key;
            btn.onClick.AddListener(() => { _input += captured; Refresh(); });
        }
    }
}

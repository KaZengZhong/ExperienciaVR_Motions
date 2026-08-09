using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuplex.WebView;

namespace Vortices
{
    /// <summary>
    /// Panel de navegador flotante que aparece frente al jugador cuando presiona "Investigar".
    /// Layout vertical (de abajo hacia arriba):
    ///   0 % –  7 % → barra botones (Atrás / ▼ / ▲ / Cerrar)
    ///   7 % – 22 % → teclado virtual
    ///  22 % – 100% → WebView
    /// </summary>
    public class TotemBrowser : MonoBehaviour
    {
        [Header("Referencia Vuplex")]
        [Tooltip("Arrastra aquí el prefab 'CanvasWebViewPrefab' de Thirdparty/Vuplex/WebView/")]
        public GameObject canvasWebViewPrefab;
        [Tooltip("Arrastra aquí el prefab 'CanvasKeyboard' de Thirdparty/Vuplex/WebView/")]
        public GameObject canvasKeyboardPrefab;

        [Header("Tamaño del navegador (metros)")]
        public float browserWidth  = 1.4f;
        public float browserHeight = 1.0f;

        [Header("Posición frente al jugador")]
        public float distanceFromPlayer = 0.7f;
        public float heightOffset = 0.2f;

        // ─── Estado interno ───────────────────────────────────────────────────
        private GameObject browserCanvas;
        private bool isOpen = false;
        private bool editorMovementWasEnabled = false;

        // ─────────────────────────────────────────────────────────────────────

        public void OpenBrowser(string url)
        {
            if (isOpen) return;
            SetMovementMode(false);
            StartCoroutine(SpawnBrowser(url));
        }

        public void CloseBrowser()
        {
            if (browserCanvas != null) Destroy(browserCanvas);
            isOpen = false;
            StartCoroutine(RestoreMovementDelayed());
        }

        private IEnumerator RestoreMovementDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            SetMovementMode(true);
            LocomotionSettings ls = FindObjectOfType<LocomotionSettings>();
            if (ls != null) ls.ApplySettings();
        }

        private void SetMovementMode(bool moving)
        {
            // Cursor: libre al abrir el browser, bloqueado al moverse
            Cursor.visible   = !moving;
            Cursor.lockState = moving ? CursorLockMode.Locked : CursorLockMode.None;

            // Deshabilitar EditorMovement (WASD / mouse look)
            EditorMovement em = FindObjectOfType<EditorMovement>();
            if (em != null)
            {
                if (!moving)
                {
                    editorMovementWasEnabled = em.enabled;
                    em.enabled = false;
                }
                else
                {
                    em.enabled = editorMovementWasEnabled;
                }
            }

            // Deshabilitar/restaurar proveedores de locomoción XR
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                if (!moving)
                {
                    foreach (MonoBehaviour comp in xrOrigin.GetComponents<MonoBehaviour>())
                    {
                        string t = comp.GetType().Name;
                        if (t.Contains("MoveProvider") || t.Contains("TurnProvider") || t.Contains("LocomotionProvider"))
                            comp.enabled = false;
                    }
                }
                else
                {
                    bool joystick      = PlayerPrefs.GetInt("movementMode", 0) == 0;
                    bool teleportation = PlayerPrefs.GetInt("movementMode", 0) == 1;
                    bool headRotation  = PlayerPrefs.GetInt("rotationMode",  0) == 1;

                    foreach (MonoBehaviour comp in xrOrigin.GetComponents<MonoBehaviour>())
                    {
                        string t = comp.GetType().Name;
                        if      (t.Contains("ContinuousMoveProvider"))  comp.enabled = joystick;
                        else if (t.Contains("TeleportationProvider"))    comp.enabled = teleportation;
                        else if (t.Contains("ContinuousTurnProvider"))   comp.enabled = !headRotation;
                        else if (t.Contains("LocomotionProvider"))       comp.enabled = true;
                    }
                }
            }
        }

        // ─── Spawning ─────────────────────────────────────────────────────────

        private IEnumerator SpawnBrowser(string url)
        {
            isOpen = true;

            // 1. Canvas WorldSpace
            browserCanvas = new GameObject("TotemBrowserCanvas");
            Canvas canvas = browserCanvas.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            int   pixelWidth  = 1200;
            int   pixelHeight = Mathf.RoundToInt(pixelWidth * (browserHeight / browserWidth));
            float scaleFactor = browserWidth / pixelWidth;

            RectTransform canvasRt = browserCanvas.GetComponent<RectTransform>();
            canvasRt.sizeDelta  = new Vector2(pixelWidth, pixelHeight);
            canvasRt.localScale = Vector3.one * scaleFactor;

            browserCanvas.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;
            browserCanvas.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            // 2. Posición
            PositionInFrontOfTotem();

            // 3. Fondo
            CreateBackground();

            // 4. Botón Cerrar (se crea aquí para que quede visible, Back se crea tras init WebView)
            CreateCloseButton();

#if UNITY_EDITOR
            CreateEditorPreview(url);
            Debug.Log($"[TotemBrowser] [EDITOR] Vista previa: {url}");
            yield break;
#else
            if (canvasWebViewPrefab == null)
            {
                Debug.LogError("[TotemBrowser] Falta CanvasWebViewPrefab.");
                CreateFallbackMessage(url);
                yield break;
            }

            // 5. WebView — ocupa de 22% hacia arriba (se reajusta tras agregar teclado)
            GameObject webViewObj = Instantiate(canvasWebViewPrefab,
                                                browserCanvas.transform.position,
                                                browserCanvas.transform.rotation,
                                                browserCanvas.transform);
            webViewObj.name = "WebView";
            webViewObj.transform.localScale = Vector3.one;

            RectTransform webViewRt = webViewObj.GetComponent<RectTransform>();
            if (webViewRt == null) webViewRt = webViewObj.AddComponent<RectTransform>();
            webViewRt.anchorMin = new Vector2(0f, 0.28f);
            webViewRt.anchorMax = Vector2.one;
            webViewRt.offsetMin = Vector2.zero;
            webViewRt.offsetMax = Vector2.zero;

            CanvasWebViewPrefab canvasWebView = webViewObj.GetComponent<CanvasWebViewPrefab>();
            if (canvasWebView == null) { Debug.LogError("[TotemBrowser] Sin CanvasWebViewPrefab."); yield break; }

            var initTask = canvasWebView.WaitUntilInitialized();
            while (!initTask.IsCompleted) yield return null;

            canvasWebView.WebView.LoadUrl(url);
            Debug.Log($"[TotemBrowser] Navegador abierto: {url}");

            // Scroll con rueda del mouse (XRUIInputModule no lo reenvía automáticamente)
            StartCoroutine(HandleMouseScroll(canvasWebView.WebView));

            // Botones que necesitan referencia al WebView
            CreateBackButton(canvasWebView);
            CreateScrollButtons(canvasWebView);

            // 6. Teclado virtual (pequeño, 7%–22%)
            SpawnKeyboard();
#endif
        }

        // ─── Scroll con mouse ─────────────────────────────────────────────────

        private IEnumerator HandleMouseScroll(IWebView webView)
        {
            while (isOpen && webView != null)
            {
                float delta = Input.mouseScrollDelta.y;
                if (Mathf.Abs(delta) > 0.001f)
                {
                    // delta > 0 = rueda hacia arriba = subir página (y negativo en Vuplex)
                    int amount = Mathf.RoundToInt(delta * 1f);
                    webView.Scroll(new Vector2Int(0, -amount));
                }
                yield return null;
            }
        }

        // ─── Posición ─────────────────────────────────────────────────────────

        private void PositionInFrontOfTotem()
        {
            Vector3 totemPos  = transform.position;
            Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : totemPos + transform.forward;

            Vector3 toPlayer = playerPos - totemPos;
            toPlayer.y = 0f;
            if (toPlayer.magnitude < 0.01f) toPlayer = transform.forward;
            toPlayer.Normalize();

            browserCanvas.transform.position = totemPos + toPlayer * distanceFromPlayer + Vector3.up * heightOffset;
            browserCanvas.transform.rotation = Quaternion.LookRotation(-toPlayer);
        }

        // ─── UI ───────────────────────────────────────────────────────────────

        private void CreateBackground()
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(browserCanvas.transform, false);
            bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);
            RectTransform rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void SpawnKeyboard()
        {
            if (canvasKeyboardPrefab == null) { Debug.LogWarning("[TotemBrowser] Falta CanvasKeyboard."); return; }

            GameObject kbObj = Instantiate(canvasKeyboardPrefab, browserCanvas.transform);
            kbObj.name = "Keyboard";

            RectTransform kbRt = kbObj.GetComponent<RectTransform>();
            if (kbRt != null)
            {
                // Teclado ocupa franja 7%–38%
                kbRt.anchorMin = new Vector2(0f, 0.07f);
                kbRt.anchorMax = new Vector2(1f, 0.28f);
                kbRt.offsetMin = Vector2.zero;
                kbRt.offsetMax = Vector2.zero;
            }

            // Desactivar scrollbars horizontales y verticales del CanvasKeyboard
            foreach (ScrollRect sr in kbObj.GetComponentsInChildren<ScrollRect>(true))
            {
                sr.horizontal          = false;
                sr.vertical            = false;
                sr.horizontalScrollbar = null;
                sr.verticalScrollbar   = null;
            }
            // Ocultar los GameObjects de scrollbar si existen
            foreach (Scrollbar sb in kbObj.GetComponentsInChildren<Scrollbar>(true))
                sb.gameObject.SetActive(false);

            CanvasKeyboard keyboard = kbObj.GetComponent<CanvasKeyboard>();
            if (keyboard == null) Debug.LogWarning("[TotemBrowser] Prefab sin CanvasKeyboard.");
            else                  Debug.Log("[TotemBrowser] Teclado virtual activo.");
        }

        // ─── Botones ──────────────────────────────────────────────────────────
        // Barra inferior: [Atrás 0–28%] [▼ 28–43%] [▲ 43–57%] [Cerrar 57–100%]

        private void CreateBackButton(CanvasWebViewPrefab canvasWebView)
        {
            MakeButton("BackButton", "< Atras", new Color(0.2f, 0.2f, 0.6f),
                new Vector2(0f, 0f), new Vector2(0.28f, 0.07f),
                () => canvasWebView.WebView.GoBack());
        }

        private void CreateScrollButtons(CanvasWebViewPrefab canvasWebView)
        {
            MakeButton("ScrollDown", "▼", new Color(0.25f, 0.25f, 0.25f),
                new Vector2(0.28f, 0f), new Vector2(0.43f, 0.07f),
                () => canvasWebView.WebView.Scroll(new Vector2Int(0,  120)));

            MakeButton("ScrollUp", "▲", new Color(0.25f, 0.25f, 0.25f),
                new Vector2(0.43f, 0f), new Vector2(0.57f, 0.07f),
                () => canvasWebView.WebView.Scroll(new Vector2Int(0, -120)));
        }

        private void CreateCloseButton()
        {
            MakeButton("CloseButton", "X  Cerrar", new Color(0.8f, 0.2f, 0.2f),
                new Vector2(0.57f, 0f), new Vector2(1f, 0.07f),
                CloseBrowser);
        }

        private void MakeButton(string objName, string label, Color color,
            Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btn = new GameObject(objName);
            btn.transform.SetParent(browserCanvas.transform, false);
            btn.AddComponent<Image>().color = color;
            btn.AddComponent<Button>().onClick.AddListener(onClick);
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(3f, 4f); rt.offsetMax = new Vector2(-3f, -4f);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        // ─── Editor preview ───────────────────────────────────────────────────

        private void CreateEditorPreview(string url)
        {
            GameObject bar = new GameObject("AddressBar");
            bar.transform.SetParent(browserCanvas.transform, false);
            bar.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.88f); barRt.anchorMax = Vector2.one;
            barRt.offsetMin = new Vector2(8f,-4f);     barRt.offsetMax = new Vector2(-8f,-8f);
            GameObject barTxt = new GameObject("Url"); barTxt.transform.SetParent(bar.transform, false);
            TextMeshProUGUI bt = barTxt.AddComponent<TextMeshProUGUI>();
            bt.text = "  " + url; bt.fontSize = 22; bt.color = new Color(0.9f,0.9f,0.9f);
            bt.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform btRt = barTxt.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;

            GameObject content = new GameObject("BrowserContent");
            content.transform.SetParent(browserCanvas.transform, false);
            content.AddComponent<Image>().color = Color.white;
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.08f); cRt.anchorMax = new Vector2(1f, 0.88f);
            cRt.offsetMin = new Vector2(8f, 4f);    cRt.offsetMax = new Vector2(-8f,-4f);
            GameObject cTxt = new GameObject("Text"); cTxt.transform.SetParent(content.transform, false);
            TextMeshProUGUI ct = cTxt.AddComponent<TextMeshProUGUI>();
            ct.text = "<b>[Vista previa - Editor]</b>\n\nEn la build se cargará:\n\n" + url +
                      "\n\n<size=20><color=#888888>Vuplex solo funciona en la build.</color></size>";
            ct.fontSize = 28; ct.color = new Color(0.15f,0.15f,0.15f);
            ct.alignment = TextAlignmentOptions.Center;
            RectTransform ctRt = cTxt.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = new Vector2(20f,20f); ctRt.offsetMax = new Vector2(-20f,-20f);
        }

        private void CreateFallbackMessage(string url)
        {
            GameObject msg = new GameObject("Fallback");
            msg.transform.SetParent(browserCanvas.transform, false);
            TextMeshProUGUI tmp = msg.AddComponent<TextMeshProUGUI>();
            tmp.text = $"Navega a:\n{url}\n\n(Asigna CanvasWebViewPrefab en el Inspector)";
            tmp.fontSize = 32; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            RectTransform rt = msg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f,60f); rt.offsetMax = new Vector2(-20f,-20f);
        }

        private void OnDestroy()
        {
            if (browserCanvas != null) Destroy(browserCanvas);
        }
    }
}
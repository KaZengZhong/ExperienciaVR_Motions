using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuplex.WebView;

namespace Vortices
{
    /// <summary>
    /// Panel de navegador flotante que aparece frente al jugador cuando presiona "Investigar".
    /// Usa Vuplex WebView para mostrar contenido web real (Google por defecto).
    /// Añade este componente al mismo GameObject que InformationTotem.
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
        [Tooltip("Distancia en metros desde el tótem hacia el interior del pasillo")]
        public float distanceFromPlayer = 0.7f;
        [Tooltip("Altura respecto a la posición del jugador")]
        public float heightOffset = 0.2f;

        // ─── Estado interno ───────────────────────────────────────────────────
        private GameObject browserCanvas;   // Canvas WorldSpace que contiene WebView + teclado + botones
        private bool isOpen = false;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Abre el navegador flotante con la URL indicada.
        /// </summary>
        public void OpenBrowser(string url)
        {
            if (isOpen) return;
            SetMovementMode(false); // liberar cursor al abrir
            StartCoroutine(SpawnBrowser(url));
        }

        /// <summary>
        /// Cierra y destruye el panel del navegador.
        /// </summary>
        public void CloseBrowser()
        {
            if (browserCanvas != null)
                Destroy(browserCanvas); // destruye browser + teclado (son hijos del mismo canvas)
            isOpen = false;
            SetMovementMode(true);
        }

        /// <summary>
        /// Activa o desactiva el movimiento del jugador y el bloqueo del cursor.
        /// Al abrir el browser, cambia el input module a StandaloneInputModule para que
        /// el mouse funcione con el canvas en world space.
        /// </summary>
        private void SetMovementMode(bool moving)
        {
            // Deshabilitar EditorMovement (WASD / mouse)
            EditorMovement em = FindObjectOfType<EditorMovement>();
            if (em != null)
                em.enabled = moving;

            // Deshabilitar los proveedores de locomoción XR
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                foreach (MonoBehaviour comp in xrOrigin.GetComponents<MonoBehaviour>())
                {
                    string typeName = comp.GetType().Name;
                    if (typeName.Contains("MoveProvider") ||
                        typeName.Contains("TurnProvider") ||
                        typeName.Contains("LocomotionProvider"))
                    {
                        comp.enabled = moving;
                    }
                }
            }
        }

        // ─── Spawning ─────────────────────────────────────────────────────────

        private IEnumerator SpawnBrowser(string url)
        {
            isOpen = true;

            // 1. Crear el Canvas WorldSpace
            browserCanvas = new GameObject("TotemBrowserCanvas");

            Canvas canvas = browserCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            // El canvas incluye: WebView (60%) + Teclado (33%) + Barra botones (7%)
            // Así todo queda en un solo canvas, sin problemas de posicionamiento.
            int pixelWidth  = 1200;
            int pixelHeight = Mathf.RoundToInt(pixelWidth * (browserHeight / browserWidth));
            float scaleFactor = browserWidth / pixelWidth;

            RectTransform canvasRt = browserCanvas.GetComponent<RectTransform>();
            canvasRt.sizeDelta  = new Vector2(pixelWidth, pixelHeight);
            canvasRt.localScale = Vector3.one * scaleFactor;

            // Canvas Scaler
            CanvasScaler scaler = browserCanvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1f;

            // TrackedDeviceGraphicRaycaster para que los controladores XR interactúen
            // con el WebView, el teclado y los botones
            browserCanvas.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            // 2. Posicionar frente al tótem (= frente al jugador, que está mirando el tótem)
            PositionInFrontOfTotem();

            // 3. Fondo oscuro del panel (para que no quede transparente)
            CreateBackground(pixelWidth, pixelHeight);

            // 4. Botón Cerrar (arriba a la derecha)
            CreateCloseButton(pixelWidth, pixelHeight);

            // 5. Área de contenido web
#if UNITY_EDITOR
            // En el Editor mostramos un panel simple — Vuplex no renderiza en editor.
            // En la build final se usa el WebView real.
            CreateEditorPreview(pixelWidth, pixelHeight, url);
            Debug.Log($"[TotemBrowser] [EDITOR] Panel de vista previa abierto para: {url}");
            yield break;
#else
            if (canvasWebViewPrefab == null)
            {
                Debug.LogError("[TotemBrowser] No hay prefab de CanvasWebViewPrefab asignado.");
                CreateFallbackMessage(pixelWidth, pixelHeight, url);
                yield break;
            }

            GameObject webViewObj = Instantiate(canvasWebViewPrefab,
                                                browserCanvas.transform.position,
                                                browserCanvas.transform.rotation,
                                                browserCanvas.transform);
            webViewObj.name = "WebView";

            RectTransform webViewRt = webViewObj.GetComponent<RectTransform>();
            if (webViewRt == null) webViewRt = webViewObj.AddComponent<RectTransform>();

            // El WebView ocupa la parte superior del canvas, justo encima del botón cerrar
            webViewRt.anchorMin = new Vector2(0f, 0.07f);
            webViewRt.anchorMax = Vector2.one;
            webViewRt.offsetMin = Vector2.zero;
            webViewRt.offsetMax = Vector2.zero;
            webViewObj.transform.localScale = Vector3.one;

            CanvasWebViewPrefab canvasWebView = webViewObj.GetComponent<CanvasWebViewPrefab>();
            if (canvasWebView == null)
            {
                Debug.LogError("[TotemBrowser] El prefab no tiene componente CanvasWebViewPrefab.");
                yield break;
            }

            var initTask = canvasWebView.WaitUntilInitialized();
            while (!initTask.IsCompleted)
                yield return null;

            canvasWebView.WebView.LoadUrl(url);

            // Desactivar input del teclado físico al WebView.
            // El CanvasKeyboard sigue funcionando porque envía teclas directamente por código.
            canvasWebView.WebView.SetFocused(false);

            Debug.Log($"[TotemBrowser] Navegador abierto con URL: {url}");

            // Agregar botón de atrás ahora que tenemos referencia al WebView
            CreateBackButton(canvasWebView);

            // Instanciar teclado en pantalla debajo del WebView
            SpawnKeyboard(canvasWebView);
#endif
        }

        private void PositionInFrontOfTotem()
        {
            // Dirección desde el tótem hacia el jugador (Camera.main)
            // Esto garantiza que el browser siempre aparece entre el tótem y el jugador,
            // independientemente del forward del tótem.
            Vector3 totemPos = transform.position;
            Vector3 playerPos = Camera.main != null
                ? Camera.main.transform.position
                : totemPos + transform.forward;

            Vector3 toPlayer = playerPos - totemPos;
            toPlayer.y = 0f;
            if (toPlayer.magnitude < 0.01f) toPlayer = transform.forward;
            toPlayer.Normalize();

            browserCanvas.transform.position = totemPos
                + toPlayer * distanceFromPlayer
                + Vector3.up * heightOffset;

            // El canvas debe mirar HACIA el jugador: su cara visible (Z-) apunta al jugador
            browserCanvas.transform.rotation = Quaternion.LookRotation(-toPlayer);

            Debug.Log($"[TotemBrowser] Canvas en: {browserCanvas.transform.position}, " +
                      $"Totem en: {totemPos}, hacia jugador: {toPlayer}");
        }

        private void CreateBackground(int w, int h)
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(browserCanvas.transform, false);

            Image img = bg.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            RectTransform rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void SpawnKeyboard(CanvasWebViewPrefab canvasWebView)
        {
            if (canvasKeyboardPrefab == null)
            {
                Debug.LogWarning("[TotemBrowser] No hay prefab de CanvasKeyboard asignado.");
                return;
            }

            // El teclado va DENTRO del mismo canvas del browser.
            // Layout de anchors (de abajo hacia arriba):
            //   0.00 - 0.07 : barra botones (Atrás / Cerrar)
            //   0.07 - 0.40 : teclado
            //   0.40 - 1.00 : WebView (Google)
            GameObject kbObj = Instantiate(canvasKeyboardPrefab, browserCanvas.transform);
            kbObj.name = "Keyboard";

            RectTransform kbRt = kbObj.GetComponent<RectTransform>();
            if (kbRt != null)
            {
                kbRt.anchorMin = new Vector2(0f, 0.07f);
                kbRt.anchorMax = new Vector2(1f, 0.40f);
                kbRt.offsetMin = new Vector2(4f, 4f);
                kbRt.offsetMax = new Vector2(-4f, -4f);
            }

            // Reajustar el WebView para dejar espacio al teclado
            RectTransform webViewRt = browserCanvas.transform.Find("WebView")?.GetComponent<RectTransform>();
            if (webViewRt != null)
            {
                webViewRt.anchorMin = new Vector2(0f, 0.40f);
                webViewRt.anchorMax = Vector2.one;
                webViewRt.offsetMin = Vector2.zero;
                webViewRt.offsetMax = Vector2.zero;
            }

            // Vuplex conecta el teclado automáticamente al WebView activo
            CanvasKeyboard keyboard = kbObj.GetComponent<CanvasKeyboard>();
            if (keyboard == null)
                Debug.LogWarning("[TotemBrowser] El prefab CanvasKeyboard no tiene componente CanvasKeyboard.");

            Debug.Log("[TotemBrowser] Teclado en pantalla activado.");
        }

        private void CreateBackButton(CanvasWebViewPrefab canvasWebView)
        {
            GameObject btnObj = new GameObject("BackButton");
            btnObj.transform.SetParent(browserCanvas.transform, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.6f, 1f); // azul oscuro

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                canvasWebView.WebView.GoBack();
            });

            // Ocupa la mitad izquierda de la barra inferior
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0.07f);
            rt.offsetMin = new Vector2(10f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "< Atras";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateCloseButton(int w, int h)
        {
            // Contenedor del botón
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(browserCanvas.transform, false);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(CloseBrowser);

            // Ocupa la mitad derecha de la barra inferior (la izquierda es para "Atrás")
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(1f, 0.07f);
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-10f, -4f);

            // Texto del botón
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "X  Cerrar navegador";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateEditorPreview(int w, int h, string url)
        {
            // Barra de dirección (parte superior)
            GameObject barObj = new GameObject("AddressBar");
            barObj.transform.SetParent(browserCanvas.transform, false);

            Image barImg = barObj.AddComponent<Image>();
            barImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.88f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.offsetMin = new Vector2(8f, -4f);
            barRt.offsetMax = new Vector2(-8f, -8f);

            GameObject barTextObj = new GameObject("UrlText");
            barTextObj.transform.SetParent(barObj.transform, false);
            TextMeshProUGUI barTmp = barTextObj.AddComponent<TextMeshProUGUI>();
            barTmp.text = "  " + url;
            barTmp.fontSize = 22;
            barTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            barTmp.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform barTextRt = barTextObj.GetComponent<RectTransform>();
            barTextRt.anchorMin = Vector2.zero;
            barTextRt.anchorMax = Vector2.one;
            barTextRt.offsetMin = Vector2.zero;
            barTextRt.offsetMax = Vector2.zero;

            // Área de contenido simulado
            GameObject contentObj = new GameObject("BrowserContent");
            contentObj.transform.SetParent(browserCanvas.transform, false);

            Image contentImg = contentObj.AddComponent<Image>();
            contentImg.color = new Color(1f, 1f, 1f, 1f); // fondo blanco como una página web

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0.08f);
            contentRt.anchorMax = new Vector2(1f, 0.88f);
            contentRt.offsetMin = new Vector2(8f, 4f);
            contentRt.offsetMax = new Vector2(-8f, -4f);

            GameObject contentTextObj = new GameObject("PreviewText");
            contentTextObj.transform.SetParent(contentObj.transform, false);
            TextMeshProUGUI contentTmp = contentTextObj.AddComponent<TextMeshProUGUI>();
            contentTmp.text = "<b>[Vista previa - Editor]</b>\n\nEn la build final aqui se cargara:\n\n" + url +
                              "\n\n<size=20><color=#888888>El navegador real (Vuplex) solo funciona en la build compilada.</color></size>";
            contentTmp.fontSize = 28;
            contentTmp.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            contentTmp.alignment = TextAlignmentOptions.Center;
            RectTransform contentTextRt = contentTextObj.GetComponent<RectTransform>();
            contentTextRt.anchorMin = Vector2.zero;
            contentTextRt.anchorMax = Vector2.one;
            contentTextRt.offsetMin = new Vector2(20f, 20f);
            contentTextRt.offsetMax = new Vector2(-20f, -20f);
        }

        private void CreateFallbackMessage(int w, int h, string url)
        {
            // Si no hay WebView prefab, muestra un mensaje con la URL
            GameObject msgObj = new GameObject("FallbackMessage");
            msgObj.transform.SetParent(browserCanvas.transform, false);

            TextMeshProUGUI tmp = msgObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"Navega a:\n{url}\n\n(Asigna el CanvasWebViewPrefab en el Inspector)";
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform rt = msgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f, 60f);
            rt.offsetMax = new Vector2(-20f, -20f);
        }

        private void OnDestroy()
        {
            // Limpiar el canvas si el tótem se destruye
            if (browserCanvas != null)
                Destroy(browserCanvas);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using Mirror;

namespace Vortices
{
    public class InformationTotem : MonoBehaviour
    {
        [Header("Configuración (se sobreescribe con SetContent)")]
        public string question = "¿Esta información es real o falsa?";
        public Sprite informationImage;
        public bool isReal = true;

        [Header("Referencias UI")]
        public GameObject panel;
        public TextMeshProUGUI questionText;    // Titular
        public Image displayImage;             // Imagen principal
        public RawImage videoDisplay;          // RawImage donde se renderiza el video
        public Button realButton;
        public Button fakeButton;
        public Button investigarButton;

        [Header("Reproductor")]
        public Button playPauseButton;         // Botón play/pause compartido para video y audio
        public AudioSource audioSource;        // AudioSource para narración o audio

        [Header("Feedback")]
        public GameObject correctFeedback;
        public GameObject incorrectFeedback;

        // ─── Estado interno ───────────────────────────────────────────────────────
        private bool  xrOriginInRange  = false;
        private bool  answered         = false;
        private bool  usedInvestigar   = false;
        private float approachTime;
        private Transform xrOrigin;

        private ProceduralMapGenerator mapGenerator;
        private TotemBrowser           totemBrowser;
        private TotemContentItem       currentContent;

        // Video
        private VideoPlayer   videoPlayer;
        private RenderTexture videoRenderTexture;

        // Estado reproductor
        private bool      isPlaying        = false;
        private Coroutine audioCoroutine   = null;
        private Coroutine videoCoroutine   = null;

        // ─────────────────────────────────────────────────────────────────────────

        public void SetMapGenerator(ProceduralMapGenerator generator)
        {
            mapGenerator = generator;
        }

        /// <summary>
        /// Asigna el contenido cargado desde el JSON.
        /// </summary>
        public void SetContent(TotemContentItem item)
        {
            if (item == null) return;
            currentContent   = item;
            question         = item.headline;
            isReal           = item.isReal;
            informationImage = item.sprite;
        }

        void Start()
        {
            if (panel != null)             panel.SetActive(false);
            if (correctFeedback != null)   correctFeedback.SetActive(false);
            if (incorrectFeedback != null) incorrectFeedback.SetActive(false);

            if (realButton != null)       realButton.onClick.AddListener(OnRealSelected);
            if (fakeButton != null)       fakeButton.onClick.AddListener(OnFakeSelected);
            if (investigarButton != null) investigarButton.onClick.AddListener(OnInvestigarSelected);
            if (playPauseButton != null)  playPauseButton.onClick.AddListener(OnPlayPauseSelected);

            totemBrowser = GetComponent<TotemBrowser>();
            if (totemBrowser == null)
                totemBrowser = gameObject.AddComponent<TotemBrowser>();

            // VideoPlayer — solo se inicializa si hay un videoDisplay asignado
            if (videoDisplay != null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();

                videoPlayer.playOnAwake     = false;
                videoPlayer.renderMode      = VideoRenderMode.RenderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                if (audioSource != null)
                    videoPlayer.SetTargetAudioSource(0, audioSource);
            }

            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
                xrOrigin = xrOriginObj.transform;
            else
                Debug.LogWarning("[Totem] No se encontró 'XR Origin' en la escena.");
        }

        void Update()
        {
            if (xrOrigin == null || answered) return;

            bool inRange = IsPlayerInSameCell();

            if (inRange && !xrOriginInRange)
            {
                xrOriginInRange = true;
                approachTime    = Time.time;
                usedInvestigar  = false;
                ShowPanel();
            }
            else if (!inRange && xrOriginInRange)
            {
                xrOriginInRange = false;
                HidePanel();
            }
        }

        private bool IsPlayerInSameCell()
        {
            if (mapGenerator == null)
                return Vector3.Distance(transform.position, xrOrigin.position) < 3f;

            float s = mapGenerator.cellSize;
            Vector2Int playerCell = new Vector2Int(
                Mathf.FloorToInt(xrOrigin.position.x / s),
                Mathf.FloorToInt(xrOrigin.position.z / s)
            );
            Vector2Int totemCell = new Vector2Int(
                Mathf.FloorToInt(transform.position.x / s),
                Mathf.FloorToInt(transform.position.z / s)
            );
            return playerCell == totemCell;
        }

        // ─── UI ──────────────────────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (panel == null) return;
            panel.SetActive(true);

            if (questionText != null)
                questionText.text = question;

            if (displayImage != null)
            {
                displayImage.gameObject.SetActive(informationImage != null);
                if (informationImage != null)
                    displayImage.sprite = informationImage;
            }

            if (investigarButton != null)
                investigarButton.gameObject.SetActive(currentContent != null && !string.IsNullOrEmpty(currentContent.searchUrl));

            if (currentContent == null) return;

            // Mostrar botón play/pause solo si hay video o audio
            bool hasMedia = !string.IsNullOrEmpty(currentContent.videoUrl) ||
                            !string.IsNullOrEmpty(currentContent.audioUrl);
            if (playPauseButton != null)
                playPauseButton.gameObject.SetActive(hasMedia);

            // Video
            if (!string.IsNullOrEmpty(currentContent.videoUrl))
                videoCoroutine = StartCoroutine(PlayVideo(currentContent.videoUrl));
            else if (videoDisplay != null)
                videoDisplay.gameObject.SetActive(false);

            // Audio
            if (!string.IsNullOrEmpty(currentContent.audioUrl))
                audioCoroutine = StartCoroutine(LoadAndPlayAudio(currentContent.audioUrl));
        }

        private void HidePanel()
        {
            if (panel != null) panel.SetActive(false);

            if (audioCoroutine != null) { StopCoroutine(audioCoroutine); audioCoroutine = null; }
            if (videoCoroutine != null) { StopCoroutine(videoCoroutine); videoCoroutine = null; }

            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
            if (audioSource  != null && audioSource.isPlaying)  audioSource.Stop();

            isPlaying = false;
        }

        // ─── Video ────────────────────────────────────────────────────────────────

        private IEnumerator PlayVideo(string url)
        {
            if (videoPlayer == null || videoDisplay == null) yield break;

            // GitHub raw no soporta streaming — descargamos primero a caché local
            string fileName = System.IO.Path.GetFileName(url.Split('?')[0]);
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, fileName);

            // Verificar que el archivo exista y no esté vacío (podría estar corrupto de un intento anterior)
            bool cacheValid = System.IO.File.Exists(tempPath) &&
                              new System.IO.FileInfo(tempPath).Length > 1024;

            if (!cacheValid)
            {
                Debug.Log($"[Totem] Descargando video a caché: {url}");
                UnityWebRequest dlReq = UnityWebRequest.Get(url);
                dlReq.downloadHandler = new DownloadHandlerFile(tempPath);
                yield return dlReq.SendWebRequest();

                if (dlReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Totem] No se pudo descargar el video: {dlReq.error}");
                    if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
                    yield break;
                }
                Debug.Log($"[Totem] Video descargado en: {tempPath}");
            }
            else
            {
                Debug.Log($"[Totem] Video cargado desde caché: {tempPath} ({new System.IO.FileInfo(tempPath).Length / 1024} KB)");
            }

            if (videoRenderTexture == null)
                videoRenderTexture = new RenderTexture(1280, 720, 0);

            videoPlayer.targetTexture = videoRenderTexture;
            videoDisplay.texture      = videoRenderTexture;
            videoDisplay.gameObject.SetActive(true);

            videoPlayer.url = new System.Uri(tempPath).AbsoluteUri;
            videoPlayer.Prepare();

            float timeout = 15f;
            float elapsed = 0f;
            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (videoPlayer.isPrepared)
            {
                videoPlayer.Play();
                isPlaying = true;
                SetPlayPauseText("Pausar");
            }
            else
                Debug.LogWarning($"[Totem] Timeout preparando video: {tempPath}");
        }

        // ─── Audio ────────────────────────────────────────────────────────────────

        private IEnumerator LoadAndPlayAudio(string url)
        {
            if (audioSource == null) yield break;

            AudioType audioType = AudioType.MPEG;
            if (url.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase)) audioType = AudioType.WAV;
            if (url.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase)) audioType = AudioType.OGGVORBIS;

            UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (!xrOriginInRange) yield break;

                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                audioSource.clip = clip;
                audioSource.Play();
                isPlaying = true;
                SetPlayPauseText("Pausar");
                Debug.Log($"[Totem] Reproduciendo audio: {url}");
            }
            else
            {
                Debug.LogWarning($"[Totem] No se pudo cargar audio '{url}': {req.error}");
            }
        }

        // ─── Botón Play/Pause ─────────────────────────────────────────────────────

        public void OnPlayPauseSelected()
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                if (videoPlayer != null && videoPlayer.isPrepared) videoPlayer.Play();
                if (audioSource  != null && audioSource.clip != null) audioSource.Play();
                SetPlayPauseText("Pausar");
            }
            else
            {
                if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Pause();
                if (audioSource  != null && audioSource.isPlaying)  audioSource.Pause();
                SetPlayPauseText("Reanudar");
            }
        }

        private void SetPlayPauseText(string text)
        {
            if (playPauseButton == null) return;
            TextMeshProUGUI label = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }

        // ─── Botón Investigar ─────────────────────────────────────────────────────

        public void OnInvestigarSelected()
        {
            usedInvestigar = true;
            if (totemBrowser == null) return;
            string url = (currentContent != null && !string.IsNullOrEmpty(currentContent.searchUrl))
                ? currentContent.searchUrl : "https://www.google.com";
            totemBrowser.OpenBrowser(url);
            Debug.Log($"[Totem] Abriendo navegador en: {url}");
        }

        // ─── Respuestas ───────────────────────────────────────────────────────────

        public void OnRealSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            MazeMetricsLogger.Instance?.LogTotemAnswer(question, true, isReal, usedInvestigar, Time.time - approachTime);

            if (isReal) { Debug.Log("[Totem] Correcto — REAL.");    HandleCorrectAnswer(); }
            else        { Debug.Log("[Totem] Incorrecto — FALSA."); HandleIncorrectAnswer(); }

            SendNetworkAnswer(true);
        }

        public void OnFakeSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            MazeMetricsLogger.Instance?.LogTotemAnswer(question, false, isReal, usedInvestigar, Time.time - approachTime);

            if (!isReal) { Debug.Log("[Totem] Correcto — FALSA."); HandleCorrectAnswer(); }
            else         { Debug.Log("[Totem] Incorrecto — REAL."); HandleIncorrectAnswer(); }

            SendNetworkAnswer(false);
        }

        private void SendNetworkAnswer(bool answeredReal)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogWarning("[Totem] No conectado al servidor — modo offline, no se sincroniza.");
                return;
            }

            float cellSize = mapGenerator != null ? mapGenerator.cellSize : 1f;
            int cx = xrOrigin != null ? Mathf.FloorToInt(xrOrigin.position.x / cellSize) : 0;
            int cz = xrOrigin != null ? Mathf.FloorToInt(xrOrigin.position.z / cellSize) : 0;

            NetworkClient.Send(new TotemAnsweredMessage
            {
                totemPosition = transform.position,
                answeredReal  = answeredReal,
                senderCellX   = cx,
                senderCellZ   = cz
            });
            Debug.Log($"[Totem] TotemAnsweredMessage enviado — pos={transform.position}, real={answeredReal}, cell=({cx},{cz})");
        }

        // Llamado desde el RPC en todos los clientes remotos
        public void ApplyNetworkAnswer(bool answeredReal, Vector2Int senderCell)
        {
            if (answered) return; // el jugador local ya lo aplicó, ignorar
            answered = true;
            HidePanel();

            bool isCorrect = (answeredReal == isReal);
            if (isCorrect)
            {
                if (correctFeedback != null)   correctFeedback.SetActive(true);
                if (incorrectFeedback != null) incorrectFeedback.SetActive(false);
                if (mapGenerator != null)      mapGenerator.ShowPartialPathFrom(senderCell);
            }
            else
            {
                if (incorrectFeedback != null) incorrectFeedback.SetActive(true);
                if (correctFeedback != null)   correctFeedback.SetActive(false);
            }
        }

        // ─── Lógica de resultado ──────────────────────────────────────────────────

        private void HandleCorrectAnswer()
        {
            if (correctFeedback != null)   correctFeedback.SetActive(true);
            if (incorrectFeedback != null) incorrectFeedback.SetActive(false);

            if (mapGenerator == null) { Debug.LogWarning("[Totem] No hay referencia al generador de mapa."); return; }
            if (xrOrigin == null)     { Debug.LogWarning("[Totem] No hay referencia al jugador."); return; }

            Vector2Int xrOriginCell = new Vector2Int(
                Mathf.FloorToInt(xrOrigin.position.x / mapGenerator.cellSize),
                Mathf.FloorToInt(xrOrigin.position.z / mapGenerator.cellSize)
            );

            mapGenerator.ShowPartialPathFrom(xrOriginCell);
        }

        private void HandleIncorrectAnswer()
        {
            if (incorrectFeedback != null) incorrectFeedback.SetActive(true);
            if (correctFeedback   != null) correctFeedback.SetActive(false);
        }
    }
}
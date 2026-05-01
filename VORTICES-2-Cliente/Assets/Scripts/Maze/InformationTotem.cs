using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Vortices
{
    public class InformationTotem : MonoBehaviour
    {
        [Header("Noticia")]
        [Tooltip("ScriptableObject con el contenido de esta noticia (crea uno en Assets → Create → Vortices → News Item)")]
        public NewsItem newsItem;

        [Header("Configuración (se sobreescribe con NewsItem si está asignado)")]
        public string question = "¿Esta información es real o falsa?";
        public Sprite informationImage;
        public bool isReal = true;

        [Header("Referencias UI")]
        public GameObject panel;
        public TextMeshProUGUI questionText;
        public Image displayImage;
        public Button realButton;
        public Button fakeButton;
        public Button investigarButton;   // nuevo botón para abrir el navegador

        [Header("Feedback")]
        public GameObject correctFeedback;
        public GameObject incorrectFeedback;

        // ─── Estado interno ───────────────────────────────────────────────────
        private bool xrOriginInRange = false;
        private bool answered = false;
        private Transform xrOrigin;

        private ProceduralMapGenerator mapGenerator;
        private TotemBrowser totemBrowser;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Llamado por ProceduralMapGenerator al instanciar el tótem.
        /// </summary>
        public void SetMapGenerator(ProceduralMapGenerator generator)
        {
            mapGenerator = generator;
        }

        void Start()
        {
            // Aplicar datos del NewsItem si está asignado
            ApplyNewsItem();

            if (panel != null)
                panel.SetActive(false);

            if (correctFeedback != null)   correctFeedback.SetActive(false);
            if (incorrectFeedback != null) incorrectFeedback.SetActive(false);

            if (realButton != null)       realButton.onClick.AddListener(OnRealSelected);
            if (fakeButton != null)       fakeButton.onClick.AddListener(OnFakeSelected);
            if (investigarButton != null) investigarButton.onClick.AddListener(OnInvestigarSelected);

            // Obtener o crear el componente TotemBrowser en este mismo GameObject
            totemBrowser = GetComponent<TotemBrowser>();
            if (totemBrowser == null)
                totemBrowser = gameObject.AddComponent<TotemBrowser>();

            // Usar XR Origin como referencia de posición del jugador
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
                xrOrigin = xrOriginObj.transform;
            else
                Debug.LogWarning("[Totem] No se encontró 'XR Origin' en la escena.");
        }

        void Update()
        {
            if (xrOrigin == null || answered) return;

            float distance = Vector3.Distance(transform.position, xrOrigin.position);

            if (distance < 3f && !xrOriginInRange)
            {
                xrOriginInRange = true;
                ShowPanel();
            }
            else if (distance >= 3f && xrOriginInRange)
            {
                xrOriginInRange = false;
                HidePanel();
            }
        }

        // ─── NewsItem ─────────────────────────────────────────────────────────

        private void ApplyNewsItem()
        {
            if (newsItem == null) return;

            question         = newsItem.headline;
            informationImage = newsItem.image;
            isReal           = newsItem.isReal;
        }

        // ─── UI ──────────────────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (panel == null) return;
            panel.SetActive(true);

            if (questionText != null)
                questionText.text = question;

            if (displayImage != null && informationImage != null)
                displayImage.sprite = informationImage;

            // Mostrar Investigar solo si hay un NewsItem asignado
            if (investigarButton != null)
                investigarButton.gameObject.SetActive(newsItem != null);
        }

        private void HidePanel()
        {
            if (panel != null)
                panel.SetActive(false);

            // El navegador NO se cierra al alejarse — el jugador lo cierra manualmente
            // con el botón "X Cerrar navegador" cuando termina de investigar.
        }

        // ─── Botón Investigar ─────────────────────────────────────────────────

        public void OnInvestigarSelected()
        {
            if (totemBrowser == null) return;

            string url = (newsItem != null) ? newsItem.searchUrl : "https://www.google.com";
            totemBrowser.OpenBrowser(url);

            Debug.Log($"[Totem] Abriendo navegador en: {url}");
        }

        // ─── Respuestas ───────────────────────────────────────────────────────

        public void OnRealSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            if (isReal)
            {
                Debug.Log("[Totem] Correcto — la información es REAL.");
                HandleCorrectAnswer();
            }
            else
            {
                Debug.Log("[Totem] Incorrecto — la información era FALSA.");
                HandleIncorrectAnswer();
            }
        }

        public void OnFakeSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            if (!isReal)
            {
                Debug.Log("[Totem] Correcto — la información es FALSA.");
                HandleCorrectAnswer();
            }
            else
            {
                Debug.Log("[Totem] Incorrecto — la información era REAL.");
                HandleIncorrectAnswer();
            }
        }

        // ─── Lógica de resultado ──────────────────────────────────────────────

        private void HandleCorrectAnswer()
        {
            if (correctFeedback != null)   correctFeedback.SetActive(true);
            if (incorrectFeedback != null) incorrectFeedback.SetActive(false);

            if (mapGenerator == null)
            {
                Debug.LogWarning("[Totem] No hay referencia al generador de mapa.");
                return;
            }

            if (xrOrigin == null)
            {
                Debug.LogWarning("[Totem] No hay referencia al jugador.");
                return;
            }

            Vector2Int xrOriginCell = new Vector2Int(
                Mathf.FloorToInt(xrOrigin.position.x / mapGenerator.cellSize),
                Mathf.FloorToInt(xrOrigin.position.z / mapGenerator.cellSize)
            );

            Debug.Log($"[Totem] Mostrando ruta parcial desde celda del jugador: {xrOriginCell}");
            mapGenerator.ShowPartialPathFrom(xrOriginCell);
        }

        private void HandleIncorrectAnswer()
        {
            if (incorrectFeedback != null) incorrectFeedback.SetActive(true);
            if (correctFeedback != null)   correctFeedback.SetActive(false);
        }
    }
}
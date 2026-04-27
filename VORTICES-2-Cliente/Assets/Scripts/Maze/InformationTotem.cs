using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Vortices
{
    public class InformationTotem : MonoBehaviour
    {
        [Header("Configuración")]
        public string question = "¿Esta información es real o falsa?";
        public Sprite informationImage;
        public bool isReal = true;

        [Header("Referencias UI")]
        public GameObject panel;
        public TextMeshProUGUI questionText;
        public Image displayImage;
        public Button realButton;
        public Button fakeButton;

        [Header("Feedback")]
        public GameObject correctFeedback;
        public GameObject incorrectFeedback;

        private bool xrOriginInRange = false;
        private bool answered = false;
        private Transform xrOrigin;   // posición real del jugador en el mundo virtual

        private ProceduralMapGenerator mapGenerator;

        /// <summary>
        /// Llamado por ProceduralMapGenerator al instanciar el tótem.
        /// </summary>
        public void SetMapGenerator(ProceduralMapGenerator generator)
        {
            mapGenerator = generator;
        }

        void Start()
        {
            if (panel != null)
                panel.SetActive(false);

            if (correctFeedback != null)
                correctFeedback.SetActive(false);
            if (incorrectFeedback != null)
                incorrectFeedback.SetActive(false);

            if (realButton != null)
                realButton.onClick.AddListener(OnRealSelected);
            if (fakeButton != null)
                fakeButton.onClick.AddListener(OnFakeSelected);

            // Usar XR Origin como referencia de posición del jugador
            // Es el objeto raíz del rig XR y su posición representa dónde está parado el jugador
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

        // ─── UI ──────────────────────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (panel == null) return;
            panel.SetActive(true);

            if (questionText != null)
                questionText.text = question;
            if (displayImage != null && informationImage != null)
                displayImage.sprite = informationImage;
        }

        private void HidePanel()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // ─── Respuestas ───────────────────────────────────────────────────────────

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

        // ─── Lógica de resultado ──────────────────────────────────────────────────

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

            // Convertir la posición del jugador en el mundo a celda de la grilla
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
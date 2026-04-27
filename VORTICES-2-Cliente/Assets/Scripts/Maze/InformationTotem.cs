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

        private bool playerInRange = false;
        private bool answered = false;
        private Transform player;

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

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("[Totem] No se encontró objeto con tag 'Player'.");
        }

        void Update()
        {
            if (player == null || answered) return;

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance < 3f && !playerInRange)
            {
                playerInRange = true;
                ShowPanel();
            }
            else if (distance >= 3f && playerInRange)
            {
                playerInRange = false;
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

            if (player == null)
            {
                Debug.LogWarning("[Totem] No hay referencia al jugador.");
                return;
            }

            // Convertir la posición del jugador en el mundo a celda de la grilla
            Vector2Int playerCell = new Vector2Int(
                Mathf.FloorToInt(player.position.x / mapGenerator.cellSize),
                Mathf.FloorToInt(player.position.z / mapGenerator.cellSize)
            );

            Debug.Log($"[Totem] Mostrando ruta parcial desde celda del jugador: {playerCell}");
            mapGenerator.ShowPartialPathFrom(playerCell);
        }

        private void HandleIncorrectAnswer()
        {
            if (incorrectFeedback != null) incorrectFeedback.SetActive(true);
            if (correctFeedback != null)   correctFeedback.SetActive(false);
        }
    }
}
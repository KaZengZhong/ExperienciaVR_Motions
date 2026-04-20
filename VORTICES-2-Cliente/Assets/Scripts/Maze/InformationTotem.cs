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
        public GameObject correctPath;
        public GameObject incorrectPath;

        private bool playerInRange = false;
        private bool answered = false;
        private Transform player;

        void Start()
        {
            if (panel != null)
                panel.SetActive(false);

            if (correctPath != null)
                correctPath.SetActive(false);
            if (incorrectPath != null)
                incorrectPath.SetActive(false);

            if (realButton != null)
                realButton.onClick.AddListener(OnRealSelected);
            if (fakeButton != null)
                fakeButton.onClick.AddListener(OnFakeSelected);

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("[Totem] No se encontró objeto con tag Player");
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

        private void ShowPanel()
        {
            if (panel != null)
            {
                panel.SetActive(true);
                if (questionText != null)
                    questionText.text = question;
                if (displayImage != null && informationImage != null)
                    displayImage.sprite = informationImage;
            }
        }

        private void HidePanel()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void OnRealSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            if (isReal)
            {
                Debug.Log("[Totem] Respuesta correcta — REAL");
                OpenCorrectPath();
            }
            else
            {
                Debug.Log("[Totem] Respuesta incorrecta — era FALSA");
                OpenIncorrectPath();
            }
        }

        public void OnFakeSelected()
        {
            if (answered) return;
            answered = true;
            HidePanel();

            if (!isReal)
            {
                Debug.Log("[Totem] Respuesta correcta — FALSA");
                OpenCorrectPath();
            }
            else
            {
                Debug.Log("[Totem] Respuesta incorrecta — era REAL");
                OpenIncorrectPath();
            }
        }

        private void OpenCorrectPath()
        {
            if (correctPath != null)
                correctPath.SetActive(true);
            if (incorrectPath != null)
                incorrectPath.SetActive(false);
        }

        private void OpenIncorrectPath()
        {
            if (incorrectPath != null)
                incorrectPath.SetActive(true);
            if (correctPath != null)
                correctPath.SetActive(false);
        }
    }
}
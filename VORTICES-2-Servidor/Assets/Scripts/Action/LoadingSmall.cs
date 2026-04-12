using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class LoadingSmall : MonoBehaviour
    {
        // Loading prompt components
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Color loadingColor;
        [SerializeField] private TextMeshProUGUI doneText;
        [SerializeField] private Color doneColor;
        [SerializeField] private TextMeshProUGUI progressText;

        // Coroutine status
        private bool switchStatusRunning;

        public void StartLoading()
        {
            if (switchStatusRunning)
            {
                StopCoroutine("SwitchStatus");
                background.color = loadingColor;
                loadingText.gameObject.SetActive(true);
                progressText.gameObject.SetActive(true);
                doneText.gameObject.SetActive(false);

            }
            else
            {
                background.gameObject.SetActive(true);
                background.color = loadingColor;
                loadingText.gameObject.SetActive(true);
                progressText.gameObject.SetActive(true);
                doneText.gameObject.SetActive(false);
            }
        }

        public void DoneLoading()
        {
            if (switchStatusRunning)
            {
                StopCoroutine("SwitchStatus");
            }
            StartCoroutine("SwitchStatus");
        }

        public IEnumerator SwitchStatus()
        {
            switchStatusRunning = true;
            background.color = doneColor;
            doneText.gameObject.SetActive(true);
            loadingText.gameObject.SetActive(false);
            yield return new WaitForSeconds(3);
            background.gameObject.SetActive(false);
            loadingText.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            doneText.gameObject.SetActive(false);
            switchStatusRunning = false;
        }

        public void UpdateText(int currentNumber, int maxNumber, string action)
        {
            float percent = (float)currentNumber / (float)maxNumber;
            progressText.text = action + " file " + currentNumber + "/" + maxNumber + " " + System.Math.Round(percent * 100, 1) + "%";
        }
    }
}

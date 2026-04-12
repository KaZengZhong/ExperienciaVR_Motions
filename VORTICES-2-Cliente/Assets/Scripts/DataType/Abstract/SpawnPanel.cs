using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

namespace Vortices
{
    public abstract class SpawnPanel : MonoBehaviour
    {
        #region Variables and properties

        // Other references
        [SerializeField] public TextMeshProUGUI alertText;

        // Panel UI Components
        [SerializeField] protected List<GameObject> uiComponents;
        [SerializeField] protected FilePathController optionFilePath;
        [SerializeField] protected OnlinePathController optionOnlinePath;

        // Send data
        protected SessionManager sessionManager;

        // Panel Properties
        public int actualComponentId { get; set; }

        // Settings
        protected float alertDuration = 5.0f;
        protected float alertFadeTime = 0.3f;

        // Coroutine
        protected bool alertCoroutineRunning;

        #endregion

        #region User Input
        // This set of functions are used so the system can take user input while changing panel components

        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }

        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = uiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            uiComponents[actualComponentId].SetActive(false);
            // Enable new component
            uiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // Block button if necessary
            BlockButton(componentId);
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());

        }
        public void ChangeVisibleComponentFade(int componentId)
        {
            StartCoroutine(ChangeComponentFade(componentId));
        }
        private IEnumerator ChangeComponentFade(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = uiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
            actualComponentId = componentId;
        }

        // Different SpawnPanels block buttons of their components differently
        public abstract void BlockButton(int componentId);

        public void RestartPanel()
        {
            // FadeOut instantly
            CanvasGroup actualCanvasGroup = uiComponents[actualComponentId].gameObject.GetComponent<CanvasGroup>();
            actualCanvasGroup.alpha = 0;
            actualCanvasGroup.blocksRaycasts = false;
            // Disable actual component
            uiComponents[actualComponentId].SetActive(false);
            // Enable new component

            uiComponents[0].SetActive(true);
            
            actualComponentId = 0;
            // FadeIn instantly new component
            CanvasGroup newCanvasGroup = uiComponents[0].gameObject.GetComponent<CanvasGroup>();
            newCanvasGroup.alpha = 1;
            newCanvasGroup.blocksRaycasts = true;
        }

        #endregion

        #region UI Alert
        protected IEnumerator SetAlert(string alertMessage)
        {
            alertCoroutineRunning = true;
            // Set message to alert
            alertText.text = alertMessage;

            // Initiate operation to change its opacity to 1 then 0
            CanvasGroup alertTextCanvasGroup = alertText.gameObject.GetComponent<CanvasGroup>();

            float timer = 0;
            while (timer <= alertFadeTime)
            {
                float newAlpha = Mathf.Lerp(0, 1, timer / alertFadeTime);
                alertTextCanvasGroup.alpha = newAlpha;

                timer += Time.deltaTime;
                yield return null;
            }
            alertTextCanvasGroup.alpha = 1;

            yield return new WaitForSeconds(alertDuration);

            timer = 0;
            while (timer <= alertFadeTime)
            {
                float newAlpha = Mathf.Lerp(1, 0, timer / alertFadeTime);
                alertTextCanvasGroup.alpha = newAlpha;

                timer += Time.deltaTime;
                yield return null;
            }
            alertTextCanvasGroup.alpha = 0;
            alertCoroutineRunning = false;
        }

        #endregion
    }
}


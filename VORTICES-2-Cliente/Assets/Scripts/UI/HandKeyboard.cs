using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

using Vuplex.WebView;

namespace Vortices
{ 
    public class HandKeyboard : MonoBehaviour
    {
        // Other references
        public GameObject keyboard;

        // Data
        private TMP_InputField inputField;

        // Operation
        private bool isOn;

        private async void Start()
        {
            // Canvas has to have main camera
            Canvas canvas = GetComponent<Canvas>();
            canvas.worldCamera = Camera.main;

            // Load Keyboard
            CanvasKeyboard keyboard = CanvasKeyboard.Instantiate();
            this.keyboard = keyboard.gameObject;
            keyboard.transform.SetParent(transform, false);
            keyboard.transform.localEulerAngles = new Vector3(0, -45.0f, 0);
            keyboard.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var rectTransformKeyboard = keyboard.transform as RectTransform;
            rectTransformKeyboard.anchoredPosition3D = new Vector3(0, 75.0f, 0);
            await keyboard.WaitUntilInitialized();
            // Keyboard will react to InputFields
            keyboard.KeyPressed += (sender, eventArgs) => {
                var key = eventArgs.Value;
                if (key == "Backspace")
                {
                    inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
                }
                else if (key.Length == 1)
                {
                    inputField.text += key;
                }
            };
            keyboard.gameObject.SetActive(false);
        }

        public void SetInputField(TMP_InputField inputField)
        {
            TMP_InputField oldInputField = this.inputField;
            if (this.inputField != inputField)
            {
                this.inputField = inputField;
                if (oldInputField == null)
                {
                    StartCoroutine(SetKeyboardOn());
                }
            }
        }

        public void RemoveInputField()
        {
            this.inputField = null;
            StartCoroutine(SetKeyboardOff());
        }

        public IEnumerator SetKeyboardOn()
        {
            if (!isOn)
            {
                isOn = true;
                keyboard.gameObject.SetActive(true);
                Fade keyboardFader = GetComponent<Fade>();
                yield return StartCoroutine(keyboardFader.FadeInCoroutine());

            }
        }
        public IEnumerator SetKeyboardOff()
        {
            if(isOn)
            {
                isOn = false;
                Fade keyboardFader = GetComponent<Fade>();
                yield return StartCoroutine(keyboardFader.FadeOutCoroutine());
                keyboard.gameObject.SetActive(false);
            }
        }

        public void OnExitHoverKeyboardOff(HoverExitEventArgs args)
        {
            this.inputField = null;
            StartCoroutine(SetKeyboardOff());
        }
    }
}

using TMPro;
using UnityEngine;

namespace Vortices
{
    public class TextInputField : MonoBehaviour
    {
        [SerializeField] public TMP_InputField inputfield;

        [SerializeField] public TextMeshProUGUI placeholder;
        [SerializeField] public TextMeshProUGUI text;

        private string placeholderText;

        private void Start()
        {
            placeholderText = placeholder.text;
            text.text = "";

            // If there is a virtual keyboard, it will subscribe to it
            GameObject keyboard = GameObject.Find("Keyboard Canvas");
            if (keyboard != null)
            {
                inputfield.onSelect.AddListener(delegate { keyboard.GetComponent<HandKeyboard>().SetInputField(inputfield); });
            }
        }

        public string GetData()
        {
            return inputfield.text;
        }
        public int GetDataInt()
        {
            try
            {
                return int.Parse(inputfield.text);
            }
            catch
            {
                return 0;
            }
        
        }

        public void SetText(string text)
        {
            inputfield.text = text;
        }

        public void ClearPlaceholderText()
        {
            placeholder.text = "";
        }

        public void RestorePlaceholderText()
        {
            placeholder.text = placeholderText;
        }
    }
}

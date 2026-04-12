using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using System.Linq;
using UnityEngine.UI;


namespace Vortices
{
    public class UISession : MonoBehaviour
    {
        // Other references
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] private SessionController sessionController;

        [SerializeField] private SessionRemoveButton removeButton;
        [SerializeField] private Toggle selectToggle;

        // Data variables
        public string sessionName;

        #region Data Operation

        public void Init(string name, SessionController sessionController)
        {
            string newName = name.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            nameText.text = newName;
            sessionName = newName;

            this.sessionController = sessionController;
            selectToggle.onValueChanged.AddListener(delegate {
                sessionController.UnlockContinueButton();
            });
            transform.parent.GetComponent<ToggleGroup2>().ribbonToggles.Add(selectToggle);
            selectToggle.onValueChanged.AddListener(delegate
            {
                transform.parent.GetComponent<ToggleGroup2>().SelectRibbonButton(selectToggle);
            });
            removeButton.GetComponent<Button>().onClick.AddListener(delegate {
                sessionController.UnlockContinueButton();
            });

            removeButton.session = this;
            removeButton.controller = this.sessionController;
        }

        public void SelectedToggle()
        {
            // If selected, configure the SessionManager

            Debug.Log("I changed session manager session name to: " + sessionName);
            sessionController.selectedSession = sessionName;
        }

        public void DestroySession()
        {
            transform.SetParent(null);

            Destroy(gameObject);
        }

        #endregion

    }
}



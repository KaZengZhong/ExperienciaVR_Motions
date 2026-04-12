using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortices;

public class UIEnvironment : MonoBehaviour
{
    // Components
    [SerializeField] private Toggle environmentToggle;

    // Data
    int environmentId;

    #region Initialize

    public void Initialize(int environmentId, SessionController sessionController, EnvironmentObject environmentObject)
    {
        this.environmentId = environmentId;

        environmentToggle.isOn = false;
        environmentToggle.interactable = true;
        sessionController.environmentScrollviewContent.GetComponent<ToggleGroup2>().ribbonToggles.Add(environmentToggle);
        environmentToggle.GetComponentInChildren<TextMeshProUGUI>().text = environmentObject.environmentName;
        environmentToggle.onValueChanged.AddListener(delegate { sessionController.environmentScrollviewContent.GetComponent<ToggleGroup2>().SelectRibbonButton(environmentToggle); });
        environmentToggle.onValueChanged.AddListener(delegate { sessionController.SetEnvironment(environmentToggle, this.environmentId); });
    }

    #endregion
}

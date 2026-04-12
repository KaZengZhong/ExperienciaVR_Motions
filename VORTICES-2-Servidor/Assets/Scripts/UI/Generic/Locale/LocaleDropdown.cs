using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocaleDropdown : MonoBehaviour
{
    // Components
    [SerializeField] public LocaleText localeText;
    [SerializeField] public LocaleOptionDropdown dropdown;
    [SerializeField] public TextMeshProUGUI dropdownText;

    public void SetText(string text)
    {
        dropdownText.text = text;
    }
}

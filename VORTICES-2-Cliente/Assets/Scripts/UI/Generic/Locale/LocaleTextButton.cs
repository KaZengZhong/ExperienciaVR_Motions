using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocaleTextButton : MonoBehaviour
{
    // Components
    [SerializeField] public LocaleText localeText;
    [SerializeField] public Button button;
    [SerializeField] public TextMeshProUGUI buttonText;

    public void SetText(string text)
    {
         buttonText.text = text;
    }
}

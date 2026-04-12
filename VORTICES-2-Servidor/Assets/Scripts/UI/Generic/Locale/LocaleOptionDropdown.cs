using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LocaleOptionDropdown : MonoBehaviour
{
    // Components
    [SerializeField] public TMP_Dropdown dropdown;

    // Settings
    public string groupKey;
    public string stringKey;

    // Data
    public List<string> originalOptions;

    public void FillDropdown(List<string> options, string contentType)
    {
        dropdown.interactable = false;

        dropdown.options.Clear();
        originalOptions.Clear();

        SetFirstOptionLocale();

        // Get current options
        foreach (string option in options)
        {
            originalOptions.Add(option);
        }

        List<TMP_Dropdown.OptionData> optionData = dropdown.options;
        // Add options
        foreach (string option in options)
        {
            if (contentType == "Input")
            {
                optionData.Add(new TMP_Dropdown.OptionData(InputFormat(option)));
            }
            else
            {
                optionData.Add(new TMP_Dropdown.OptionData(option));
            }

        }

        if (optionData.Count > 1)
        {
            dropdown.interactable = true;
        }
    }

    private void SetFirstOptionLocale()
    {
        // Get current options
        List<TMP_Dropdown.OptionData> optionData = dropdown.options;
        // Create locale option
        optionData.Add(new TMP_Dropdown.OptionData(LocalizationController.instance.FetchString(groupKey,stringKey)));
        originalOptions.Add(LocalizationController.instance.FetchString(groupKey, stringKey));
    }

    public void SetFirstOptionString(string firstOption)
    {
        // Get current options
        dropdown.captionText.text = InputFormat(firstOption);

    }

    private string InputFormat(string input)
    {
        if (input == "Default")
        {
            return input;
        }

        string betterVisualOption = input.Replace(@"\s{2,}", "");
        string[] optionParts = betterVisualOption.Split(new string[] { "/" }, StringSplitOptions.None);
        if (optionParts[0].Length > 4)
        {
            optionParts[0] = optionParts[0].Substring(0, 4) + "...";
        }
        betterVisualOption = optionParts[0] + "/" + string.Join("/", optionParts, 1, optionParts.Length - 1);

        return betterVisualOption;
    }
}

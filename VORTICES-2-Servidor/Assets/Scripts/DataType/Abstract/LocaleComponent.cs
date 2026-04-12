using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Component subscribes to event that changes its text when the language is changed
public class LocaleComponent : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        // On language change
        LocalizationController.OnLanguageChange += OnLanguageChangeHandler;
        // On enable
        StartCoroutine(OnLanguageChangeHandlerCoroutine());
    }

    protected virtual void OnDisable()
    {
       LocalizationController.OnLanguageChange -= OnLanguageChangeHandler;
    }

    protected virtual void OnLanguageChangeHandler()
    {
        // Execute Update text after LocalizationController is enabled
        StartCoroutine(OnLanguageChangeHandlerCoroutine());
    }

    protected IEnumerator OnLanguageChangeHandlerCoroutine()
    {
        while (LocalizationController.instance == null)
        {
            yield return null;
        }

        while (!LocalizationController.instance.initialized)
        {
            yield return null;
        }

        UpdateText();
    } 

    

    public virtual void UpdateText()
    {
        // To be implemented in the derived classes
    }
}

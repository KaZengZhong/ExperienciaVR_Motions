using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Displays errors in case something goes wrong
public class ErrorController : MonoBehaviour
{
    // Components
    public TextMeshProUGUI errorBox;
    public TextMeshProUGUI loadingBox;

    // Settings
    private float alertFadeTime = 0.3f;

    // Coroutine
    private bool alertCoroutineRunning;
    Coroutine loadingCoroutine;

    public static ErrorController instance;
    public void Initialize()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowError(string message, int duration)
    {
        if(!alertCoroutineRunning) 
        {
            StartCoroutine(SetAlert(message, duration, errorBox));
        }
    }

    public void ShowLoading(bool enable)
    {
        if (enable)
        {
            loadingCoroutine = StartCoroutine(SetAlert(LocalizationController.instance.FetchString("baseStrings", "loading"), 100000, loadingBox));
        }
        else
        {
            loadingBox.GetComponent<CanvasGroup>().alpha = 0;
            StopCoroutine(loadingCoroutine);
            alertCoroutineRunning = false;
        }
    }

    #region UI Alert

    private IEnumerator SetAlert(string alertMessage, float alertDuration, TextMeshProUGUI dialog)
    {
        alertCoroutineRunning = true;
        
        if(alertDuration > 300)
        {
            alertCoroutineRunning = false;
        }

        // Set message to alert
        dialog.SetText(alertMessage);

        // Initiate operation to change its opacity to 1 then 0
        CanvasGroup alertTextCanvasGroup = dialog.gameObject.GetComponent<CanvasGroup>();

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

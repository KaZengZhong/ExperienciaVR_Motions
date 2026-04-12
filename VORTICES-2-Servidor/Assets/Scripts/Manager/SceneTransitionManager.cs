using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vuplex.WebView;
using UnityEditor;
using UnityEngine.InputSystem.XInput;

namespace Vortices
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public FadeScreen fadeScreen;
        private float blackScreenDuration = 2.0f;
        public string sceneName;

        public bool returnToMain;

        public static SceneTransitionManager instance;

        private void Start()
        {
            // Instance initializing
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting += OnQuitting;
            #endif
        }

        public void GoToScene()
        {
            StartCoroutine(GoToSceneRoutine());
        }

        public IEnumerator GoToSceneRoutine()
        {
            fadeScreen = GameObject.FindObjectOfType<FadeScreen>();
            fadeScreen.FadeOut();

            float timer = 0;
            while(timer <= fadeScreen.fadeDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            //Launch new scene
            string sceneName = "";
            EnvironmentObject currentEnvironment = AddonsController.instance.currentEnvironmentObject;
            if (!returnToMain)
            {
                AddonsController.instance.LoadEnvironmentScene();
                sceneName = currentEnvironment.environmentName;

            }
            else
            {
                sceneName = "Main Menu";
            }
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            operation.allowSceneActivation = false;

            timer = 0;
            while(timer <= blackScreenDuration && !operation.isDone)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            operation.allowSceneActivation = true;
            if (sceneName != "Main Menu")
            {
                currentEnvironment.sceneBundle.Unload(false);
            }

        }



        public IEnumerator FadeScreenOut()
        {
            yield return StartCoroutine(fadeScreen.FadeRoutine(0, 1));
        }

        // Handle application exit
        /*private async void OnApplicationQuit()
        {
            await StandaloneWebView.TerminateBrowserProcess();
        }*/


        private void OnQuitting()
        {
            /*StartCoroutine(ClearWebData());*/
            AddonsController.instance.ClearEnvironment();
        }

        /*private IEnumerator ClearWebData()
        {
            yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
        }*/


    }
    }

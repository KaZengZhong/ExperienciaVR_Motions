using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vuplex.WebView;

namespace Vortices
{
    public abstract class MuseumSpawnBase : MonoBehaviour
    {
        // SpawnBaseMuseum Data Components
        [HideInInspector] public List<string> elementPaths;
        [HideInInspector] public string rootUrl { get; set; }
        [SerializeField] public GameObject defaultPicture;
        [SerializeField] public List<GameObject> elementList;

        // Movement variables
        protected int globalIndex;

        // Settings
        public string browsingMode { get; set; }

        // Coroutine
        protected Queue<IEnumerator> coroutineQueue;
        protected bool coordinatorWorking;
        protected bool movingOperationRunning;

        #region Spawn
        // Every base creates its set of spawn elements differently
        public abstract IEnumerator StartGenerateSpawnElements();

        #endregion

        #region Multimedia Spawn
        public IEnumerator DestroyBase()
        {
            int fadeCoroutinesRunning = 0;
            // Every element has to be alpha 0
            foreach (GameObject element in elementList)
            {
                Fade elementFader = element.GetComponent<Fade>();
                elementFader.lowerAlpha = 0;
                elementFader.upperAlpha = 1;
                TaskCoroutine fadeCoroutine = new TaskCoroutine(elementFader.FadeOutCoroutine());
                fadeCoroutine.Finished += delegate (bool manual)
                {
                    fadeCoroutinesRunning--;
                };
                fadeCoroutinesRunning++;
            }

            List<GameObject> controllers = GameObject.FindGameObjectsWithTag("Controller").ToList();
            if (controllers.Count > 0)
            {
                foreach (GameObject manager in controllers)
                {
                    Destroy(manager.gameObject);
                }
            }

            List<GameObject> externals = GameObject.FindGameObjectsWithTag("External").ToList();
            if (externals.Count > 0)
            {
                foreach (GameObject external in externals)
                {
                    Destroy(external.gameObject);
                }
            }

            // Restore old pictures
            defaultPicture.SetActive(true);
            // Fade in old pictures
            Fade defaultFader = defaultPicture.GetComponent<Fade>();
            defaultFader.lowerAlpha = 0;
            defaultFader.upperAlpha = 1;
            TaskCoroutine fadeCoroutine2 = new TaskCoroutine(defaultFader.FadeInCoroutine());
            fadeCoroutine2.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            foreach (GameObject element in elementList)
            {
                element.GetComponent<MuseumElement>().DestroyWebView();
                element.SetActive(false);
            }

            /*if (browsingMode == "Online")
            {
                yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
                //Web.ClearAllData();
            }*/

        }

        #endregion
    }
}


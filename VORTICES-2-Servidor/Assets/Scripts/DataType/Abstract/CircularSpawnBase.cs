using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;
using Vuplex.WebView;

namespace Vortices
{
    public abstract class CircularSpawnBase : MonoBehaviour
    {
        // Other references
        protected List<GameObject> groupList;

        // SpawnBase Data Components
        [HideInInspector] public List<string> elementPaths;

        // Movement variables
        protected int globalIndex;
        protected bool lastLoadForward;

        // Settings
        [HideInInspector] public Vector3Int dimension { get; set; }
        public bool volumetric { get; set; }
        public string browsingMode { get; set; }
        public string displayMode { get; set; }
        public GameObject frontGroup;
        public float afterSpawnTime;
        public float spawnCooldownX = 1.0f;
        public float spawnCooldownZ = 1.0f;
        public float timeLerp = 1f;
        public float softFadeUpperAlpha = 0.6f;
        protected float movementOffset = 0.1f;
        public string moveElementDirection;

        // Bounds
        protected LayoutGroup3D layoutGroup;
        public Vector3 centerPosition;
        public Vector4 bounds; //PRIVATE
        protected float boundOffset = 0.001f;

        // Coroutine
        protected Queue<IEnumerator> coroutineQueue;
        protected bool coordinatorWorking;
        public bool movingOperationRunning;

        // Auxiliary References
        protected SessionManager sessionManager;

        private void OnEnable()
        {
            sessionManager = GameObject.FindObjectOfType<SessionManager>();

            moveElementDirection = "";

            //Start Coroutine Coordinator
            coroutineQueue = new Queue<IEnumerator>();
            StartCoroutine(CoroutineCoordinator());
        }

        #region Group Spawn

        // Every base creates its first set of spawn groups differently
        public abstract void StartGenerateSpawnGroup();

        protected void MoveGlobalIndex(bool forwards)
        {
            if (forwards)
            {
                if (!lastLoadForward)
                {
                    globalIndex += dimension.x * dimension.y * dimension.z;
                }
                else
                {
                    globalIndex += dimension.x * dimension.y;
                }

                lastLoadForward = true;
            }
            else
            {
                if (lastLoadForward)
                {
                    globalIndex -= dimension.x * dimension.y * dimension.z;
                }
                else
                {
                    globalIndex -= dimension.x * dimension.y;
                }

                lastLoadForward = false;
            }
        }

        public IEnumerator DestroyBase()
        {
            int fadeCoroutinesRunning = 0;
            // Every group has to be alpha 0
            foreach (GameObject group in groupList)
            {
                Fade groupFader = group.GetComponent<Fade>();
                groupFader.lowerAlpha = 0;
                groupFader.upperAlpha = 1;
                TaskCoroutine fadeCoroutine = new TaskCoroutine(groupFader.FadeOutCoroutine());
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

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            /*if (browsingMode == "Online")
            {
                yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
                //Web.ClearAllData();
            }*/

            Destroy(gameObject);
        }

        #endregion

        #region Input

        public void Update()
        {
            // If spawn is done, this makes sure the cooldown applies
            if (afterSpawnTime < spawnCooldownZ)
            {
                afterSpawnTime += Time.deltaTime;
            }

            if (frontGroup != null && moveElementDirection != "")
            {
                // Execute action assigned to drag direction
                PerformAction(moveElementDirection);
            }
        }


        // Different bases do different things according to the actual dragDir (Element movement)
        public abstract void PerformAction(string moveDir);

        private IEnumerator CoroutineCoordinator()
        {
            while (true)
            {
                while (coroutineQueue.Count > 0)
                {
                    coordinatorWorking = true;
                    yield return StartCoroutine(coroutineQueue.Dequeue());
                    coordinatorWorking = false;
                }

                yield return null;

            }
        }

        #endregion

        #region Spawn Movement
        // Moves then spawns more multimedia
        protected virtual IEnumerator GroupSpawnRight()
        {
            // Different bases make multimedia go right differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnLeft()
        {
            // Different bases make multimedia go left differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPull()
        {
            // Different bases make multimedia go into the foreground differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPush()
        {
            // Different bases make multimedia go onto the background differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnUp()
        {
            // Different bases make multimedia go up differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnDown()
        {
            // Different bases make multimedia go down differently
            yield return null;
        }



        #endregion

        #region Movement
        // Only moves
        protected virtual IEnumerator GroupRight()
        {
            // Different bases make multimedia go right differently
            yield return null;
        }

        protected virtual IEnumerator GroupLeft()
        {
            // Different bases make multimedia go left differently
            yield return null;
        }

        protected virtual IEnumerator GroupPull()
        {
            // Different bases make multimedia go into the foreground differently
            yield return null;
        }

        protected virtual IEnumerator GroupPush()
        {
            // Different bases make multimedia go onto the background differently
            yield return null;
        }

        protected virtual IEnumerator GroupUp()
        {
            // Different bases make multimedia go up differently
            yield return null;
        }

        protected virtual IEnumerator GroupDown()
        {
            // Different bases make multimedia go down differently
            yield return null;
        }


        protected void LogMovement(string movementDir)
        {
            sessionManager.loggingController.LogMovement(movementDir);
        }

        #endregion
    }
}

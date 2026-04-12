using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Vortices
{
    public abstract class SpawnGroup : MonoBehaviour
    {
        // Other references
        protected LayoutGroup3D layoutGroup;
        protected List<GameObject> rowList;

        // Data variables
        [HideInInspector] public List<string> elementPaths;

        // Utility
        public int globalIndex;
        protected bool lastLoadForward;
        protected List<string> loadPaths;
        protected List<GameObject> unloadObjects;
        protected List<GameObject> loadObjects;
        protected Fade groupFader;
        private int spawnedHandlingCoroutinesRunning;

        // Settings
        [HideInInspector] public Vector3Int dimension { get; set; }
        public string browsingMode { get; set; }
        public string displayMode { get; set; }
        public float softFadeUpperAlpha { get; set; }
  

        // Auxiliary Task Class
        public GameObject renderController;

        #region Multimedia Spawn
        public IEnumerator StartSpawnOperation(int offsetGlobalIndex, bool softFadeIn)
        {
            // Startup
            globalIndex = offsetGlobalIndex;
            lastLoadForward = true;
            rowList = new List<GameObject>();
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            loadObjects = new List<GameObject>();
            groupFader = GetComponent<Fade>();

            int startingLoad = dimension.x * dimension.y;

            // Execution
            yield return StartCoroutine(ObjectSpawn(0, startingLoad, true, softFadeIn));
        }

        // Spawns files using overriden GenerateExitObjects and GenerateEnterObjects
        protected IEnumerator ObjectSpawn(int unloadNumber, int loadNumber, bool forwards, bool softFade)
        {
            ObjectPreparing(unloadNumber, loadNumber, forwards);
            yield return StartCoroutine(DestroyObjectHandling());
            yield return StartCoroutine(ObjectLoad(loadNumber, forwards));
            yield return StartCoroutine(SpawnedObjectHandling(softFade));
        }
    
        // Destroys placement objects not needed and insert new ones at the same time
        protected void ObjectPreparing(int unloadNumber, int loadNumber, bool forwards)
        {
            // Generate list of child objects to leave the scene
            GenerateExitObjects(unloadNumber, forwards);

            // Generate list of child objects to spawn into the scene
            GenerateEnterObjects(loadNumber, forwards);
        }

        protected IEnumerator ObjectLoad(int loadNumber, bool forwards)
        {
            // Generate selection path to get via render
            yield return StartCoroutine(GenerateLoadPaths(loadNumber, forwards));

            // Make them appear in the scene
            RenderController render = Instantiate(renderController).GetComponent<RenderController>();
            yield return StartCoroutine(render.PlaceMultimedia(loadPaths, loadObjects, browsingMode, displayMode));

            Destroy(render.gameObject);
        }

        protected IEnumerator DestroyObjectHandling()
        {
            foreach (GameObject go in unloadObjects)
            {
                Destroy(go.gameObject);
            }

            yield return null;
        }

        protected IEnumerator SpawnedObjectHandling(bool softFade)
        {
            spawnedHandlingCoroutinesRunning = 0;

            foreach (GameObject go in loadObjects)
            {
                Fade objectFader = go.GetComponent<Fade>();
                if (softFade)
                {
                    objectFader.upperAlpha = softFadeUpperAlpha;
                }
                TaskCoroutine fadeCoroutine = new TaskCoroutine(objectFader.FadeInCoroutine());
                fadeCoroutine.Finished += delegate(bool manual)
                {
                    spawnedHandlingCoroutinesRunning--;
                };
                spawnedHandlingCoroutinesRunning++;
            }

            while (spawnedHandlingCoroutinesRunning > 0)
            {
                yield return null;
            }

        }

        protected IEnumerator GenerateLoadPaths(int loadNumber, bool forwards)
        {
            int index = 0;

            if (forwards)
            {
                if (!lastLoadForward)
                {
                    
                    globalIndex += loadNumber * dimension.y - 1;
                    
                }
                lastLoadForward = true;
            }
            else
            {
                if (lastLoadForward)
                {
                    
                    globalIndex -= loadNumber * dimension.y - 1;
                    
                }
                lastLoadForward = false;
            }
            if (!forwards)
            {
                globalIndex -= loadNumber + 1;
            }

            while (index < loadNumber)
            {
                globalIndex++;
                string actualPath = "";

                actualPath = CircularList.GetElement<string>(elementPaths, globalIndex);

                // Look if that path is in memory X
                loadPaths.Add(actualPath);

                index++;
                yield return null;
            }

            if (!forwards)
            {
                globalIndex -= loadNumber - 1;
            }
        }

        public void GenerateExitObjects(int unloadNumber, bool forwards)
        {
            unloadObjects = new List<GameObject>();

            for (int i = 0; i < unloadNumber / dimension.x; i++)
            {
                if (forwards)
                {
                    unloadObjects.Add(rowList[0].gameObject);
                    rowList.RemoveAt(0);
                }
                else
                {
                    unloadObjects.Add(rowList[rowList.Count - 1].gameObject);
                    rowList.RemoveAt(rowList.Count - 1);
                }
            }
        }

        public void GenerateEnterObjects(int loadNumber, bool forwards)
        {
            loadObjects = new List<GameObject>();

            for (int i = 0; i < loadNumber / dimension.x; i++)
            {
                GameObject rowObject = BuildRow(forwards);
                for (int j = 0; j < dimension.x; j++)
                {
                    GameObject positionObject = new GameObject();
                    positionObject.AddComponent<Fade>();
                    positionObject.transform.parent = rowObject.transform;

                    loadObjects.Add(positionObject);
                }
            }

        }

        // spawnGroups build its rows differently
        protected abstract GameObject BuildRow(bool onTop);

        public IEnumerator SpawnForwards(int loadNumber, bool softFade)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, true,  softFade));

        }

        public IEnumerator SpawnBackwards(int loadNumber, bool softFade)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, false, softFade));
        }

        #endregion

        #region Movement
        public IEnumerator SwapRowsVertically(string dragDir)
        {
            // Put first row in last position
            if (dragDir == "Down")
            {
                GameObject firstRow = rowList[0];
                ListUtils.Move(rowList, 0, rowList.Count - 1);
                firstRow.transform.SetAsLastSibling();
            }
            // Put last row in first position
            else if (dragDir == "Up")
            {
                GameObject lastRow = rowList[rowList.Count - 1];
                ListUtils.Move(rowList, rowList.Count - 1, 0);
                lastRow.transform.SetAsFirstSibling();
            }
            yield return null;
        }

        public IEnumerator SwapRowsHorizontally(string dragDir)
        {
            foreach (GameObject row in rowList)
            {
                // Put first element in last position
                if (dragDir == "Left")
                {
                    GameObject firstElement = row.transform.GetChild(0).gameObject;
                    firstElement.transform.SetAsLastSibling();
                }
                // Put last element in first position
                else if (dragDir == "Right")
                {
                    GameObject lastElement = row.transform.GetChild(dimension.x - 1).gameObject;
                    lastElement.transform.SetAsFirstSibling();
                }

            }
            yield return null;
        }
        #endregion

    }
}

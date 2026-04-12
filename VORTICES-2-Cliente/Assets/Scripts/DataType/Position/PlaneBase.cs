using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;

namespace Vortices
{
    public class PlaneBase : CircularSpawnBase
    {
        // Other references
        [SerializeField] private GameObject linearRailPrefab;
        private GameObject linearRail;
        public GameObject planeGroupPrefab;

        // Movement variables
        private bool lerpToPositionRunning;

        // Auxiliary References
        private SpawnController spawnController;

        private void Update()
        {
            base.Update();
            if (movingOperationRunning == true && spawnController != null && spawnController.movingOperationRunning == false)
            {
                spawnController.movingOperationRunning = true;
            }
            else if (movingOperationRunning == false && spawnController != null &&
                     spawnController.movingOperationRunning == true)
            {
                spawnController.movingOperationRunning = false;
            }
        }

        #region Group Spawn

        public override void StartGenerateSpawnGroup()
        {
            spawnController = GameObject.FindObjectOfType<SpawnController>();
            globalIndex = -1;
            lastLoadForward = true;
            // Linear Rail Generation
            linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.PrimaryAlignment = Alignment.Min;
            railLayout.LayoutAxis = LayoutAxis3D.Z;
            // Plane Group Generation
            groupList = new List<GameObject>();
            for(int i = 0; i < dimension.z; i++)
            {
                GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
                groupList.Add(gameObject);
                PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
                spawnGroup.Init(elementPaths, dimension, browsingMode, displayMode, softFadeUpperAlpha);
                bool softFadeIn = true;
                if (i == 0)
                {
                    // Front Group Operations
                    frontGroup = gameObject;
                    softFadeIn = false;
                }
                else
                {
                    MoveGlobalIndex(true);
                }
                StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, softFadeIn));
            }
        }

        #endregion

        #region Input
        // Changed so it only spawns when pulling or pushing
        public override void PerformAction(string moveDir)
        {
            Vector3 center = frontGroup.transform.position;
            // This means the base has been pulled and will spawn inwards
            if (dimension.z > 1 && moveDir == "Pull")
            {
                if (afterSpawnTime >= spawnCooldownZ && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local")
                    {
                        spawnController.ResetElements();
                        coroutineQueue.Enqueue(GroupSpawnPull());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupPull());
                    }

                }
            }
            // This means the base has been pushed and will spawn outwards
            else if (dimension.z > 1 && moveDir == "Push")
            {
                if (afterSpawnTime >= spawnCooldownZ && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local")
                    {
                        spawnController.ResetElements();
                        coroutineQueue.Enqueue(GroupSpawnPush());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupPush());
                    }
                }
            }
            // This means the base has touched the left bound and will spawn
            else if (moveDir == "Left")
            {
                if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local" /* && ((center.x + boundOffset) > bounds.w && (center.x - boundOffset) > bounds.w)*/)
                    {
                        coroutineQueue.Enqueue(GroupLeft());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupLeft());
                    }
                }
            }
            // This means the base has touched the right bound and will spawn
            else if (moveDir == "Right")
            {
                if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local" /* && ((center.x + boundOffset) < bounds.y && (center.x - boundOffset) < bounds.y)*/)
                    {
                        coroutineQueue.Enqueue(GroupRight());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupRight());
                    }
                }
            }
            else if (moveDir == "Up")
            {
                if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local")
                    {
                        coroutineQueue.Enqueue(GroupUp());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupUp());
                    }
                }
            }
            else if (moveDir == "Down")
            {
                if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                {
                    afterSpawnTime = 0;
                    if (browsingMode == "Local")
                    {
                        coroutineQueue.Enqueue(GroupDown());
                    }
                    else if (browsingMode == "Online")
                    {
                        coroutineQueue.Enqueue(GroupDown());
                    }
                }
            }
        }

        #endregion

        #region Spawn Movement
        // Movement that involves spawning (When using circular list) on any of the dragDirs (Local Mode)
        protected override IEnumerator GroupSpawnDown()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Down");

            int movingCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = groupList[i].GetComponent<PlaneGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                TaskCoroutine spawnCoroutine = new TaskCoroutine(planeGroup.SpawnForwards(dimension.x, softFadeIn));
                spawnCoroutine.Finished += delegate (bool manual) { movingCoroutinesRunning--; };
                movingCoroutinesRunning++;
            }
            globalIndex += dimension.y;

            while (movingCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnUp()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Up");

            int movingCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = groupList[i].GetComponent<PlaneGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                TaskCoroutine spawnCoroutine = new TaskCoroutine(planeGroup.SpawnBackwards(dimension.x, softFadeIn));
                spawnCoroutine.Finished += delegate(bool manual) { movingCoroutinesRunning--; };
                movingCoroutinesRunning++;
            }

            globalIndex -= dimension.y;
            
            while (movingCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnPull()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Pull");

            // Destroy group in front
            GameObject planeInFront = groupList[0];
            groupList.Remove(planeInFront);
            Destroy(planeInFront.transform.gameObject);
            planeInFront.transform.parent = null;
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1
            Fade frontGroupFader = frontGroup.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(true);
            // Spawn group in back
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
            groupList.Add(gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.Init(elementPaths, dimension, browsingMode, displayMode, softFadeUpperAlpha);
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, true));

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }
        protected override IEnumerator GroupSpawnPush()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Push");

            // Destroy group in back
            GameObject planeInBack = groupList[groupList.Count - 1];
            groupList.Remove(planeInBack);
            planeInBack.transform.parent = null;
            Destroy(planeInBack.transform.gameObject);
            // Front group has to be fade alpha 0
            Fade frontGroupFader = groupList[0].gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(false);
            // Spawn group in front
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position - new Vector3(0, 0, 1f), transform.rotation, linearRail.transform);
            gameObject.transform.SetSiblingIndex(0);
            groupList.Insert(0, gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.Init(elementPaths, dimension, browsingMode, displayMode, softFadeUpperAlpha);
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, false));

            // Front Group Operations
            frontGroup = gameObject;

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }


        #endregion

        #region Movement
        // Movement that doesn't involve spawning so it is just group movements
        protected override IEnumerator GroupRight()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Right");

            PlaneGroup planeGroup = frontGroup.GetComponent <PlaneGroup>();
            yield return StartCoroutine(planeGroup.SwapRowsHorizontally("Right"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupLeft()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Left");

            PlaneGroup planeGroup = frontGroup.GetComponent<PlaneGroup>();
            yield return StartCoroutine(planeGroup.SwapRowsHorizontally("Left"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupPull()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Pull");

            // Bring group in front to back
            GameObject planeGroupInFront = groupList[0];
            ListUtils.Move(groupList, 0, groupList.Count - 1);
            planeGroupInFront.transform.SetAsLastSibling();
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1 and back group has to be fade alpha softfadeUpperAlpha
            Fade frontGroupFader = frontGroup.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            Fade backGroupFader = planeGroupInFront.gameObject.GetComponent<Fade>();
            backGroupFader.lowerAlpha = softFadeUpperAlpha;
            backGroupFader.upperAlpha = 1;
            fadeCoroutine = new TaskCoroutine(backGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupPush()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Push");

            // Bring group in back to front
            GameObject planeGroupInFront = groupList[groupList.Count - 1];
            ListUtils.Move(groupList, groupList.Count - 1, 0);
            planeGroupInFront.transform.SetAsFirstSibling();
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1 and back group has to be fade alpha softfadeUpperAlpha
            Fade frontGroupFader = frontGroup.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            GameObject backGroup = groupList[1];
            Fade backGroupFader = backGroup.gameObject.GetComponent<Fade>();
            backGroupFader.lowerAlpha = softFadeUpperAlpha;
            backGroupFader.upperAlpha = 1;
            fadeCoroutine = new TaskCoroutine(backGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupUp()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Up");

            PlaneGroup planeGroup = frontGroup.GetComponent<PlaneGroup>();
            yield return StartCoroutine(planeGroup.SwapRowsVertically("Up"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupDown()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Down");

            PlaneGroup planeGroup = frontGroup.GetComponent<PlaneGroup>();
            yield return StartCoroutine(planeGroup.SwapRowsVertically("Down"));
            movingOperationRunning = false;
        }


        #endregion


    }
}

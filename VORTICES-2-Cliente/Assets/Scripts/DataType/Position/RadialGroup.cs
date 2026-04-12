using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class RadialGroup : SpawnGroup
    {
        // Other references
        [SerializeField] GameObject radialRingPrefab;
        public GameObject radialRingLinearRail;

        // Utility
        private int rotationCoroutinesRunning;

        // Settings
        public float groupRadius = 2.0f;
        public float groupAngleOffset = 15.0f;
        public float rotationAngleStep = 1.0f;
        public float rotationTime = 0.5f;

        #region Movement

        public IEnumerator RotateSpawnGroup(string moveDir)
        {
            rotationCoroutinesRunning = 0;

            foreach (GameObject radialRing in rowList)
            {
                GameObject ring = radialRing.transform.GetChild(0).gameObject;
                TaskCoroutine rotateCoroutine = new TaskCoroutine(RotateRing(ring, moveDir, rotationTime, rotationAngleStep));
                rotateCoroutine.Finished += delegate (bool manual)
                {
                    rotationCoroutinesRunning--;
                };
                rotationCoroutinesRunning++;
            }

            while (rotationCoroutinesRunning > 0)
            {
                yield return null;
            }
        }

        public IEnumerator RotateRing(GameObject radialRing, string dragDir, float rotationTime, float rotationAngleStep)
        {
            LayoutGroup3D layout = radialRing.GetComponent<LayoutGroup3D>();

            float timeElapsed = 0;
            float startingAngle = layout.StartAngleOffset;
            float finalAngle = 0;
            if (dragDir == "Right")
            {
                finalAngle = layout.StartAngleOffset - rotationAngleStep;
            }
            else if (dragDir == "Left")
            {
                finalAngle = layout.StartAngleOffset + rotationAngleStep;
            }

            while (timeElapsed < rotationTime)
            {
                timeElapsed += Time.deltaTime;
                layout.StartAngleOffset = Mathf.Lerp(startingAngle, finalAngle, timeElapsed / rotationTime);
                yield return null;
            }
            groupAngleOffset = layout.StartAngleOffset;
        }

        public IEnumerator RadiusLerp(string dragDir, float radiusStep, float timeLerp)
        {
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialRing in rowList)
            {
                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(RadiusLerpRing(radialRing, dragDir, radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;
            }

            while (radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }
        }

        private IEnumerator RadiusLerpRing(GameObject radialRing, string dragDir, float radiusStep, float timeLerp)
        {
            LayoutGroup3D ringLayout = radialRing.transform.GetChild(0).GetComponent<LayoutGroup3D>();
            float timeElapsed = 0;
            float finalRadius = 0;
            if (dragDir == "Push")
            {
                finalRadius = ringLayout.Radius + radiusStep;
            }
            else if (dragDir == "Pull")
            {
                finalRadius = ringLayout.Radius - radiusStep;
            }
            while (timeElapsed < timeLerp)
            {
                timeElapsed += Time.deltaTime;
                ringLayout.Radius = Mathf.Lerp(ringLayout.Radius, finalRadius, timeElapsed / timeLerp);
                yield return null;
            }

            ringLayout.Radius = finalRadius;
            groupRadius = finalRadius;
        }

        #endregion

        #region Multimedia Spawn
        public void Init(List<string> elementPaths, Vector3Int dimension, string browsingMode, string displayMode, GameObject linearRail, float groupRadius, float groupAngleOffset,  float softFadeUpperAlpha, float rotationAngleStep)
        {
            this.elementPaths = elementPaths;
            this.dimension = dimension;
            this.browsingMode = browsingMode;
            this.displayMode = displayMode;
            this.radialRingLinearRail = linearRail;
            this.groupRadius = groupRadius;
            this.groupAngleOffset = groupAngleOffset;
            this.softFadeUpperAlpha = softFadeUpperAlpha;
            this.rotationAngleStep = rotationAngleStep;
        }

        protected override GameObject BuildRow(bool onTop)
        {
            GameObject gameObject = Instantiate(radialRingPrefab, transform.position, radialRingPrefab.transform.rotation, radialRingLinearRail.transform);
            // Radial Ring Setting
            LayoutGroup3D ringLayout = gameObject.transform.GetChild(0).GetComponent<LayoutGroup3D>();
            ringLayout.Radius = groupRadius;
            ringLayout.StartAngleOffset = groupAngleOffset;

            if (!onTop)
            {
                gameObject.transform.SetAsFirstSibling();
                rowList.Insert(0 ,gameObject);
            }
            else
            {
                rowList.Add(gameObject);
            }

            return gameObject.transform.GetChild(0).gameObject;
        }




        #endregion
    }
}


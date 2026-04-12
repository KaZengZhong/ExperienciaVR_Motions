using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Vortices
{
    // This base does not create objects, it uses premade objects and it spawns webviews into them
    public class MuseumBase : MuseumSpawnBase
    {

        #region Element Spawn

        public void Initialize(List<string> urls)
        {
            elementPaths = urls;
        }

        public override IEnumerator StartGenerateSpawnElements()
        {
            Debug.Log("Generando elementos offline/online");

            int fadeCoroutinesRunning = 0;
            GameObject scenarioGroup = GameObject.Find("Scenario");
            Transform framesObject = scenarioGroup.transform.Find("Frames");
            defaultPicture = framesObject.gameObject;

            // Fade old elements
            if (defaultPicture != null)
            {
                Debug.Log("Desvaneciendo elementos antiguos...");
                Fade defaultElementFader = defaultPicture.GetComponent<Fade>();
                defaultElementFader.lowerAlpha = 0;
                defaultElementFader.upperAlpha = 1;
                
                TaskCoroutine fadeCoroutine = new TaskCoroutine(defaultElementFader.FadeOutCoroutine());
                fadeCoroutine.Finished += delegate (bool manual)
                {
                    fadeCoroutinesRunning--;
                    Debug.Log("FadeOutCoroutine completado para defaultPicture.");
                };
                fadeCoroutinesRunning++;

                while (fadeCoroutinesRunning > 0)
                {
                    Debug.Log("Esperando a que termine el FadeOut de defaultPicture...");
                    yield return null;
                }

                defaultPicture.SetActive(false);
                Debug.Log("defaultPicture desactivada.");
                }
                else
                {
                    Debug.LogWarning("defaultPicture es null, no hay elementos antiguos para desvanecer.");
                }

                // Fill every element in the list 
                int spawnCoroutinesRunning = 0;
                int elementsSpawned = 0;

                Debug.Log($"Cantidad de elementos en elementList: {elementList.Count}");
                foreach (GameObject element in elementList)
                {

                    if (elementsSpawned >= 6)
                            {
                                Debug.Log("Se alcanzó el límite de 6 elementos generados.");
                                break;
                            }

                    Debug.Log($"Habilitando elemento: {element.name}");
                    element.SetActive(true);

                    MuseumElement elementComponent = element.GetComponent<MuseumElement>();
                    if (elementComponent == null)
                    {
                        Debug.LogError($"El elemento {element.name} no tiene el componente MuseumElement. Verifica el prefab.");
                        continue;
                    }

                    Debug.Log($"Inicializando MuseumElement con {elementPaths.Count} rutas y modo {browsingMode}.");
                    elementComponent.Init(elementPaths, browsingMode);

                    TaskCoroutine spawnCoroutine = new TaskCoroutine(elementComponent.StartSpawnOperation(globalIndex++));
                    spawnCoroutine.Finished += delegate (bool manual)
                    {
                        spawnCoroutinesRunning--;
                        Debug.Log($"SpawnCoroutine completado para elemento: {element.name}");
                    };
                    spawnCoroutinesRunning++;
                    elementsSpawned++;
                }

                while (spawnCoroutinesRunning > 0)
                {
                    Debug.Log("Esperando a que terminen todas las SpawnCoroutines...");
                    yield return null;
                }

                Debug.Log("Todos los elementos han sido generados correctamente.");
            }
        #endregion

        public MuseumElement GetMuseumElementByIndex(int globalIndex)
        {
            foreach (Transform child in transform)
            {
                var museumElement = child.GetComponent<MuseumElement>();
                if (museumElement != null && museumElement.globalIndex == globalIndex)
                {
                    return museumElement;
                }
            }
            return null;
        }
    }
}


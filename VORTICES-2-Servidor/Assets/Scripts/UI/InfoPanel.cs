using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vortices;


namespace Vortices
{
    public class InfoPanel : MonoBehaviour
    {
        // Other references
        [SerializeField] Button startButton;
        [SerializeField] Button returnButton;

        // Auxiliary References
        private SessionManager sessionManager;
        private SpawnController spawnController;


        private void Start()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            spawnController = GameObject.FindObjectOfType<SpawnController>(true);
            SessionManager.instance.inputController.RestartInputs();
            StartCoroutine(WaitForInit());
        }

        private IEnumerator WaitForInit()
        {
            yield return new WaitForSeconds(sessionManager.initializeTime + 3.0f);
            startButton.interactable = true;
            returnButton.interactable = true;
        }

        public void Spawn ()
        {
            // Start the logger
            spawnController.StartSession(false, null);
            sessionManager.loggingController.LogSessionStatus("Start");
        }

        public void Stop()
        {
            // Stop the logger
            sessionManager.loggingController.LogSessionStatus("Stop");
            StartCoroutine(spawnController.StopSession());
        }

    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.XR;
using Vortices;

public class PlayerMovement : NetworkBehaviour
{
    public Transform cameraTransform;
    private Transform xrOriginTransform;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            return; // Solo el jugador local actualiza su propia posición
        }

        StartCoroutine(WaitForMuseumBaseAndCamera());
    }

    private void Update()
    {
        if (!isLocalPlayer || cameraTransform == null)
        {
            return;
        }

        // Mueve el PlayerPrefab a la posición de la cámara
        transform.position = cameraTransform.position;

        // Asegura que el Cube esté en la posición correcta dentro del PlayerPrefab
        Transform cubeTransform = transform.Find("Cube"); // Asegúrate de que el nombre es correcto
        if (cubeTransform != null)
        {
            cubeTransform.localPosition = Vector3.zero; // Asegura que el Cube esté alineado
        }
    }


    IEnumerator WaitForMuseumBaseAndCamera()
    {
        Debug.Log("[Cliente] Esperando a que MuseumBase cargue...");

        MuseumBase localMuseumBase = null;
        while (localMuseumBase == null)
        {
            yield return null;
            localMuseumBase = FindObjectOfType<MuseumBase>();
        }

        Debug.Log("[Cliente] MuseumBase encontrado. Buscando XR Origin y cámara...");

        while (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            yield return null;
        }
        Debug.Log($"[PlayerSync] Cámara encontrada: {cameraTransform.name}");

        Debug.Log("[Cliente] XR Origin y cámara vinculados correctamente.");
    }

}


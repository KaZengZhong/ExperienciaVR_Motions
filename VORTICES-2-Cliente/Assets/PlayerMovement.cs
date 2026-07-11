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
        if (!isLocalPlayer) return;

        // El Cube del prefab tiene BoxCollider — deshabilitarlo en el jugador local
        // para que no bloquee el CharacterController de EditorMovement
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        StartCoroutine(WaitForCamera());
    }

    private void Update()
    {
        if (!isLocalPlayer || cameraTransform == null) return;

        transform.position = cameraTransform.position;
        transform.rotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
    }

    private IEnumerator WaitForCamera()
    {
        Debug.Log("[PlayerMovement] Buscando cámara principal...");
        while (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            yield return null;
        }
        Debug.Log($"[PlayerMovement] Cámara vinculada: {cameraTransform.name}");
    }

}


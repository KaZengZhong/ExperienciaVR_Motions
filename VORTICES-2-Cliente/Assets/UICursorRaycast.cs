using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UICursorRaycast : MonoBehaviour
{
    public Camera playerCamera; // Cámara que va a lanzar el rayo
    public float raycastDistance = 100f; // Distancia del rayo
    public LayerMask ignoreLayerMask; // Capas que quieres que el raycast ignore

    void Update()
    {
        // Lanza un rayo desde el centro de la pantalla ignorando las capas especificadas
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        // Aquí usamos el LayerMask para ignorar la capa de la consola
        if (Physics.Raycast(ray, out hit, raycastDistance, ~ignoreLayerMask))
        {
            // Muestra el objeto que está siendo detectado
            Debug.Log("Objeto detectado: " + hit.collider.gameObject.name);

            // Verifica si el objeto tiene un componente de UI (como un botón)
            if (hit.collider != null && hit.collider.GetComponent<IPointerClickHandler>() != null)
            {
                // Simula que el botón está seleccionado
                ExecuteEvents.Execute(hit.collider.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

                // Detecta clic con el mouse
                if (Input.GetMouseButtonDown(0))
                {
                    ExecuteEvents.Execute(hit.collider.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Vortices;

public class HandController : MonoBehaviour
{
    // Other references
    public Element selectElement;
    private RighthandTools righthandTools;

    // Input
    [SerializeField] InputActionProperty aPress;
    [SerializeField] InputActionProperty bPress;



    private void Start()
    {
        righthandTools = FindObjectOfType<RighthandTools>();

        if (righthandTools == null)
        {
            Debug.LogError("[HandController] No se encontró el script RightHandTools.");
            return;
        }

        bPress.action.started += OpenChat;
        aPress.action.started += SelectElement;

    }

    private void OnDisable()
    {
        aPress.action.started -= SelectElement;
        bPress.action.started -= OpenChat;

    }

    #region Controller Actions

    // Select Actions
    public void SelectElement(InputAction.CallbackContext context)
    {
        if(selectElement != null)
        {
            selectElement.GetComponent<Element>().SelectElement();
        }
    }

    private void OpenChat(InputAction.CallbackContext context)
    {
        Debug.Log("[HandController] Botón B presionado: Abrir/Cerrar Chat.");
        if (righthandTools != null)
        {
            righthandTools.OnChatToggleChanged(true); // Abre el chat
        }
    }

    #endregion

}

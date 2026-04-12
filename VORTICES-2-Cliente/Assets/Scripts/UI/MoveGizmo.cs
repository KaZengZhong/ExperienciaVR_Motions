using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Vortices;

public class MoveGizmo : MonoBehaviour
{
    // Other references
    [SerializeField] GameObject moveGizmoPrefab;
    static private GameObject moveGizmo;
    public CircularSpawnBase spawnBase;

    // Input
    [SerializeField] InputActionProperty gripPress;

    // Setting
    private bool triggeredGizmo;


    private void OnEnable()
    {
        gripPress.action.started += StartGizmo;
        gripPress.action.canceled += StopGizmo;

    }

    private void OnDisable()
    {
        gripPress.action.started -= StartGizmo;
        gripPress.action.canceled -= StopGizmo;

    }

    public void Initialize(CircularSpawnBase spawnBase)
    {
        // MoveGizmo needs connection to a CircularSpawnBase to function
        this.spawnBase = spawnBase;
    }

    private void StartGizmo(InputAction.CallbackContext context)
    {
        // Spawn Gizmo
        triggeredGizmo = false;
        moveGizmo = GameObject.Find("Move Gizmo(Clone)");

        // Only one gizmo at a time
        if (moveGizmo == null)
        {
            moveGizmo = Instantiate(moveGizmoPrefab, transform.position, Camera.main.transform.rotation, null);

            // Set Gizmo Filter for each hand
            if (gameObject.name == "LeftHand Controller")
            {
                moveGizmo.layer = LayerMask.NameToLayer("Interactable Left");
                foreach(Transform child in moveGizmo.transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Interactable Left");
                }
            }
            else if (gameObject.name == "RightHand Controller")
            {
                moveGizmo.layer = LayerMask.NameToLayer("Interactable Right");
                foreach (Transform child in moveGizmo.transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Interactable Right");
                }
            }
        }
    }

    private void StopGizmo(InputAction.CallbackContext context)
    {
        // Releasing the grip button will have two outcomes, if there was no direction chosen with the controller it will simply be destroyed
        // otherwise if there was a direction chosen then a stop moving elements order will be made

        // No direction selected
        if(!triggeredGizmo && moveGizmo != null)
        {
            // Destroy Gizmo if is made with this controller
            bool destroy = false;

            if ((gameObject.name == "LeftHand Controller" && moveGizmo.layer == LayerMask.NameToLayer("Interactable Left")) ||
                (gameObject.name == "RightHand Controller" && moveGizmo.layer == LayerMask.NameToLayer("Interactable Right")))
            {
                destroy = true;
            }

            if (destroy)
            {
                DestroyGizmo();
            }
        }
        // Direction selected
        else
        {
            Debug.Log("There was a direction and a stop moving element order has to be made");
            spawnBase.moveElementDirection = "";
        }
    }

    private void DestroyGizmo()
    {
        Destroy(moveGizmo.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("MovementSphere"))
        {
            return;
        }


        if ((gameObject.name == "LeftHand Controller" && moveGizmo.layer == LayerMask.NameToLayer("Interactable Left")) ||
            (gameObject.name == "RightHand Controller" && moveGizmo.layer == LayerMask.NameToLayer("Interactable Right")))
        {
            // Get the direction 
            string moveDir = "";
            switch (other.gameObject.name)
            {
                case "Left Sphere":
                    triggeredGizmo = true;
                    moveDir = "Left";
                    break;
                case "Right Sphere":
                    triggeredGizmo = true;
                    moveDir = "Right";
                    break;
                case "Up Sphere":
                    triggeredGizmo = true;
                    moveDir = "Up";
                    break;
                case "Down Sphere":
                    triggeredGizmo = true;
                    moveDir = "Down";
                    break;
                case "Forward Sphere":
                    triggeredGizmo = true;
                    moveDir = "Push";
                    break;
                case "Back Sphere":
                    triggeredGizmo = true;
                    moveDir = "Pull";
                    break;
            }

            // Destroy the gizmo
            DestroyGizmo();

            // Start the moving order
            spawnBase.moveElementDirection = moveDir;
            Debug.Log("There was a moving order with the direction: " + moveDir + " sent to the spawnbase: " + spawnBase.gameObject.name);

            triggeredGizmo = true;
        }
    }
}

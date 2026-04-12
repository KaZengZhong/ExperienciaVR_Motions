using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ChatManager : NetworkBehaviour
    {
        public GameObject chatCanvas; // El Canvas del Chat
        public TMP_InputField chatInputField; // El campo para escribir mensajes
        public TMP_Text chatDisplay; // Donde se mostrarán los mensajes
        
        private void Start()
        {

            // Verificar si tiene autoridad
            if (!hasAuthority)
            {
                Debug.LogWarning("El cliente no tiene autoridad sobre este ChatCanvas.");
                return;
            }

            Debug.Log("El cliente tiene autoridad sobre el ChatCanvas.");
            }

        public void ToggleChat()
        {
            if (chatCanvas == null)
            {
                Debug.LogError("ChatCanvas no está asignado en el ChatManager.");
                return;
            }

            // Alternar visibilidad del ChatCanvas
            chatCanvas.SetActive(!chatCanvas.activeSelf);

            Debug.Log($"ChatCanvas ahora está {(chatCanvas.activeSelf ? "activado" : "desactivado")}.");

            // Si se activa, posicionarlo frente al jugador
            if (chatCanvas.activeSelf)
            {
                // Obtén la cámara principal
                Camera mainCamera = Camera.main;

                if (mainCamera != null)
                {
                    // Posiciona el canvas frente a la cámara
                    chatCanvas.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2.0f;

                    // Orienta el canvas hacia la cámara
                    chatCanvas.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward);

                    Debug.Log("ChatCanvas reposicionado frente al jugador.");
                }
                else
                {
                    Debug.LogError("No se encontró la cámara principal.");
                }
            }
        }



        [Command]
        void CmdSendMessage(string message)
        {
            if (!hasAuthority)
            {
                Debug.LogWarning("El cliente no tiene autoridad para enviar mensajes desde este objeto.");
                return;
            }

            ServerReceiveMessage($"{connectionToClient.connectionId}: {message}");
        }


        [Server]
        void ServerReceiveMessage(string message)
        {
            RpcReceiveMessage(message); // Propaga el mensaje a todos los clientes
        }

        [ClientRpc]
        void RpcReceiveMessage(string message)
        {
            chatDisplay.text += $"{message}\n";
        }

        public void OnSendButtonPressed()
        {
            if (!string.IsNullOrEmpty(chatInputField.text))
            {
                CmdSendMessage(chatInputField.text); // Envía el mensaje al servidor
                chatInputField.text = ""; // Limpia el campo de entrada
            }
        }
    }

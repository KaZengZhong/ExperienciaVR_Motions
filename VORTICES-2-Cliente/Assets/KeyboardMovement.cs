using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f; // Añadir el control de rotación en Y

    void Start()
    {
        // Bloquear el cursor en el centro de la pantalla y hacerlo invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Movimiento con teclado WASD
        float moveHorizontal = Input.GetAxis("Horizontal"); // A y D
        float moveVertical = Input.GetAxis("Vertical"); // W y S

        Vector3 move = transform.right * moveHorizontal + transform.forward * moveVertical;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Rotación con el mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Control de la rotación vertical
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Limitar la rotación vertical

        // Control de la rotación horizontal
        rotationY += mouseX;

        // Aplicar las rotaciones
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f); // Rotación en ambos ejes
    }
}

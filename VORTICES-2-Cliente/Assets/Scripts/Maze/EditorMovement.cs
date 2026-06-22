using UnityEngine;

namespace Vortices
{
    public class EditorMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 2f;

        private float rotY = 0f;
        private CharacterController cc;
        private bool browserMode = false;
        private float verticalVelocity = 0f;
        private const float gravity = 20f;

        void Start()
        {
            cc = GetComponentInParent<CharacterController>();
            rotY = transform.parent.eulerAngles.y;
        }

        void Update()
        {
            // Tab — alternar entre modo movimiento y modo browser
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                browserMode = !browserMode;
                Cursor.lockState = browserMode ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible   = browserMode;
            }

            if (browserMode) return;

            if (Input.GetKey(KeyCode.Q))
            {
                rotY += Input.GetAxis("Mouse X") * lookSpeed;
                transform.parent.rotation = Quaternion.Euler(0, rotY, 0);
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 forward = transform.parent.forward;
            Vector3 right   = transform.parent.right;
            Vector3 move    = forward * v + right * h;

            // Gravedad acumulada — se detiene al tocar el suelo
            if (cc != null && cc.isGrounded)
                verticalVelocity = -2f;
            else
                verticalVelocity -= gravity * Time.deltaTime;

            if (cc != null)
            {
                cc.Move(move * moveSpeed * Time.deltaTime);
                cc.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            }
        }

        // Llamado desde TotemBrowser al abrir/cerrar el navegador
        public void SetBrowserMode(bool active)
        {
            browserMode      = active;
            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = active;
        }
    }
}
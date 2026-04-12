using UnityEngine;

namespace Vortices
{
    public class EditorMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 2f;

        private float rotY = 0f;
        private CharacterController cc;

        void Start()
        {
#if UNITY_EDITOR
            // El CharacterController está en el XR Origin (padre del padre)
            cc = GetComponentInParent<CharacterController>();
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(1))
            {
                rotY += Input.GetAxis("Mouse X") * lookSpeed;
                transform.parent.rotation = Quaternion.Euler(0, rotY, 0);
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 forward = transform.parent.forward;
            Vector3 right = transform.parent.right;
            Vector3 move = forward * v + right * h;
            move.y = -1f; // gravedad simple

            if (cc != null)
                cc.Move(move * moveSpeed * Time.deltaTime);
#endif
        }
    }
}
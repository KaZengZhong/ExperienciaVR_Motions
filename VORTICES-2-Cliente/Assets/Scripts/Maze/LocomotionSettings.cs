using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace Vortices
{
    public class LocomotionSettings : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private GameObject teleportationRay;
        [SerializeField] private float moveSpeed = 2f;

        [Header("Rotación")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float followSpeed = 1f;
        [SerializeField] private float deadZone = 0.5f;

        private bool headRotationMode = false;
        private XROrigin xrOrigin;
        private float previousCameraY;

        private void Start()
        {
            xrOrigin = FindObjectOfType<XROrigin>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (cameraTransform != null)
                previousCameraY = cameraTransform.eulerAngles.y;

            ApplySettings();
        }

        public void ApplySettings()
        {
            // Movimiento
            int movMode = PlayerPrefs.GetInt("movementMode", 0);
            bool joystick      = movMode == 0;
            bool teleportation = movMode == 1;

            var moveProviders = FindObjectsOfType<ActionBasedContinuousMoveProvider>();
            Debug.Log($"[LocomotionSettings] ContinuousMoveProviders encontrados: {moveProviders.Length}");
            foreach (var p in moveProviders)
            {
                Debug.Log($"[LocomotionSettings] Provider: {p.gameObject.name}, enabled={p.enabled}, speed={p.moveSpeed}");
                p.enabled = joystick;
                p.moveSpeed = moveSpeed;
            }

            foreach (var p in FindObjectsOfType<TeleportationProvider>())
                p.enabled = teleportation;

            if (teleportationRay != null)
                teleportationRay.SetActive(teleportation);

            // Rotación
            int rotMode = PlayerPrefs.GetInt("rotationMode", 0);
            headRotationMode = rotMode == 1;

            foreach (var p in FindObjectsOfType<ActionBasedContinuousTurnProvider>())
                p.enabled = !headRotationMode;
        }

        private void Update()
        {
            if (!headRotationMode || xrOrigin == null || cameraTransform == null) return;

            float currentCameraY = cameraTransform.eulerAngles.y;
            float delta = Mathf.DeltaAngle(previousCameraY, currentCameraY);

            if (Mathf.Abs(delta) < deadZone)
            {
                previousCameraY = currentCameraY;
                return;
            }

            float step = delta * followSpeed;
            xrOrigin.transform.RotateAround(cameraTransform.position, Vector3.up, step);

            previousCameraY = currentCameraY + step;
        }
    }
}

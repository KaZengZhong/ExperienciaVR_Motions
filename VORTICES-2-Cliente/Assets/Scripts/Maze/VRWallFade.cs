using UnityEngine;

namespace Vortices
{
    public class VRWallFade : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private FadeScreen fadeScreen;
        [SerializeField] private float checkRadius = 0.15f;
        [SerializeField] private float fadeSpeed = 8f;

        private Renderer fadeRenderer;
        private bool active = false;

        private void Start()
        {
            active = PlayerPrefs.GetInt("movementMode", 0) == 2;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (fadeScreen != null)
                fadeRenderer = fadeScreen.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (!active || cameraTransform == null || fadeRenderer == null) return;

            bool insideWall = IsInsideWall();
            float target = insideWall ? 1f : 0f;

            Color c = fadeRenderer.material.GetColor("_Color");
            c.a = Mathf.MoveTowards(c.a, target, fadeSpeed * Time.deltaTime);
            fadeRenderer.material.SetColor("_Color", c);
        }

        private bool IsInsideWall()
        {
            Collider[] hits = Physics.OverlapSphere(cameraTransform.position, checkRadius);
            foreach (var hit in hits)
            {
                string n = hit.gameObject.name;
                if (n.StartsWith("Wall") || n.StartsWith("Goal") || n.StartsWith("Door"))
                    return true;
            }
            return false;
        }
    }
}

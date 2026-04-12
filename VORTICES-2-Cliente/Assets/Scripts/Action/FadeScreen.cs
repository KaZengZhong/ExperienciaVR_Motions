using System.Collections;
using UnityEngine;

namespace Vortices
{
    [RequireComponent(typeof(Renderer))]
    public class FadeScreen : MonoBehaviour
    {
        public bool fadeOnStart = true;
        public float fadeDuration = 2.0f;

        public Color fadeColor;
        private Renderer rend;
        private BoxCollider boxCollider;

        void Start()
        {
            rend = GetComponent<Renderer>();
            boxCollider = GetComponent<BoxCollider>();

            if(fadeOnStart)
            {
                FadeIn();
            }
        }

        public void FadeIn()
        {
            Fade(1, 0);
        }

        public void FadeOut()
        {
            Fade(0, 1);
        }

        public void Fade(float alphaIn, float alphaOut)
        {
            StartCoroutine(FadeRoutine(alphaIn, alphaOut));
        }

        public IEnumerator FadeRoutine(float alphaIn, float alphaOut)
        {
            float timer = 0;

            while(timer <= fadeDuration) 
            {
                Color newColor = fadeColor;
                newColor.a = Mathf.Lerp(alphaIn, alphaOut, timer / fadeDuration);
                rend.material.SetColor("_Color", newColor);

                timer += Time.deltaTime;
                yield return null;
            }

            Color finalColor = fadeColor;
            finalColor.a = alphaOut;
            rend.material.SetColor("_Color", finalColor);

            if(alphaOut == 0)
            {
                boxCollider.enabled = false;
            }
            else
            {
                boxCollider.enabled = true;
            }
        }
    }
}

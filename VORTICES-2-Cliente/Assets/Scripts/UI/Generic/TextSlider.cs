using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class TextSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public float GetData()
        {
            if (slider.value < 0.5)
            {
                return (float)System.Math.Round(0.5 + slider.value, 1);
            }
            else
            {
                return (float)System.Math.Round(slider.value * 2, 1);
            }

        }
    }
}

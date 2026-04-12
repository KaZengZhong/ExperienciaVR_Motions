using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class ToggleGroup2 : MonoBehaviour
    {
        public List<Toggle> ribbonToggles; // Add all the [toggles] in the same group to this list

        public void SelectRibbonButton(Toggle but) // Call this functin with the toggle and pass the toggle itself as the parameter
        {
            foreach (Toggle b in ribbonToggles)
            {
                if (b == but)
                {
                    b.interactable = false;
                }
                else
                {
                    b.interactable = true;
                }
            }
        }
    }
}

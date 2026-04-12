using TMPro;
using UnityEngine;

namespace Vortices
{
    public class TextDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        public int GetData()
        {
            return dropdown.value;
        }

    }
}

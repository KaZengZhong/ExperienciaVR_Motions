using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.UI;

namespace Vortices
{
    public class UIElementCategory : MonoBehaviour
    {
        // Other references
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private RighthandTools righthandTools;

        [SerializeField] private Toggle selectToggle;

        // Data variables
        public string categoryName;
        public bool changeSelection;


        #region Data Operation
        public void Init(string name, RighthandTools righthandTools)
        {
            string newName = name.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            nameText.text = newName;
            categoryName = newName;

            this.righthandTools = righthandTools;
            changeSelection = true;
        }



        public void SetToggle(bool on)
        {
            selectToggle.isOn = on;
        }

        public void SelectedToggle()
        {
            // Changing a category in this UI Component will change the element selected categories and the category controller categories
            // to save them correctly
            if (changeSelection)
            {
                if (righthandTools.elementSelectedCategories.Contains(categoryName))
                {
                    righthandTools.RemoveFromSelectedCategories(categoryName);
                }
                else
                {
                    righthandTools.AddToSelectedCategories(categoryName);
                }
            }
        }
        #endregion
    }

}

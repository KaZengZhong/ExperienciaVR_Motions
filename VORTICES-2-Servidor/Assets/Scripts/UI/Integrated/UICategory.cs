using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using System.Linq;
using UnityEngine.UI;

namespace Vortices
{
    public class UICategory : MonoBehaviour
    {
        // Other references
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] private CategorySelector categorySelector;

        [SerializeField] private CategoryRemoveButton removeButton;
        [SerializeField] public Toggle selectToggle;

        // Data variables
        public GameObject horizontalGroup; // Horizontal group gameobject where this category is present
        public string categoryName;
        public bool changeSelection;


        #region Data Operation

        public void Init(string name, CategorySelector categorySelector, GameObject horizontalGroup)
        {
            string newName = name.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            nameText.text = newName;
            categoryName = newName;
            this.horizontalGroup = horizontalGroup;

            this.categorySelector = categorySelector;
            selectToggle.onValueChanged.AddListener(delegate {
                categorySelector.UnlockContinueButton();
                });
            removeButton.GetComponent<Button>().onClick.AddListener(delegate {
                    categorySelector.UnlockContinueButton();
                });

            removeButton.category = this;
            removeButton.selector = this.categorySelector;
            changeSelection = true;
        }

        public void SelectedToggle()
        {
            if (changeSelection)
            {
                if (categorySelector.selectedCategories.Contains(categoryName))
                {
                    categorySelector.RemoveFromSelectedCategories(categoryName);
                }
                else
                {
                    categorySelector.AddToSelectedCategories(categoryName);
                }
            }
        }

        public void SetToggle(bool on)
        {
            selectToggle.isOn = on;
        }


        public void DestroyCategory()
        {
            transform.SetParent(null);
            if (horizontalGroup.transform.childCount - 1 == 0)
            {
                Destroy(horizontalGroup.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

    }
}


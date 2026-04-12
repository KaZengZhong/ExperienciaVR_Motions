using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Vortices
{
    public class CategoryRemoveButton : MonoBehaviour
    {
        public CategorySelector selector;
        public UICategory category;

        public void RemoveCategory()
        {
            selector.RemoveUICategory(category);
        }
    }
}



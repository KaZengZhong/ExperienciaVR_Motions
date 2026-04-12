using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class TextScrollView : MonoBehaviour
    {
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private Transform scrollViewContent;
        [SerializeField] private Scrollbar contentScrollBar;

        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI selectionText;


        public List<string> scrollViewPaths { get; private set; }

        public void AddPaths(List<string> stringList, int numberOfFolders)
        {
            scrollViewPaths = stringList;

            placeholderText.gameObject.SetActive(false);
            selectionText.gameObject.SetActive(true);

            string selection;
            if (stringList.Count > 1 || stringList.Count == 0)
            {
                selection = stringList.Count + " files selected from ";
            }
            else
            {
                selection = stringList.Count + " file selected from ";
            }

            if (numberOfFolders > 1)
            {
                selection += numberOfFolders + " folders.";
            }
            else
            {
                selection += "1 folder.";
            }

            selectionText.text = selection;
        }

        public void ClearPaths()
        {
            if (scrollViewPaths != null)
            {
                scrollViewPaths.Clear();
            }
        }

    }
}

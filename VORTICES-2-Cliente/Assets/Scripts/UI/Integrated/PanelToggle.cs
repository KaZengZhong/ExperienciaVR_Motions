using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class PanelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject spawnPanel;
        [SerializeField] private Toggle togglePanel;

        public void ToggleSpawnPanel()
        {
            if (!togglePanel.isOn)
            {
                spawnPanel.SetActive(true);
            }
            else
            {
                spawnPanel.SetActive(false);
            }

        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Audio;
using System.Linq;
using UnityEngine.UI;
using TMPro;

using System;

namespace Vortices
{
    public enum FrameLimit
    {
        noLimit = 0,
        limit30 = 30,
        limit60 = 60,

    }

    public class OptionsPanel : MonoBehaviour
    {
        public AudioMixer audioMixer;
        Resolution[] resolutions;
        float[] refreshRates;

        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown refreshRateDropdown;
        public TMP_Dropdown qualitySettingsDropdown;

        private void Start()
        {
            resolutions = Screen.resolutions;

            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;
            for(int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            qualitySettingsDropdown.value = QualitySettings.GetQualityLevel();
            Console.WriteLine("quality " + qualitySettingsDropdown.value);
            qualitySettingsDropdown.RefreshShownValue();

            refreshRateDropdown.ClearOptions();

            Unity.XR.Oculus.Performance.TryGetAvailableDisplayRefreshRates(out refreshRates);


            List<string> rateOptions = new List<string>();
            int currentRateIndex = 0;
            if(refreshRates.Length > 0)
            {
                for (int i = 0; i < refreshRates.Length; i++)
                {
                    string option = refreshRates[i].ToString();
                    rateOptions.Add(option);

                    float currentRate;
                    Unity.XR.Oculus.Performance.TryGetDisplayRefreshRate(out currentRate);
                    if (refreshRates[i] == currentRate)
                    {
                        currentRateIndex = i;
                    }
                }

                refreshRateDropdown.AddOptions(rateOptions);
                refreshRateDropdown.value = currentRateIndex;
                refreshRateDropdown.RefreshShownValue();
            }
            else
            {
                rateOptions.Add("Screen Rate");
                refreshRateDropdown.AddOptions(rateOptions);
                refreshRateDropdown.RefreshShownValue();
            }



   
            
        }

        public void SetResolution(int resolutionIndex)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, true);
        }

        public void SetRefreshRate(int refreshIndex)
        {
            float refreshRate = refreshRates[refreshIndex];
            Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(refreshRate);
        }

        public void SetVolume(float volume)
        {
            audioMixer.SetFloat("Volume", volume);
        }

        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
    }
}


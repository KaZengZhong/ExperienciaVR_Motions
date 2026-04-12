using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using JetBrains.Annotations;

namespace Vortices
{
    public enum Mode
    {
        UiActions = 0,
        VorticesActions = 1,
        XRHead = 2,
        XRLeftController = 3,
        XRRightController = 4,
    }

    // This is a experience input controller, it lets the launcher controller know what inputs it can map to and will execute these bindings when opened    
    public class InputController : MonoBehaviour
    {
        // Data
        private string filePath;
        private List<InputActionModeData> inputActionModeData;

        // Settings
        // This list will be filled with all input actions from the Asset, different assets means different modes
        // Note that the "first execution" has to be made to generate this input file then uploaded as a release
        public List<InputActionAsset> inputActionAssets;

        // Application Bindings
        public InputActionReference elementsPushAction;
        public InputActionReference elementsPullAction;
        public InputActionReference moveElementsAction;

        public InputActionReference selectElementAction;

        #region Initialize

        private void OnDisable()
        {
            elementsPushAction.action.performed -= HandleCustomInput;
            elementsPullAction.action.performed -= HandleCustomInput;
            moveElementsAction.action.performed -= HandleCustomInput;

            selectElementAction.action.performed -= HandleCustomInput;
        }

        public void Initialize()
        {
            // Create JSON
            filePath = Path.GetDirectoryName(Application.dataPath) + "/input_mapping.json";

            RestartInputs();
            // Create file to communicate with launcher if there is none
            SaveInputBindings();
            // Load Bindings
            LoadInputBindings();
            // Override Bindings
            OverrideBindings();
        }
        public void RestartInputs()
        {
            elementsPushAction.action.performed += HandleCustomInput;
            elementsPullAction.action.performed += HandleCustomInput;
            moveElementsAction.action.performed += HandleCustomInput;

            selectElementAction.action.performed += HandleCustomInput;
            elementsPullAction.action.Enable();
            elementsPushAction.action.Enable();
            moveElementsAction.action.Enable();
            selectElementAction.action.Enable();
        }

        #region Sending Input Binding Data to Launcher

        private List<InputActionModeData> GenerateInputActionModeData()
        {
            // Generates one InputActionMode for each control mode implemented in the application
            List<InputActionModeData> allInputActionModeData = new List<InputActionModeData>();

            // VORTICES-2 will have different input groups, head, left controller, right controller, UI and Vortices

            InputActionModeData vorticesActions = new InputActionModeData();
            vorticesActions.modeName = Enum.GetName(typeof(Mode), Mode.VorticesActions);
            List<string> vorticesActionsMaps = new List<string>{ "Vortices Actions" };
            vorticesActions.inputActions = GenerateInputActionDataFromAsset(0, vorticesActionsMaps);
            allInputActionModeData.Add(vorticesActions);

            InputActionModeData uiActions = new InputActionModeData();
            uiActions.modeName = Enum.GetName(typeof(Mode), Mode.UiActions);
            List<string> uiActionsMaps = new List<string> { "XRI UI" };
            uiActions.inputActions = GenerateInputActionDataFromAsset(0, uiActionsMaps);
            allInputActionModeData.Add(uiActions);

            InputActionModeData XRHead = new InputActionModeData();
            XRHead.modeName = Enum.GetName(typeof(Mode), Mode.XRHead);
            List<string> XRHeadMaps = new List<string> { "XRI Head" };
            XRHead.inputActions = GenerateInputActionDataFromAsset(0, XRHeadMaps);
            allInputActionModeData.Add(XRHead);

            InputActionModeData XRLeftController = new InputActionModeData();
            XRLeftController.modeName = Enum.GetName(typeof(Mode), Mode.XRLeftController);
            List<string> XRLeftControllerMaps = new List<string> { "XRI LeftHand", "XRI LeftHand Interaction", "XRI LeftHand Locomotion" };
            XRLeftController.inputActions = GenerateInputActionDataFromAsset(0, XRLeftControllerMaps);
            allInputActionModeData.Add(XRLeftController);

            InputActionModeData XRRightController = new InputActionModeData();
            XRRightController.modeName = Enum.GetName(typeof(Mode), Mode.XRRightController);
            List<string> XRRightControllerMaps = new List<string> { "XRI RightHand", "XRI RightHand Interaction", "XRI RightHand Locomotion" };
            XRRightController.inputActions = GenerateInputActionDataFromAsset(0, XRRightControllerMaps);
            allInputActionModeData.Add(XRRightController);

            return allInputActionModeData;
        }

        private List<InputActionData> GenerateInputActionDataFromAsset(int assetId, List<string> mapNames)
        {
            List<InputActionData> availableActions = new List<InputActionData>();

            // Get actions
            List<InputAction> inputActions = GetAllActionsFromAsset(assetId, mapNames);

            // Transform actions into data caring about composites
            availableActions = GetInputActionDataFromActions(inputActions);

            return availableActions;
        }


        private List<InputActionMap> GetMapsFromAsset(InputActionAsset asset)
        {
            List<InputActionMap> inputActionMaps = new List<InputActionMap>();

            if (asset == null)
            {
                Debug.LogError("InputActionAsset is not assigned.");
                return null;
            }

            foreach (InputActionMap map in asset.actionMaps)
            {
                inputActionMaps.Add(map);
            }

            return inputActionMaps;
        }

        private List<InputActionMap> GetMapsFromAsset(InputActionAsset asset, List<string> mapNames)
        {
            List<InputActionMap> inputActionMaps = new List<InputActionMap>();

            if (asset == null)
            {
                Debug.LogError("InputActionAsset is not assigned.");
                return null;
            }

            foreach (InputActionMap map in asset.actionMaps)
            {
                if (mapNames.Contains(map.name))
                {
                    inputActionMaps.Add(map);
                }
            }

            return inputActionMaps;
        }

        private List<InputAction> GetAllActionsFromMaps(List<InputActionMap> maps)
        {
            List<InputAction> inputActions = new List<InputAction>();
            
            foreach (InputActionMap map in maps)
            {
                foreach (InputAction action in map.actions)
                {
                    inputActions.Add(action);
                }
            }

            return inputActions;
        }

        private List<InputAction> GetAllActionsFromAsset(int assetId, List<string> mapNames)
        {
            List<InputAction> inputActions = new List<InputAction>();
            
            List<InputActionMap> inputActionMaps = new List<InputActionMap>();
            foreach (string names in mapNames)
            {
                inputActionMaps = GetMapsFromAsset(inputActionAssets[assetId], mapNames);
            }

            if (inputActionMaps.Count == 0)
            {
                Debug.Log("No map with names in asset selected with id: " + assetId);
                return null;
            }

            inputActions = GetAllActionsFromMaps(inputActionMaps);

            return inputActions;
        }

        private List<InputActionData> GetInputActionDataFromActions(List<InputAction> actions)
        {
            if (actions == null)
            {
                return null;
            }

            List<InputActionData> inputActionData = new List<InputActionData>();

            foreach (InputAction action in actions)
            {
                InputActionData inputAction = new InputActionData();
                inputAction.actionName = action.name;
                inputAction.actionMap = action.actionMap.name;
                inputAction.controlType = action.expectedControlType;

                List<InputBindingData> bindingData = new List<InputBindingData>();
                foreach (InputBinding originalbinding in action.bindings)
                {
                    InputBindingData newBinding = new InputBindingData();
                    newBinding.bindingName = originalbinding.name;
                    newBinding.isComposite = originalbinding.isComposite;
                    newBinding.isPartOfComposite = originalbinding.isPartOfComposite;
                    newBinding.path = "Default";

                    bindingData.Add(newBinding);
                }
                inputAction.inputBindings = bindingData;

                inputActionData.Add(inputAction);
            }

            return inputActionData;
        }

        #endregion

        #region Recieving Input Action Data from Launcher

        private void OverrideBindings()
        {
            if (inputActionModeData == null)
            {
                return;
            }

            // VORTICES-2 will have different input groups, head, left controller, right controller, UI and Vortices

            InputActionModeData vorticesActionsMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.VorticesActions));
            List<string> vorticesActionsMaps = new List<string> { "Vortices Actions" };
            List<InputAction> vorticesActionsInputActions = GetAllActionsFromAsset(0, vorticesActionsMaps);
            // Apply bindings
            OverrideModeActions(vorticesActionsInputActions, vorticesActionsMode.inputActions);

            InputActionModeData uiActionsMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.UiActions));
            List<string> uiActionsMaps = new List<string> { "XRI UI" };
            List<InputAction> uiActionsInputActions = GetAllActionsFromAsset(0, uiActionsMaps);
            // Apply bindings
            OverrideModeActions(uiActionsInputActions, uiActionsMode.inputActions);

            InputActionModeData XRHeadMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.XRHead));
            List<string> XRHeadMaps = new List<string> { "XRI Head" };
            List<InputAction> XRHeadInputActions = GetAllActionsFromAsset(0, XRHeadMaps);
            // Apply bindings
            OverrideModeActions(XRHeadInputActions, XRHeadMode.inputActions);

            InputActionModeData XRLeftControllerMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.XRLeftController));
            List<string> XRLeftControllerMaps = new List<string> { "XRI LeftHand", "XRI LeftHand Interaction", "XRI LeftHand Locomotion" };
            List<InputAction> XRLeftControllerInputActions = GetAllActionsFromAsset(0, XRLeftControllerMaps);
            // Apply bindings
            OverrideModeActions(XRLeftControllerInputActions, XRLeftControllerMode.inputActions);

            InputActionModeData XRRightControllerMode = GetModeInputActionData(Enum.GetName(typeof(Mode), (int)Mode.XRRightController));
            List<string> XRRightControllerMaps = new List<string> { "XRI RightHand", "XRI RightHand Interaction", "XRI RightHand Locomotion" };
            List<InputAction> XRRightControllerInputActions = GetAllActionsFromAsset(0, XRRightControllerMaps);
            // Apply bindings
            OverrideModeActions(XRRightControllerInputActions, XRRightControllerMode.inputActions);

        }

        private InputActionModeData GetModeInputActionData(string modeName)
        {
            foreach (InputActionModeData modeData in inputActionModeData)
            {
                if (modeData.modeName == modeName)
                {
                    return modeData;
                }
            }

            return null;
        }

        
        private void OverrideModeActions(List<InputAction> modeInputActions, List<InputActionData> overrideInputActions)
        {
            if (overrideInputActions == null)
            {
                return;
            }

            foreach (InputActionData action in overrideInputActions)
            {
                foreach (InputBindingData binding in action.inputBindings)
                {
                    if(binding.path == "Default")
                    {
                        continue;
                    }

                    // If the path is changed this means override
                    // Search the original input action
                    InputAction originalInputAction = GetInputActionFromList(modeInputActions, action);
                    // And with it, the original binding
                    InputBinding originalInputBinding = GetInputBindingFromAction(originalInputAction, binding);
                    int originalInputBindingIndex = GetBindingIndexFromAction(originalInputAction, binding);
                    if (originalInputBinding.name == "fail")
                    {
                        continue;
                    }
                    // Then Override original action
                    List<string> path = FormatBindingPath(binding.path);
                    ChangeBindingPath(originalInputAction, originalInputBinding, originalInputBindingIndex, path[0], path[1]);
                }
            }
        }
        
        private InputAction GetInputActionFromList(List<InputAction> inputActions, InputActionData inputAction)
        {
            foreach (InputAction action in inputActions)
            {
                if (action.actionMap.name == inputAction.actionMap &&
                    action.name == inputAction.actionName &&
                    action.expectedControlType == inputAction.controlType)
                {
                    return action;
                }
            }

            return null;
        }
        
        private InputBinding GetInputBindingFromAction(InputAction originalAction, InputBindingData binding)
        {
            foreach (InputBinding actionBinding in originalAction.bindings)
            {
                if (actionBinding.name == binding.bindingName &&
                    actionBinding.isComposite == binding.isComposite &&
                    actionBinding.isPartOfComposite == binding.isPartOfComposite)
                {
                    return actionBinding;
                }
            }

            return new InputBinding(name = "fail");
        }

        private int GetBindingIndexFromAction(InputAction originalAction, InputBindingData binding)
        {
            int index = -1;
            foreach (InputBinding actionBinding in originalAction.bindings)
            {
                index++;
                if (actionBinding.name == binding.bindingName &&
                    actionBinding.isComposite == binding.isComposite &&
                    actionBinding.isPartOfComposite == binding.isPartOfComposite)
                {
                    return index;
                }
            }

            return index;

        }

        private List<string> FormatBindingPath(string resultPathBinding)
        {
            List<string> result = new List<string>();

            // Split the string
            string[] pathParts = resultPathBinding.Split('/');

            if (pathParts.Length >= 2)
            {
                string device = pathParts[0];
                string path = string.Join("/", pathParts, 1, pathParts.Length - 1);

                result.Add(device);
                result.Add(path);
            }

            return result;
        }

        private void ChangeBindingPath(InputAction action, InputBinding binding, int bindingIndex , string deviceName, string bindingPath)
        {
            // ApplyBindingOverride over with its binding index
            action.ApplyBindingOverride(bindingIndex, $"<{deviceName}>/{bindingPath}");
            Debug.Log("Successfully remapped to: " + action.name + " with " + $"<{deviceName}>/{bindingPath}");
            action.performed += Test;

            //TEST OVERRIDE
            //action.started += Test;
        }

        public void Test(InputAction.CallbackContext context)
        {
            Debug.Log("La tecla fue presionada" + " activando: " + context.action.name);
        }
        #endregion


        #endregion

        #region Custom Bindings

        public void HandleCustomInput(InputAction.CallbackContext context)
        {
            // Circular base actions
            if(context.action == elementsPullAction.action ||
               context.action == elementsPushAction.action ||
               context.action == moveElementsAction.action)
            {
                if (GameObject.FindObjectOfType<CircularSpawnBase>() == null)
                {
                    Debug.Log("Input received, there is no CircularSpawnBase to move");
                    return;
                }

                CircularSpawnBase spawnBase = GameObject.FindObjectOfType<CircularSpawnBase>();

                if (context.performed)
                {
                    if (context.action == elementsPushAction.action)
                    {
                        spawnBase.PerformAction("Push");
                    }
                    else if (context.action == elementsPullAction.action)
                    {
                        spawnBase.PerformAction("Pull");
                    }
                    else if (context.action == moveElementsAction.action)
                    {
                        if (context.ReadValue<Vector2>() == new Vector2(0, 1))
                        {
                            spawnBase.PerformAction("Up");
                        }
                        else if (context.ReadValue<Vector2>() == new Vector2(0, -1))
                        {
                            spawnBase.PerformAction("Down");
                        }
                        else if (context.ReadValue<Vector2>() == new Vector2(-1, 0))
                        {
                            spawnBase.PerformAction("Left");
                        }
                        else if (context.ReadValue<Vector2>() == new Vector2(1, 0))
                        {
                            spawnBase.PerformAction("Right");
                        }
                    }
                }
            }

            if (context.action == selectElementAction.action)
            {
                if (GameObject.Find("RightHand Controller").GetComponent<HandController>() == null)
                {
                    Debug.Log("Input received, there is no HandController used to select an element");
                    return;
                }

                HandController handController = GameObject.Find("RightHand Controller").GetComponent<HandController>();

                if (context.performed)
                {
                    handController.SelectElement(context);
                }
            }
        }


        #endregion

        #region Persistence

        private void SaveInputBindings()
        {
            if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
            {
                InputActionsData actionsData = new InputActionsData();

                actionsData.allInputActionsModeData = GenerateInputActionModeData();
                string json = JsonUtility.ToJson(actionsData, true);

                File.WriteAllText(filePath, json);
            }
        }

        private void LoadInputBindings()
        {
            if (!File.Exists(filePath) || JsonChecker.IsJsonEmpty(filePath))
            {
                return;
            }
            
            string json = File.ReadAllText(filePath);

            // Load all data to overwrite

            inputActionModeData = JsonUtility.FromJson<InputActionsData>(json).allInputActionsModeData;
        }

        #endregion
    }

    #region Persistence Classes

    [Serializable]
    public class InputActionsData
    {
        public List<InputActionModeData> allInputActionsModeData;
    }

    [Serializable]
    public class InputActionModeData
    {
        public string modeName;
        public List<InputActionData> inputActions;
    }

    [Serializable]
    public class InputActionData
    {
        public string actionName;
        public string actionMap;
        public string controlType;
        public List<InputBindingData> inputBindings;
    }

    [Serializable]
    public class InputBindingData
    {
        public string bindingName;
        public bool isComposite;
        public bool isPartOfComposite;
        public string path;
    }

    #endregion
}
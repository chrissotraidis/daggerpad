using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.UI;
using NativeFilePickerNamespace;

namespace DaggerfallWorkshop.Game
{
    public class TouchscreenLayoutsManager : MonoBehaviour
    {
        private const int DaggerPadPresetVersion = 6;
        private const string DaggerPadSelectionMigrationKey = "DaggerPad_iOSDefaultSelectionVersion";

        public static TouchscreenLayoutsManager Instance{get; private set;}

        public static event System.Action<string> LayoutLoaded;

        [SerializeField] private Button importLayoutButton;
        [SerializeField] private Button exportLayoutButton;
        [SerializeField] private Button deleteLayoutButton;
        [SerializeField] private Button importTextureButton;
        [SerializeField] private Button createNewButtonButton;
        [SerializeField] private Button deleteSelectedButtonButton;
        [SerializeField] private TMPro.TMP_Dropdown layoutsDropdown;
        [SerializeField] private TMPro.TMP_InputField currentLayoutName;
        [SerializeField] private List<BuiltInSpriteConfig> builtInSprites = new List<BuiltInSpriteConfig>();
        [SerializeField] private List<BuiltInSpriteConfig> builtInKnobSprites = new List<BuiltInSpriteConfig>();

        [Header("Selected Button Options")]
        [SerializeField] private TMPro.TMP_Dropdown actionMappingDropdown;
        [SerializeField] private TMPro.TMP_Dropdown keycodeDropdown;
        [SerializeField] private TMPro.TMP_InputField buttonNameInputField;
        [SerializeField] private TMPro.TMP_Dropdown buttonTypeDropdown;
        [SerializeField] private TMPro.TMP_Dropdown anchorDropdown;
        [SerializeField] private TMPro.TMP_Dropdown labelAnchorDropdown;
        [SerializeField] private TMPro.TMP_Dropdown spriteDropdown;
        [SerializeField] private TMPro.TMP_Dropdown knobSpriteDropdown;
        [SerializeField] private Slider selectedJoystickSensitivityHorizontalSlider;
        [SerializeField] private Slider selectedJoystickSensitivityVerticalSlider;

        [Header("Assets")]
        [SerializeField] private TextAsset initialDefaultLayout;
        [SerializeField] private TextAsset initialGamepadLayout;

        private List<string> cachedSpritePaths = new List<string>();
        private List<string> cachedKnobSpritePaths = new List<string>();
        [System.Serializable]
        public struct BuiltInSpriteConfig
        {
            public string textureName;
            public string spriteName;
        }
        private readonly List<string> imageSearchPatterns = new List<string>(new string[]{"*.png", "*.jpg", "*.jpeg"});

        private Dictionary<string, int> acceptedKeyCodes = new Dictionary<string, int>();

        public static string LayoutsPath { get { return Path.Combine(Paths.PersistentDataPath, "TouchscreenLayouts"); } }
        public static string LastSelectedLayout
        {
            get
            {
#if UNITY_IOS
                return PlayerPrefs.GetString("TouchscreenLayoutsManager_LastSelectedLayout", "simplified-layout");
#else
                return PlayerPrefs.GetString("TouchscreenLayoutsManager_LastSelectedLayout", "default-layout");
#endif
            }
            set { PlayerPrefs.SetString("TouchscreenLayoutsManager_LastSelectedLayout", value); }
        }

        public TouchscreenLayoutConfiguration CurrentlyLoadedLayout {get{return currentlyLoadedLayout;}}

        private TouchscreenLayoutConfiguration currentlyLoadedLayout;
        private List<string> cachedLayoutNames = new List<string>();

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            currentLayoutName.text = "default-layout";
            importLayoutButton.onClick.AddListener(ImportNewLayout);
            exportLayoutButton.onClick.AddListener(ExportCurrentLayout);
            deleteLayoutButton.onClick.AddListener(DeleteCurrentLayout);
            importTextureButton.onClick.AddListener(ImportNewButtonTexture);
            createNewButtonButton.onClick.AddListener(CreateNewButtonInLayout);
            deleteSelectedButtonButton.onClick.AddListener(DeleteCurrentlySelectedButtonFromLayout);
            currentLayoutName.onEndEdit.AddListener(AttemptRenamingLayout);
            buttonNameInputField.onEndEdit.AddListener(AttemptRenamingButton);
            layoutsDropdown.onValueChanged.AddListener(OnLayoutsDropdownValueChanged);
            actionMappingDropdown.onValueChanged.AddListener(OnEditControlsDropdownValueChanged);
            keycodeDropdown.onValueChanged.AddListener(OnEditControlsKeyCodeDropdownValueChanged);
            buttonTypeDropdown.onValueChanged.AddListener(OnButtonTypeDropdownValueChanged);
            anchorDropdown.onValueChanged.AddListener(OnButtonAnchorDropdownValueChanged);
            labelAnchorDropdown.onValueChanged.AddListener(OnLabelAnchorDropdownValueChanged);
            selectedJoystickSensitivityHorizontalSlider.onValueChanged.AddListener(OnSelectedJoystickSensitivityHorizontalChanged);
            selectedJoystickSensitivityVerticalSlider.onValueChanged.AddListener(OnSelectedJoystickSensitivityVerticalChanged);
            SetupUI();
            TouchscreenInputManager.Instance.onCurrentlyEditingButtonChanged += SetupUIBasedOnCurrentlyEditingTouchscreenButton;
            TouchscreenInputManager.Instance.onEditControlsToggled += TouchscreenInputManager_OnEditControlsToggled;
            //Invoke("WriteCurrentLayoutToPath", 1f);
            //Invoke("ImportNewLayout", 1.5f);
        }
        private void OnDestroy()
        {
            if(TouchscreenInputManager.Instance)
                TouchscreenInputManager.Instance.onCurrentlyEditingButtonChanged -= SetupUIBasedOnCurrentlyEditingTouchscreenButton;
        }

        public void SetButtonEnabled(string buttonName, bool enabled)
        {
            int indx = currentlyLoadedLayout.buttons.FindIndex(p => p.Name == buttonName);
            if(indx >= 0){
                currentlyLoadedLayout.buttons[indx].IsEnabled = enabled;
                WriteCurrentLayoutToPath();
            }
        }

        private void ImportNewLayout()
        {
            TouchscreenInputManager.Instance.PopupMessage.Open("Pick a layout zip file.", () => {
                NativeFilePicker.FilePickedCallback filePickedCallback = new NativeFilePicker.FilePickedCallback(OnLayoutFilePicked);
                NativeFilePicker.PickFile(filePickedCallback, NativeFilePicker.ConvertExtensionToFileType("zip"));
            }, null, "Okay", "Cancel", false);
        }
        private void ImportNewButtonTexture()
        {
            NativeFilePicker.FilePickedCallback filePickedCallback = new NativeFilePicker.FilePickedCallback(OnButtonTexturePicked);
            NativeFilePicker.PickFile(
                filePickedCallback,
                NativeFilePicker.ConvertExtensionToFileType("png"),
                NativeFilePicker.ConvertExtensionToFileType("jpg"),
                NativeFilePicker.ConvertExtensionToFileType("jpeg"));
        }
        private void DeleteCurrentLayout()
        {
            bool isDefaultLayout = currentlyLoadedLayout == null || currentlyLoadedLayout.name == "default-layout" || currentlyLoadedLayout.name == "gamepad-layout";

            void DoDeleteCurrentLayout()
            {
                if(isDefaultLayout) {
                    string layoutName = currentlyLoadedLayout != null ? currentlyLoadedLayout.name : "default-layout";
                    Directory.Delete(Path.Combine(LayoutsPath, layoutName), true);
                    RegenerateDefaultLayoutIfMissing(layoutName);
                    layoutsDropdown.value = layoutName == "default-layout" ? 0 : layoutsDropdown.options.FindIndex(p => p.text == layoutName);
                    LoadLayoutByName(layoutName);
                } else {
                    Directory.Delete(Path.Combine(LayoutsPath, currentlyLoadedLayout.name), true);
                    int lastSelectedLayoutIndex = layoutsDropdown.options.FindIndex(p => p.text == currentlyLoadedLayout.name);
                    currentlyLoadedLayout = null;
                    UpdateLayoutsDropdown();
                    if(lastSelectedLayoutIndex >= 0 && lastSelectedLayoutIndex < layoutsDropdown.options.Count -1) // -1 is the Create New Layout option
                        layoutsDropdown.value = lastSelectedLayoutIndex;
                    else
                        layoutsDropdown.value = layoutsDropdown.options.Count - 2; // Select the second to last option (last real layout before Create New Layout)
                    LoadLayoutByName(layoutsDropdown.options[layoutsDropdown.value].text);
                }
            }
            if(isDefaultLayout){
                TouchscreenInputManager.Instance.PopupMessage.Open($"Delete the default layout, returning all buttons to their initial values?", DoDeleteCurrentLayout, null, "Yes", "No");
            } else {
                TouchscreenInputManager.Instance.PopupMessage.Open($"Delete the current layout, {currentlyLoadedLayout.name}?", DoDeleteCurrentLayout, null, "Yes", "No");
            }
        }
        private void ExportCurrentLayout()
        {
            // get current layout to export
            TouchscreenLayoutConfiguration exportedConfig = GetCurrentLayoutConfig();

            // create cached folder that we will zip up and export
            string cachePath = Path.Combine(Application.temporaryCachePath, "TouchscreenLayouts");
            if(Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);
            string folderToZipPath = Path.Combine(cachePath, currentlyLoadedLayout.name);
            Directory.CreateDirectory(folderToZipPath);
            Directory.CreateDirectory(Path.Combine(folderToZipPath, "textures"));

            // set default position and scale for all buttons to their current position and scale
            for(int i = 0; i < exportedConfig.buttons.Count; ++i){
                var buttonConfig = exportedConfig.buttons[i];
                buttonConfig.DefaultPosition = buttonConfig.Position;
                buttonConfig.DefaultScale = buttonConfig.Scale;
                buttonConfig.DefaultIsEnabled = buttonConfig.IsEnabled;
                buttonConfig.DefaultActionMapping = buttonConfig.ActionMapping;
                buttonConfig.DefaultKeyCodeMapping = buttonConfig.KeyCodeMapping;
                exportedConfig.buttons[i] = buttonConfig;
            }

            // copy the textures to cache
            for(int i = 0; i < exportedConfig.buttons.Count; ++i){
                if(!exportedConfig.buttons[i].UsesBuiltInTexture){
                    if(File.Exists(exportedConfig.buttons[i].TextureFilePath)){
                        string exportPath = Path.Combine(folderToZipPath, "textures", exportedConfig.buttons[i].TextureFileName);
                        File.Copy(exportedConfig.buttons[i].TextureFilePath, exportPath, true);
                    }
                }
                if(!exportedConfig.buttons[i].UsesBuiltInKnobTexture){
                    if(File.Exists(exportedConfig.buttons[i].KnobTextureFilePath)){
                        string exportPath = Path.Combine(folderToZipPath, "textures", exportedConfig.buttons[i].KnobTextureFileName);
                        File.Copy(exportedConfig.buttons[i].KnobTextureFilePath, exportPath, true);
                    }
                }
            }

            // write the json to cache
            TouchscreenLayoutConfiguration.WriteToPath(exportedConfig, Path.Combine(folderToZipPath, currentlyLoadedLayout.name + ".json"));
            
            // zip it all up
            string zipPath = folderToZipPath.TrimEnd(Path.DirectorySeparatorChar) + ".zip";
            DaggerfallWorkshop.Utility.ZipFileUtils.ZipFile(folderToZipPath, zipPath);
            Directory.Delete(folderToZipPath, true);

            // export via native file picker
            void exportFileCallback(bool success){
                if(success) {
                    Directory.Delete(cachePath, true);
                } else {
                    TouchscreenInputManager.Instance.PopupMessage.Open($"Failed to export layout via native file picker. You can find it at {zipPath}", null, null, "Okay", "", false);
                }
            }
            NativeFilePicker.ExportFile(zipPath, exportFileCallback);
        }
        private void AttemptRenamingButton(string newButtonName)
        {
            TouchscreenButton curButton = TouchscreenInputManager.Instance.CurrentlyEditingButton;
            if(string.IsNullOrEmpty(newButtonName) || newButtonName == curButton.gameObject.name)
                buttonNameInputField.text = curButton.gameObject.name;
            else if(currentlyLoadedLayout.buttons.Any(p => p.Name == newButtonName)) {
                TouchscreenInputManager.Instance.PopupMessage.Open("That button name is already being used in this layout.", null, null, "Okay", "", false);
                buttonNameInputField.text = curButton.gameObject.name;
            } else {
                string oldButtonName = curButton.gameObject.name;
                curButton.gameObject.name = newButtonName;
                int buttIndex = currentlyLoadedLayout.buttons.FindIndex(p => p.Name == oldButtonName);
                currentlyLoadedLayout.buttons[buttIndex].Name = newButtonName;

                // Update any drawers that reference the old button name
                foreach (var buttonConfig in currentlyLoadedLayout.buttons)
                {
                    if (buttonConfig.ButtonType == TouchscreenButtonType.Drawer && buttonConfig.ButtonsInDrawer != null)
                    {
                        for (int i = 0; i < buttonConfig.ButtonsInDrawer.Count; i++)
                        {
                            if (buttonConfig.ButtonsInDrawer[i] == oldButtonName)
                            {
                                buttonConfig.ButtonsInDrawer[i] = newButtonName;
                            }
                        }
                    }
                }
                WriteCurrentLayoutToPath();
            }
        }

        private string GetNextButtonName()
        {
            int highestNumber = 0;
            foreach (var button in currentlyLoadedLayout.buttons)
            {
                if (button.Name.StartsWith("new-button-"))
                {
                    string numberStr = button.Name.Substring("new-button-".Length);
                    if (int.TryParse(numberStr, out int number))
                    {
                        highestNumber = Math.Max(highestNumber, number);
                    }
                }
            }
            return "new-button-" + (highestNumber + 1).ToString();
        }
        private void CreateNewButtonInLayout()
        {
            TouchscreenButtonConfiguration newButtonConfig = new(
                GetNextButtonName(),
                new Vector2(70, -100), new Vector2(70, 70), TouchscreenButtonType.Button, true, true, "linux_buttons", "button_blank", 
                "", "", InputManager.Actions.Unknown
            );
            newButtonConfig.LayoutParentName = currentlyLoadedLayout.name;
            var newButton = TouchscreenButtonEnableDisableManager.Instance.AddButtonFromPool(newButtonConfig);
            currentlyLoadedLayout.buttons.Add(newButtonConfig);
            WriteCurrentLayoutToPath();
            TouchscreenInputManager.Instance.EditTouchscreenButton(newButton);
            newButton.ApplyConfiguration(newButtonConfig);
            newButton.IsNewlyCreated = true;
        }
        private void DeleteCurrentlySelectedButtonFromLayout()
        {
            TouchscreenButton curButton = TouchscreenInputManager.Instance.CurrentlyEditingButton;
            if(curButton)
            {
                TouchscreenInputManager.Instance.EditTouchscreenButton(null);
                TouchscreenButtonEnableDisableManager.Instance.ReturnButtonToPool(curButton);
                WriteCurrentLayoutToPath();
                LoadLayoutByName(currentLayoutName.text);
            }
        }
        private void AttemptRenamingLayout(string newLayoutName)
        {
            string oldLayoutName = currentlyLoadedLayout.name;
            if(string.IsNullOrEmpty(newLayoutName) || newLayoutName == currentlyLoadedLayout.name)
                currentLayoutName.text = oldLayoutName;
            else if(layoutsDropdown.options.Any(p => p.text == newLayoutName)) {
                TouchscreenInputManager.Instance.PopupMessage.Open("That layout name is already being used", null, null, "Okay", "", false);
                currentLayoutName.text = oldLayoutName;
            } else {
                try{
                    // Get the current state with all button positions BEFORE renaming
                    TouchscreenLayoutConfiguration currentState = GetCurrentLayoutConfig();
                    currentState.name = newLayoutName;
                    
                    // Update LayoutParentName for all buttons in the current state
                    foreach (var button in currentState.buttons)
                    {
                        button.LayoutParentName = newLayoutName;
                    }

                    // Move the directory
                    Directory.Move(Path.Combine(LayoutsPath, oldLayoutName), Path.Combine(LayoutsPath, newLayoutName));
                    
                    // Update UI elements
                    int layoutDropdownIndex = layoutsDropdown.options.FindIndex(p => p.text == oldLayoutName);
                    layoutsDropdown.options[layoutDropdownIndex].text = newLayoutName;
                    layoutsDropdown.GetComponentInChildren<TMPro.TMP_Text>().text = newLayoutName;

                    // Write the current state to the new location
                    TouchscreenLayoutConfiguration.WriteToPath(currentState, Path.Combine(LayoutsPath, newLayoutName, newLayoutName + ".json"));
                    File.Delete(Path.Combine(LayoutsPath, newLayoutName, oldLayoutName + ".json"));
                    
                    // Update our internal reference
                    currentlyLoadedLayout = currentState;
                    
                    // Regenerate default layouts if we renamed away from them
                    if (oldLayoutName == "default-layout")
                        RegenerateDefaultLayoutIfMissing("default-layout");
                    if (oldLayoutName == "gamepad-layout")
                        RegenerateDefaultLayoutIfMissing("gamepad-layout");
                    
                    UpdateLayoutsDropdown();
                    
                    // Update LastSelectedLayout to the new name
                    LastSelectedLayout = newLayoutName;

                    // Reload the layout to ensure all buttons are properly updated
                    LoadLayout(currentlyLoadedLayout);
                } catch (Exception e){
                    Debug.LogError(e);
                    TouchscreenInputManager.Instance.PopupMessage.Open($"Error renaming layout: {e}", null, null, "Okay", "", false);
                    currentLayoutName.text = oldLayoutName;
                }
            }
        }
        private bool LoadLayoutByName(string layoutName, bool showErrorPopups = false)
        {
            Debug.Log($"TouchscreenLayoutsManager: Loading {layoutName} from {LayoutsPath}");
            static string getDirName(string dirPath) => Path.GetFileName(dirPath.TrimEnd('/', '\\'));
            // get valid layouts (directories in LayoutsPath that contain a .json named same as dir)
            cachedLayoutNames = Directory.GetDirectories(LayoutsPath).Where(p => File.Exists(Path.Combine(p, getDirName(p) + ".json"))).Select(s => getDirName(s)).ToList();
            if(!cachedLayoutNames.Contains(layoutName)){
                if(showErrorPopups)
                    TouchscreenInputManager.Instance.PopupMessage.Open($"Couldn't find a layout file named {layoutName}.json within a {Path.Combine(LayoutsPath, layoutName)} directory", null, null, "Okay", "", false);
                return false;
            } else {
                try{
                    var layout = TouchscreenLayoutConfiguration.ReadFromPath(Path.Combine(LayoutsPath, layoutName, layoutName + ".json"));
                    LoadLayout(layout);
                    currentLayoutName.text = layout.name;
                    LastSelectedLayout = layout.name;
                    SelectDropdownValueForLayoutName(layout.name);
                    LoadSpriteDropdown(true);
                    LoadSpriteDropdown(false);
                    return true;
                } catch (Exception e){
                    Debug.LogError("Error loading layout: " + e);
                    Debug.LogError(e.StackTrace);
                    return false;
                }
            }
        }
        private void SelectDropdownValueForLayoutName(string layoutName)
        {
            int dropdownIndex = layoutsDropdown.options.FindIndex(p => p.text == layoutName);
            if(dropdownIndex >= 0)
                layoutsDropdown.value = dropdownIndex;
            else
                Debug.LogError("Couldn't find layouts dropdown index for " + layoutName);
        }
        public void WriteCurrentLayoutToPath() => WriteLayoutToPath(GetCurrentLayoutConfig());
        public void ReloadCurrentLayout() => LoadLayoutByName(currentlyLoadedLayout.name);
        private void WriteLayoutToPath(TouchscreenLayoutConfiguration layout)
        {
            Debug.Log($"TouchscreenLayoutsManager: Writing {layout.name} to {LayoutsPath}");
            string path = Path.Combine(LayoutsPath, layout.name, layout.name + ".json");
            TouchscreenLayoutConfiguration.WriteToPath(layout, path);
        }
        public void SetupUIBasedOnCurrentlyEditingTouchscreenButton(TouchscreenButton touchscreenButton)
        {
            if (touchscreenButton){
                actionMappingDropdown.interactable = touchscreenButton.CanActionBeEdited;
                actionMappingDropdown.value = (int)touchscreenButton.myAction;
                keycodeDropdown.interactable = touchscreenButton.CanActionBeEdited;
                keycodeDropdown.value = keycodeDropdown.options.FindIndex(p => p.text == touchscreenButton.myKey.ToString());

                var buttonConfig = touchscreenButton.GetCurrentConfiguration();
                buttonNameInputField.text = buttonConfig.Name;
                buttonTypeDropdown.value = buttonTypeDropdown.options.FindIndex(p => p.text == buttonConfig.ButtonType.ToString());
                anchorDropdown.value = anchorDropdown.options.FindIndex(p => p.text == buttonConfig.Anchor.ToString());
                labelAnchorDropdown.value = labelAnchorDropdown.options.FindIndex(p => p.text == buttonConfig.LabelAnchor.ToString());
                spriteDropdown.onValueChanged.RemoveListener(OnSpriteDropdownValueChanged);
                knobSpriteDropdown.onValueChanged.RemoveListener(OnKnobSpriteDropdownChanged);
                SyncSpriteDropdownValueToCurrentButtonSprite(true);
                SyncSpriteDropdownValueToCurrentButtonSprite(false);
                spriteDropdown.onValueChanged.AddListener(OnSpriteDropdownValueChanged);
                knobSpriteDropdown.onValueChanged.AddListener(OnKnobSpriteDropdownChanged);
                selectedJoystickSensitivityHorizontalSlider.value = buttonConfig.JoystickSensitivityHorizontal;
                selectedJoystickSensitivityVerticalSlider.value = buttonConfig.JoystickSensitivityVertical;
                //spriteDropdown.value = spriteDropdown.options.FindIndex(p => p.text == buttonConfig.LabelAnchor.ToString());
                //knobSpriteDropdown.value = knobSpriteDropdown.options.FindIndex(p => p.text == buttonConfig.LabelAnchor.ToString());

            }
        }
        private void UpdateLayoutsDropdown()
        {
            layoutsDropdown.ClearOptions();
            // get directories that have a .json under them named the same as the directory
            List<string> layoutsInPath = Directory.GetDirectories(LayoutsPath).Where(p => File.Exists(Path.Combine(p, Path.GetFileName(p.TrimEnd('/', '\\')) + ".json"))).Select(s => s.Split(Path.DirectorySeparatorChar).Last()).ToList();
            Debug.Log(layoutsInPath.Count + "Layouts in Layouts Path: " + LayoutsPath);
            layoutsDropdown.AddOptions(layoutsInPath);
            layoutsDropdown.AddOptions(new List<string> { "<Create New Layout>" });
            if(currentlyLoadedLayout != null)
                SelectDropdownValueForLayoutName(currentlyLoadedLayout.name);
        }
        private void SetupUI()
        {
            UpdateLayoutsDropdown();

            // edit controls canvas setup

            // Add button action mapping options
            actionMappingDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int i = 0; i <= (int)InputManager.Actions.Custom10; ++i)
                options.Add(((InputManager.Actions)i).ToString());
            actionMappingDropdown.AddOptions(options);

            // Add button key mapping options
            IEnumerable<int> allKeyCodes = ((KeyCode[])System.Enum.GetValues(typeof(KeyCode))).Select(s => (int)s);
            keycodeDropdown.ClearOptions();
            foreach (var key in allKeyCodes)
                if (key < (int)KeyCode.Joystick1Button0 && !InputManager.unacceptedAnyKeys.Contains(key))
                    acceptedKeyCodes[((KeyCode)key).ToString()] = key;
            keycodeDropdown.AddOptions(acceptedKeyCodes.Select(s => s.Key).ToList());

            // Add button type options
            buttonTypeDropdown.ClearOptions();
            options.Clear();
            for (int i = 0; i <= (int)TouchscreenButtonType.Drawer; ++i)
                options.Add(((TouchscreenButtonType)i).ToString());
            buttonTypeDropdown.AddOptions(options);

            // Add button anchor mapping options
            anchorDropdown.ClearOptions();
            options.Clear();
            for (int i = 0; i <= (int)TouchscreenButtonAnchor.BottomRight; ++i)
                options.Add(((TouchscreenButtonAnchor)i).ToString());
            anchorDropdown.AddOptions(options);

            // Add label anchor mapping options
            labelAnchorDropdown.ClearOptions();
            options.Clear();
            for (int i = 0; i <= (int)TouchscreenButtonAnchor.BottomRight; ++i)
                options.Add(((TouchscreenButtonAnchor)i).ToString());
            labelAnchorDropdown.AddOptions(options);

            // // Add sprite options
            // LoadSpriteDropdown(true);
            // LoadSpriteDropdown(false);
        }

        private void LoadSpriteDropdown(bool isKnob)
        {
            TMPro.TMP_Dropdown dropdown = isKnob ? knobSpriteDropdown : spriteDropdown;
            dropdown.ClearOptions();
            List<BuiltInSpriteConfig> builtIns = isKnob ? builtInKnobSprites : builtInSprites;
            cachedSpritePaths.Clear();

            Debug.Log("Loading " + currentlyLoadedLayout.name);
            string curLayoutPath = Path.Combine(LayoutsPath, currentlyLoadedLayout.name);
            var extensions = new List<string> { ".png", ".jpg", ".jpeg" };
            cachedSpritePaths.AddRange(GetImageFilesRecursive(curLayoutPath, extensions));

            dropdown.onValueChanged.RemoveListener(OnKnobSpriteDropdownChanged);
            dropdown.onValueChanged.RemoveListener(OnSpriteDropdownValueChanged);

            List<string> options = new List<string>();
            options.AddRange(builtIns.Select(s => string.IsNullOrEmpty(s.spriteName) ? s.textureName : $"{s.spriteName}"));
            options.AddRange(cachedSpritePaths.Select(s => Path.GetFileNameWithoutExtension(s)));
            dropdown.AddOptions(options);

            SyncSpriteDropdownValueToCurrentButtonSprite(isKnob);
            dropdown.onValueChanged.AddListener(isKnob ? OnKnobSpriteDropdownChanged : OnSpriteDropdownValueChanged);
        }
        private void SyncSpriteDropdownValueToCurrentButtonSprite(bool isKnob)
        {
            TMPro.TMP_Dropdown dropdown = isKnob ? knobSpriteDropdown : spriteDropdown;
            if(TouchscreenInputManager.Instance.CurrentlyEditingButton != null){
                string currentlyEditingButtonName = TouchscreenInputManager.Instance.CurrentlyEditingButton.gameObject.name;
                int curButtonIndex = currentlyLoadedLayout.buttons.FindIndex(p => p.Name == currentlyEditingButtonName);
                var curButton = currentlyLoadedLayout.buttons[curButtonIndex];
                bool useBuiltIn = isKnob ? curButton.UsesBuiltInKnobTexture : curButton.UsesBuiltInTexture;
                if(useBuiltIn){
                    dropdown.value = dropdown.options.FindIndex(p => p.text == (isKnob ? curButton.KnobSpriteName : curButton.SpriteName));
                } else {
                    string texName = isKnob ? Path.GetFileNameWithoutExtension(curButton.KnobTextureFileName) : Path.GetFileNameWithoutExtension(curButton.TextureFileName);
                    dropdown.value = dropdown.options.FindLastIndex(p => p.text == texName);
                }
            }
        }
        private void ChangeCurrentButtonSprite(int spriteOptionIndex, bool isKnob)
        {
            List<BuiltInSpriteConfig> builtInSpriteConfigs = isKnob ? builtInKnobSprites : builtInSprites;
            string currentlyEditingButtonName = TouchscreenInputManager.Instance.CurrentlyEditingButton.gameObject.name;
            int curButtonIndex = currentlyLoadedLayout.buttons.FindIndex(p => p.Name == currentlyEditingButtonName);
            var curButtonConfig = currentlyLoadedLayout.buttons[curButtonIndex];
            var buttonInstance = TouchscreenInputManager.Instance.CurrentlyEditingButton;
            var config = buttonInstance.GetCurrentConfiguration(curButtonConfig.LayoutParentName);

            if(spriteOptionIndex < builtInSpriteConfigs.Count){
                BuiltInSpriteConfig newSpriteConfig = builtInSpriteConfigs[spriteOptionIndex];
                if(isKnob){
                    config.UsesBuiltInKnobTexture = true;
                    config.KnobTextureFileName = newSpriteConfig.textureName;
                    config.KnobSpriteName = newSpriteConfig.spriteName;
                } else {
                    config.UsesBuiltInTexture = true;
                    config.TextureFileName = newSpriteConfig.textureName;
                    config.SpriteName = newSpriteConfig.spriteName;
                }
            } else {
                spriteOptionIndex -= builtInSpriteConfigs.Count;
                string newSpritePath = cachedSpritePaths[spriteOptionIndex];
                if(isKnob){
                    config.UsesBuiltInKnobTexture = false;
                    config.KnobTextureFileName = Path.GetFileName(newSpritePath);
                    config.KnobSpriteName = "";
                } else {
                    config.UsesBuiltInTexture = false;
                    config.TextureFileName = Path.GetFileName(newSpritePath);
                    config.SpriteName = "";
                }
            }
            currentlyLoadedLayout.buttons[curButtonIndex] = config;
            buttonInstance.ApplyConfiguration(config);
        }
        public void LoadLastSelectedOrDefaultLayout()
        {
            if(!Directory.Exists(LayoutsPath))
                Directory.CreateDirectory(LayoutsPath);
            RegenerateDefaultLayoutIfMissing("default-layout");
            RegenerateDefaultLayoutIfMissing("gamepad-layout");
            RegenerateDaggerPadLayoutIfMissing("simplified-layout");
            RegenerateDaggerPadLayoutIfMissing("gesture-layout");
            RegenerateDaggerPadLayoutIfMissing("accessibility-layout");
#if UNITY_IOS
            SelectSimplifiedLayoutForLegacyIpadSelection();
#endif
            UpdateLayoutsDropdown();
            if(!LoadLayoutByName(LastSelectedLayout, false)){
                if(LastSelectedLayout == "gamepad-layout")
                    RegenerateBrokenDefaultLayout("gamepad-layout");
                else if(!LoadLayoutByName("default-layout")){
                    RegenerateBrokenDefaultLayout("default-layout");
                }
            }
        }

#if UNITY_IOS
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SelectSimplifiedLayoutForLegacyIpadSelection()
        {
            if (PlayerPrefs.GetInt(DaggerPadSelectionMigrationKey, 0) >= DaggerPadPresetVersion)
                return;

            string selectedLayout = LastSelectedLayout;
            string selectedLayoutPath = Path.Combine(LayoutsPath, selectedLayout, $"{selectedLayout}.json");
            TouchscreenLayoutConfiguration layout = TouchscreenLayoutConfiguration.ReadFromPath(selectedLayoutPath);
            TouchscreenButtonConfiguration fixedJoystick = layout?.buttons?.FirstOrDefault(button => button.Name == "joystick");
            bool legacyPhoneLayout = layout != null && layout.daggerPadPresetVersion == 0 &&
                !layout.leftJoystickEnabled && !layout.rightJoystickEnabled &&
                fixedJoystick != null && fixedJoystick.IsEnabled && fixedJoystick.Scale.x >= 240f &&
                layout.buttons.Count(button => button.IsEnabled) >= 20;

            if (legacyPhoneLayout)
            {
                Debug.Log($"DaggerPad: Preserving legacy layout '{selectedLayout}' but selecting the iPad simplified preset.");
                LastSelectedLayout = "simplified-layout";
            }

            PlayerPrefs.SetInt(DaggerPadSelectionMigrationKey, DaggerPadPresetVersion);
            PlayerPrefs.Save();
        }
#endif
        public void RegenerateBrokenDefaultLayout(string layoutName = "default-layout", bool showPopup = true)
        {
            // default layout broken. Regenerate it!
            string defaultLayoutDir = Path.Combine(LayoutsPath, layoutName);
            if (Directory.Exists(defaultLayoutDir))
            {
                Directory.Delete(defaultLayoutDir, true);
            }
            // Also delete the initial-default-layout.json so it is regenerated from the TextAsset
            string initialDefaultLayoutPath = Path.Combine(Paths.PersistentDataPath, $"initial-{layoutName}.json");
            if (File.Exists(initialDefaultLayoutPath))
            {
                File.Delete(initialDefaultLayoutPath);
            }
            RegenerateDefaultLayoutIfMissing(layoutName);
            UpdateLayoutsDropdown();
            LoadLayoutByName(layoutName);
            if(showPopup)
                TouchscreenInputManager.Instance.PopupMessage.Open(
                    $"The {layoutName} was broken and has been reset to its original state.",
                    null, null, "Okay", "", false
                );
        }
        private void RegenerateDefaultLayoutIfMissing(string layoutName = "default-layout")
        {            
            string defaultLayoutPath = Path.Combine(LayoutsPath, layoutName, $"{layoutName}.json");
            if(!File.Exists(defaultLayoutPath)){
                string initialDefaultLayoutPath =Path.Combine(Paths.PersistentDataPath, $"initial-{layoutName}.json");
                if(!File.Exists(initialDefaultLayoutPath))
                    File.WriteAllText(initialDefaultLayoutPath, layoutName.Contains("default") ? initialDefaultLayout.text : initialGamepadLayout.text);
                string defaultLayoutText = File.ReadAllText(initialDefaultLayoutPath);
                Directory.CreateDirectory(Path.Combine(LayoutsPath, layoutName));
                File.WriteAllText(defaultLayoutPath, defaultLayoutText);
            }
        }

        private void RegenerateDaggerPadLayoutIfMissing(string layoutName)
        {
            string layoutDirectory = Path.Combine(LayoutsPath, layoutName);
            string layoutPath = Path.Combine(layoutDirectory, $"{layoutName}.json");
            if (File.Exists(layoutPath))
            {
                TouchscreenLayoutConfiguration existingLayout = TouchscreenLayoutConfiguration.ReadFromPath(layoutPath);
                if (existingLayout != null && existingLayout.daggerPadPresetVersion >= DaggerPadPresetVersion)
                    return;
            }

            TouchscreenLayoutConfiguration layout = TouchscreenLayoutConfiguration.Deserialize(initialDefaultLayout.text);
            if (layout == null)
                return;

            layout.name = layoutName;
            layout.daggerPadPresetVersion = DaggerPadPresetVersion;
            layout.gestureCombat = layoutName == "gesture-layout";
            layout.weaponSwingMode = layout.gestureCombat ? 0 : 1;
            bool accessibilityLayout = layoutName == "accessibility-layout";

            // DaggerPad's standard presets use the screen halves as virtual trackpads:
            // left for movement and right for look. This is easier to acquire on iPad
            // than a large fixed joystick and leaves substantially more of the game visible.
            layout.leftJoystickEnabled = true;
            layout.rightJoystickEnabled = false;
            layout.screenTapsActivateCenterObject = false;
            layout.touchscreenSensitivity = 1f;
            layout.defaultUIAlpha = accessibilityLayout ? 1f : 0.84f;

            string[] visibleControls =
            {
                "activate-center-object", "ready-weapon", "inventory", "escape",
                "enter-key", "drawer", "edit-controls",
            };
            string[] drawerControls =
            {
                "auto-map", "rest", "quick-save", "quick-load", "status", "travel-map",
                "logbook", "notebook", "switch-hand", "use-magic-item", "toggle-run",
            };
            HashSet<string> enabledButtons = new HashSet<string>(visibleControls);
            foreach (string buttonName in drawerControls)
                enabledButtons.Add(buttonName);
            if (!layout.gestureCombat)
                enabledButtons.Add("swing-weapon");

            foreach (TouchscreenButtonConfiguration button in layout.buttons)
            {
                button.DefaultIsEnabled = button.IsEnabled = enabledButtons.Contains(button.Name);
                ConfigureDaggerPadButton(button, accessibilityLayout);
            }

            TouchscreenButtonConfiguration drawer = layout.buttons.FirstOrDefault(button => button.Name == "drawer");
            if (drawer != null)
            {
                drawer.ButtonsInDrawer = drawerControls.ToList();
                drawer.IsDrawerOpen = false;
                drawer.DrawerClosedColor = new Color(0.82f, 0.82f, 0.82f, 0.95f);
            }

            Directory.CreateDirectory(layoutDirectory);
            TouchscreenLayoutConfiguration.WriteToPath(layout, layoutPath);
        }

        private static void ConfigureDaggerPadButton(TouchscreenButtonConfiguration button, bool accessibilityLayout)
        {
            ApplyDaggerPadButtonTheme(button);

            Vector2 position;
            Vector2 scale;
            TouchscreenButtonAnchor anchor;

            switch (button.Name)
            {
                case "joystick":
                    // Retained for custom layouts, but the built-in iPad presets use
                    // the invisible left screen-half movement surface instead.
                    position = new Vector2(55, 60);
                    scale = new Vector2(240, 240);
                    anchor = TouchscreenButtonAnchor.BottomLeft;
                    break;
                case "activate-center-object":
                    position = new Vector2(-30, -95);
                    scale = new Vector2(76, 76);
                    anchor = TouchscreenButtonAnchor.MiddleRight;
                    break;
                case "swing-weapon":
                    position = new Vector2(-30, -205);
                    scale = new Vector2(96, 96);
                    anchor = TouchscreenButtonAnchor.MiddleRight;
                    break;
                case "ready-weapon":
                    position = new Vector2(-30, -305);
                    scale = new Vector2(68, 68);
                    anchor = TouchscreenButtonAnchor.MiddleRight;
                    break;
                case "toggle-run":
                    position = new Vector2(108, -460);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "inventory":
                    position = new Vector2(0, 30);
                    scale = new Vector2(68, 68);
                    anchor = TouchscreenButtonAnchor.BottomMiddle;
                    break;
                case "escape":
                    position = new Vector2(74, 30);
                    scale = new Vector2(68, 68);
                    anchor = TouchscreenButtonAnchor.BottomMiddle;
                    break;
                case "enter-key":
                    position = new Vector2(-170, 30);
                    scale = new Vector2(112, 64);
                    anchor = TouchscreenButtonAnchor.BottomMiddle;
                    break;
                case "drawer":
                    position = new Vector2(-74, 30);
                    scale = new Vector2(68, 64);
                    anchor = TouchscreenButtonAnchor.BottomMiddle;
                    break;
                case "edit-controls":
                    position = new Vector2(148, 30);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.BottomMiddle;
                    break;
                case "auto-map":
                    position = new Vector2(38, -250);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "rest":
                    position = new Vector2(108, -250);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "quick-save":
                    position = new Vector2(178, -250);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "quick-load":
                    position = new Vector2(38, -320);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "status":
                    position = new Vector2(108, -320);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "travel-map":
                    position = new Vector2(178, -320);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "logbook":
                    position = new Vector2(38, -390);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "notebook":
                    position = new Vector2(108, -390);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "switch-hand":
                    position = new Vector2(178, -390);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                case "use-magic-item":
                    position = new Vector2(38, -460);
                    scale = new Vector2(64, 64);
                    anchor = TouchscreenButtonAnchor.TopLeft;
                    break;
                default:
                    return;
            }

            if (accessibilityLayout && button.Name != "joystick")
                scale *= 1.15f;

            button.Anchor = anchor;
            button.LabelAnchor = anchor == TouchscreenButtonAnchor.MiddleRight ? TouchscreenButtonAnchor.MiddleLeft :
                anchor == TouchscreenButtonAnchor.TopMiddle ? TouchscreenButtonAnchor.BottomMiddle : TouchscreenButtonAnchor.TopMiddle;
            button.DefaultPosition = button.Position = position;
            button.DefaultScale = button.Scale = scale;
            button.Text = button.Name == "enter-key" ? "ENTER" : "";
            button.TextColor = new Color(0.94f, 0.87f, 0.69f, 1f);
        }

        private static void ApplyDaggerPadButtonTheme(TouchscreenButtonConfiguration button)
        {
            string textureName = button.Name switch
            {
                "activate-center-object" => "daggerpad_use",
                "swing-weapon" => "daggerpad_attack",
                "ready-weapon" => "daggerpad_ready",
                "inventory" => "daggerpad_inventory",
                "escape" => "daggerpad_pause",
                "enter-key" => "daggerpad_button_frame",
                "drawer" => "daggerpad_more",
                "edit-controls" => "daggerpad_settings",
                "auto-map" => "daggerpad_automap",
                "rest" => "daggerpad_rest",
                "quick-save" => "daggerpad_save",
                "quick-load" => "daggerpad_load",
                "status" => "daggerpad_status",
                "travel-map" => "daggerpad_travel",
                "logbook" => "daggerpad_logbook",
                "notebook" => "daggerpad_notebook",
                "switch-hand" => "daggerpad_switch_hand",
                "use-magic-item" => "daggerpad_magic",
                "toggle-run" => "daggerpad_run",
                _ => null,
            };

            if (string.IsNullOrEmpty(textureName))
                return;

            button.UsesBuiltInTexture = true;
            button.TextureFileName = textureName;
            button.SpriteName = "";
        }

        public void LoadLayout(TouchscreenLayoutConfiguration layoutConfig)
        {
            if(currentlyLoadedLayout != null && layoutConfig.name != currentlyLoadedLayout.name)
                WriteCurrentLayoutToPath();

            TouchscreenInputManager.ClearInputState();
            TouchscreenButtonEnableDisableManager.Instance.ReturnAllButtonsToPool();
            TouchscreenInputManager.Instance.SetUIAlpha(layoutConfig.defaultUIAlpha);
            TouchscreenInputManager.Instance.SetJoystickTapsShouldActivateCenterObject(layoutConfig.screenTapsActivateCenterObject);
            TouchscreenButtonEnableDisableManager.Instance.IsLeftJoystickEnabled = layoutConfig.leftJoystickEnabled;
            TouchscreenButtonEnableDisableManager.Instance.IsRightJoystickEnabled = layoutConfig.rightJoystickEnabled;
            TouchscreenInputManager.Instance.SetTouchscreenSensitivity(layoutConfig.touchscreenSensitivity >= 0 ? layoutConfig.touchscreenSensitivity : 1.0f);
            DaggerfallUnity.Settings.GestureCombat = layoutConfig.gestureCombat;
            if (layoutConfig.weaponSwingMode >= 0)
                DaggerfallUnity.Settings.WeaponSwingMode = Mathf.Clamp(layoutConfig.weaponSwingMode, 0, 2);
            layoutConfig.buttons.Sort(new TouchscreenButtonConfigurationComparer());

            for(int i = 0; i < layoutConfig.buttons.Count; ++i)
            {
                var buttonConfig = TouchscreenButton.ConvertButtonConfigToLatestVersion(layoutConfig.buttons[i]);
                buttonConfig.LayoutParentName = layoutConfig.name;
                layoutConfig.buttons[i] = buttonConfig;
                TouchscreenButtonEnableDisableManager.Instance.AddButtonFromPool(buttonConfig);
            }
            // DaggerfallGC.ThrottledUnloadUnusedAssets();

            currentlyLoadedLayout = layoutConfig;

            Debug.Log("Loaded layout: " + layoutConfig.name);
            LayoutLoaded?.Invoke(layoutConfig.name);
#if UNITY_IOS
            if (layoutConfig.daggerPadPresetVersion >= DaggerPadPresetVersion &&
                PlayerPrefs.GetInt("DaggerPadControlHintVersion", 0) < DaggerPadPresetVersion)
            {
                DaggerfallUI.AddHUDText("LEFT: move   RIGHT: look + double-tap use", 4f);
                PlayerPrefs.SetInt("DaggerPadControlHintVersion", DaggerPadPresetVersion);
                PlayerPrefs.Save();
            }
#endif
        }
        public TouchscreenLayoutConfiguration GetCurrentLayoutConfig()
        {
            string layoutName = name = currentlyLoadedLayout != null ? currentlyLoadedLayout.name : "default-layout";
            var layout = new TouchscreenLayoutConfiguration(){
                name = layoutName,
                daggerPadPresetVersion = currentlyLoadedLayout != null ? currentlyLoadedLayout.daggerPadPresetVersion : 0,
                defaultUIAlpha = TouchscreenInputManager.Instance.UIAlpha,
                screenTapsActivateCenterObject = VirtualJoystick.JoystickTapsShouldActivateCenterObject,
                leftJoystickEnabled = TouchscreenButtonEnableDisableManager.Instance.IsLeftJoystickEnabled,
                rightJoystickEnabled = TouchscreenButtonEnableDisableManager.Instance.IsRightJoystickEnabled,
                touchscreenSensitivity = TouchscreenInputManager.Instance.TouchscreenSensitivity,
                gestureCombat = DaggerfallUnity.Settings.GestureCombat,
                weaponSwingMode = DaggerfallUnity.Settings.WeaponSwingMode,
                buttons = TouchscreenButtonEnableDisableManager.Instance.GetAllButtons().Select(s => s.GetCurrentConfiguration(layoutName)).ToList()
            };
            layout.buttons.Sort(new TouchscreenButtonConfigurationComparer());
            return layout;
        }
        private void OnLayoutFilePicked(string pickedPath)
        {
            string cachePath = Path.Combine(Application.temporaryCachePath, "TouchscreenLayouts");
            if(Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);
            Directory.CreateDirectory(cachePath);

            string pickedFileNameNoExt = Path.GetFileNameWithoutExtension(pickedPath);
            string extractedPath = Directory.CreateDirectory(Path.Combine(cachePath, pickedFileNameNoExt)).FullName;
            DaggerfallWorkshop.Utility.ZipFileUtils.UnzipFile(pickedPath, extractedPath);

            foreach (string file in Directory.GetFiles(extractedPath, "*.json", SearchOption.AllDirectories))
            {
                string layoutName = Path.GetFileNameWithoutExtension(file);
                string fileParentDir = Path.GetDirectoryName(file);
                if(TouchscreenLayoutConfiguration.ReadFromPath(file) != null)
                {
                    // this is the directory level that we want to copy over to layouts path!
                    void onConfirmOverwrite() {
                        string layoutPath = Path.Combine(LayoutsPath, layoutName);
                        Debug.Log($"{extractedPath}\n\n{fileParentDir}\n\n{file}\n\n{layoutPath}");
                        foreach(string file2 in Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories)){
                            string newFile2Path = file2.Replace(extractedPath, layoutPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(newFile2Path));
                            File.Copy(file2, newFile2Path, true);
                        }
                    }
                    // but we'd better check if it already exists, and notify the user if so
                    if(Directory.Exists(Path.Combine(LayoutsPath, layoutName)))
                        TouchscreenInputManager.Instance.PopupMessage.Open("This layout name already exists! Overwrite the layout?", onConfirmOverwrite, null, "Overwrite", "Cancel");
                    else
                        onConfirmOverwrite();

                    // we can delete the cache path now
                    Directory.Delete(cachePath, true);

                    // load the new layout
                    UpdateLayoutsDropdown();
                    LoadLayoutByName(layoutName);
                    return; // We're done. Return early
                }
            }
            TouchscreenInputManager.Instance.PopupMessage.Open("This doesn't seem to be a valid layout file. Layout must be a zip file containing a '.json' named the same name (i.e. 'layout1.zip', containing 'layout1.json')", null, null, "Okay", "", true);
            Directory.Delete(cachePath, true);
        }
        private void OnButtonTexturePicked(string path)
        {
            string fileName = Path.GetFileName(path);
            string texturesPath = Path.Combine(LayoutsPath, currentlyLoadedLayout.name, "textures");
            string outPath = Path.Combine(texturesPath, fileName);
            Directory.CreateDirectory(texturesPath);
            void onConfirmOverwrite(){
                bool didFileExist = File.Exists(outPath);
                File.Copy(path, outPath, true);
                if(didFileExist){
                    // reload all buttons using that texture
                    LoadLayout(currentlyLoadedLayout);
                } else {
                    // add file to dropdown
                    LoadSpriteDropdown(false);
                    LoadSpriteDropdown(true);
                }
            }
            if(File.Exists(outPath)){
                TouchscreenInputManager.Instance.PopupMessage.Open("Texture already exists. Overwrite it with this new texture?", onConfirmOverwrite, null, "Overwrite", "Cancel");
            } else {
                onConfirmOverwrite();
            }
        }
        private void OnLayoutsDropdownValueChanged(int newVal)
        {
            string selectedLayout = layoutsDropdown.options[newVal].text;
            if (selectedLayout == "<Create New Layout>")
            {
                // Store the previous selection
                int previousSelection = layoutsDropdown.options.FindIndex(p => p.text == (currentlyLoadedLayout?.name ?? "default-layout"));
                
                TouchscreenInputManager.Instance.PopupMessage.Open(
                    "Enter name for new layout:",
                    null,
                    () => {
                        // Restore previous selection on cancel
                        layoutsDropdown.value = previousSelection;
                    },
                    "Create",
                    "Cancel",
                    true,
                    (string newLayoutName) => {
                        if (string.IsNullOrEmpty(newLayoutName))
                        {
                            // Restore previous selection on empty name
                            layoutsDropdown.value = previousSelection;
                            return;
                        }
                        
                        // Check if layout name already exists
                        if (Directory.Exists(Path.Combine(LayoutsPath, newLayoutName)))
                        {
                           CoroutineManager.Instance.DoOnDelay(() => {
                                TouchscreenInputManager.Instance.PopupMessage.Open(
                                    "A layout with that name already exists.",
                                    null,
                                    null,
                                    "Okay",
                                    "",
                                    false
                                );
                            }, .1f);
                            // Restore previous selection on duplicate name
                            layoutsDropdown.value = previousSelection;
                            return;
                        }

                        // Create new layout based on current layout
                        TouchscreenLayoutConfiguration newLayout = GetCurrentLayoutConfig();
                        newLayout.name = newLayoutName;
                        
                        // Create directory and save layout
                        string newLayoutPath = Path.Combine(LayoutsPath, newLayoutName);
                        Directory.CreateDirectory(newLayoutPath);
                        WriteLayoutToPath(newLayout);

                        // Copy textures directory if it exists
                        string sourceTexturesPath = Path.Combine(LayoutsPath, currentlyLoadedLayout.name, "textures");
                        string destTexturesPath = Path.Combine(newLayoutPath, "textures");
                        if (Directory.Exists(sourceTexturesPath))
                        {
                            Directory.CreateDirectory(destTexturesPath);
                            foreach (string file in Directory.GetFiles(sourceTexturesPath, "*.*", SearchOption.AllDirectories))
                            {
                                string destFile = file.Replace(sourceTexturesPath, destTexturesPath);
                                File.Copy(file, destFile, true);
                            }
                        }
                        
                        // Update UI and load new layout
                        UpdateLayoutsDropdown();
                        LoadLayoutByName(newLayoutName);
                    }
                );
            }
            else
            {
                LoadLayoutByName(selectedLayout);
            }
        }
        private void OnSpriteDropdownValueChanged(int newVal)
        {
            ChangeCurrentButtonSprite(newVal, false);

            WriteCurrentLayoutToPath();
        }
        private void OnKnobSpriteDropdownChanged(int newVal)
        {
            ChangeCurrentButtonSprite(newVal, true);

            WriteCurrentLayoutToPath();
        }
        private void OnEditControlsDropdownValueChanged(int newVal)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.myAction = (InputManager.Actions)newVal;

                WriteCurrentLayoutToPath();
            }
        }
        private void OnEditControlsKeyCodeDropdownValueChanged(int newVal)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                KeyCode newKey = (KeyCode)acceptedKeyCodes[keycodeDropdown.options[newVal].text];
                TouchscreenInputManager.Instance.CurrentlyEditingButton.myKey = newKey;

                WriteCurrentLayoutToPath();
            }
        }
        private void OnButtonTypeDropdownValueChanged(int newVal)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.SetButtonType((TouchscreenButtonType)newVal);

                WriteCurrentLayoutToPath();
            }
        }
        private void OnButtonAnchorDropdownValueChanged(int newVal)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.SetButtonAnchor((TouchscreenButtonAnchor)newVal);

                WriteCurrentLayoutToPath();
            }
        }
        private void OnLabelAnchorDropdownValueChanged(int newVal)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.SetLabelAnchor((TouchscreenButtonAnchor)newVal);

                WriteCurrentLayoutToPath();
            }
        }
        private void OnSelectedJoystickSensitivityHorizontalChanged(float value)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.SetJoystickHorizontalSensitivity(value);
                WriteCurrentLayoutToPath();
            }
        }
        private void OnSelectedJoystickSensitivityVerticalChanged(float value)
        {
            if (TouchscreenInputManager.Instance.CurrentlyEditingButton)
            {
                TouchscreenInputManager.Instance.CurrentlyEditingButton.SetJoystickVerticalSensitivity(value);
                WriteCurrentLayoutToPath();
            }
        }
        private void TouchscreenInputManager_OnEditControlsToggled(bool isOn)
        {
            if(!isOn && currentlyLoadedLayout != null)
                WriteCurrentLayoutToPath();
        }

        private List<string> GetImageFilesRecursive(string rootPath, List<string> extensions)
        {
            List<string> files = new List<string>();
            if (!Directory.Exists(rootPath))
                return files;

            try
            {
                foreach (var file in Directory.GetFiles(rootPath))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (extensions.Contains(ext))
                        files.Add(file);
                }
                foreach (var dir in Directory.GetDirectories(rootPath))
                {
                    files.AddRange(GetImageFilesRecursive(dir, extensions));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error enumerating files: {e}");
            }
            return files;
        }
    }
    [System.Serializable]
    public class TouchscreenLayoutConfiguration
    {
        public string name = "default-layout";
        public int daggerPadPresetVersion = 0;
        public float defaultUIAlpha = 1f;
        public bool leftJoystickEnabled = true;
        public bool rightJoystickEnabled = true;
        public bool screenTapsActivateCenterObject = true;
        public float touchscreenSensitivity = 1.0f;
        public bool gestureCombat = false;
        public int weaponSwingMode = -1;
        public List<TouchscreenButtonConfiguration> buttons;

        public static TouchscreenLayoutConfiguration ReadFromPath(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"TouchscreenButtonSerializable file path {path} does not exist");
                return null;
            }
            return Deserialize(File.ReadAllText(path));
        }
        public static void WriteToPath(TouchscreenLayoutConfiguration layout, string path)
        {
            try
            {
                File.WriteAllText(path, Serialize(layout));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write button {layout} to path {path} due to error {e}");
            }
        }
        public static TouchscreenLayoutConfiguration Deserialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<TouchscreenLayoutConfiguration>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize json string into a TouchscreenLayoutConfiguration object due to error {e}\n\nJSON contents:\n{json}");
                return null;
            }
        }
        public static string Serialize(TouchscreenLayoutConfiguration layout)
        {
            try
            {
                // List<TouchscreenButtonConfiguration> prevButtons = layout.buttons.ToList();
                // layout.buttons.ForEach(p => {
                //     p.TexturePath = Path.GetFileName(p.TexturePath);
                //     p.KnobTexturePath = Path.GetFileName(p.KnobTexturePath);
                // });
                // layout.buttons = prevButtons;
                string serializedLayout = JsonConvert.SerializeObject(layout, Formatting.Indented);
                return serializedLayout;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize layout {layout.name} due to error {e}");
                return "";
            }
        }

    }
}

using DaggerfallWorkshop;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DaggerfallWorkshop.Game
{
    public static class AScreen
    {
        // iOS: the game surface (Unity's view, bounds x scale), which follows a resized
        // iPad window and a move to a TV. Screen.width/height there is the render
        // resolution that SetScreenResolution requests, and currentResolution is the
        // display's.
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DaggerPadViewPixelSize(out int width, out int height);

        private static int viewFrame = -1, viewWidth, viewHeight;

        [DllImport("__Internal")]
        private static extern int DaggerPadOnSecondScreen();

        /// <summary>True when the game's window is on a TV or monitor.</summary>
        public static bool OnSecondScreen => DaggerPadOnSecondScreen() != 0;

        private static void ReadView()
        {
            if (viewFrame == Time.frameCount)
                return;
            viewFrame = Time.frameCount;
            DaggerPadViewPixelSize(out viewWidth, out viewHeight);
        }
#endif
        /// <summary>
        /// Platform-independent screen width
        /// </summary>
        public static int width
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                ReadView();
                if (viewWidth > 0)
                    return viewWidth;
                if (Display.main.systemWidth > 0)
                    return Display.main.systemWidth;
#endif
                return Application.isMobilePlatform || AndroidUtils.IsRunningInSimulator ? Screen.currentResolution.width : Screen.width;
            }
        }
        /// <summary>
        /// Platform-independent screen height
        /// </summary>
        public static int height
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                ReadView();
                if (viewHeight > 0)
                    return viewHeight;
                if (Display.main.systemHeight > 0)
                    return Display.main.systemHeight;
#endif
                return Application.isMobilePlatform || AndroidUtils.IsRunningInSimulator ? Screen.currentResolution.height : Screen.height;
            }
        }
    }

    /// <summary>
    /// Handles Android's Screen stuff like resolution setting and orientation
    /// </summary>
    public class AndroidScreenManager : MonoBehaviour
    {
        public static event System.Action<Resolution> ScreenResolutionChanged;
        private Resolution lastResolution;

        private bool isSetup = false;

        private IEnumerator Start()
        {
            for (int i = 0; i < 5; ++i){ // one of those race conditions that require things setup after Start()
                lastResolution = new Resolution() { width = DaggerfallUnity.Settings.ResolutionWidth, height = DaggerfallUnity.Settings.ResolutionHeight };
                SetResolution(lastResolution.width, lastResolution.height);
                GameManager.UpdateScreenOrientation();
                yield return null;
            }
            isSetup = true;
        }
        private void Update()
        {
            if(!isSetup)
                return;

            int x = AScreen.width;
            int y = AScreen.height;
            if (x != lastResolution.width || y != lastResolution.height){
                // looks like the resolution changed. Let's update the daggerfall unity resolution
                SetResolution(x, y);
                lastResolution = new Resolution() { width = x, height = y };

                GameManager.UpdateScreenOrientation();
            }
        }
        public static void SetResolution(int x, int y)
        {
            Debug.Log("AndroidScreenManager: Current screen updated to new resolution");

            SettingsManager.SetScreenResolution(x, y, DaggerfallUnity.Settings.Fullscreen);
#if UNITY_IOS && !UNITY_EDITOR
            LogDisplay(x, y);
#endif
            var allCams = FindObjectsOfType<Camera>();
            foreach (var cam in allCams)
                cam.ResetAspect();
            if(TouchscreenInputManager.Instance)
                TouchscreenInputManager.Instance.SetupUIRenderTexture();

            ScreenResolutionChanged?.Invoke(new Resolution(){width=x, height=y});
        }
#if UNITY_IOS && !UNITY_EDITOR
        // One line per resolution change in Documents/DaggerPad-display.log, so a wrong
        // layout on a TV can be read back from the iPad afterwards.
        private static void LogDisplay(int x, int y)
        {
            try
            {
                string line = string.Format("{0:u} asked {1}x{2} view {3}x{4} render {5}x{6} display {7}x{8} system {9}x{10} screens {11} tv {12}\n",
                    System.DateTime.UtcNow, x, y, AScreen.width, AScreen.height, Screen.width, Screen.height,
                    Screen.currentResolution.width, Screen.currentResolution.height,
                    Display.main.systemWidth, Display.main.systemHeight, Display.displays.Length, AScreen.OnSecondScreen);
                string path = System.IO.Path.Combine(Application.persistentDataPath, "DaggerPad-display.log");
                var info = new System.IO.FileInfo(path);
                if (info.Exists && info.Length > 256 * 1024)
                    info.Delete();
                System.IO.File.AppendAllText(path, line);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("DaggerPad display log: " + e.Message);
            }
        }
#endif
        public static void SetOrientationToAny()
        {
            Debug.Log("AndroidScreenManager: Setting oritentation to Any");
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
        public static void SetOrientationToPortrait()
        {
            Debug.Log("AndroidScreenManager: Setting oritentation to Portrait");
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
        public static void SetOrientationToLandscape()
        {
            Debug.Log("AndroidScreenManager: Setting oritentation to Landscape");
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
            // DeviceOrientationManager.ForceOrientation(ScreenOrientation.AutoRotation);
        }
    }
}

using System;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerPad.iOS
{
    /// <summary>
    /// Protects an active session when iPadOS backgrounds or suspends the app.
    /// </summary>
    public sealed class DaggerPadLifecycle : MonoBehaviour
    {
        private const string SuspensionSaveName = "DaggerPad AutoSave";
        private const float SaveDebounceSeconds = 3f;

        private float lastSuspensionSaveTime = float.NegativeInfinity;
        private bool pausedAudio;
        private bool twoFingerTapActive;
        private int firstFingerId = -1;
        private int secondFingerId = -1;
        private Vector2 firstFingerStart;
        private Vector2 secondFingerStart;
        private float twoFingerTapStartTime;
        private float twoFingerMaximumMovement;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_IOS && !UNITY_EDITOR
            GameObject lifecycleObject = new GameObject(nameof(DaggerPadLifecycle));
            DontDestroyOnLoad(lifecycleObject);
            lifecycleObject.AddComponent<DaggerPadLifecycle>();
#endif
        }

        private int loggedClicks;

        // The first 60 clicks of each launch, appended to Documents/DaggerPad-display.log:
        // where Unity and Daggerfall's UI think the pointer is, and how the top window is
        // laid out, so a menu that does not fit or answer on a TV can be read back.
        private void LogClickGeometry()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (loggedClicks >= 60 || !Input.GetMouseButtonDown(0))
                return;
            loggedClicks++;
            try
            {
                string top = "none";
                if (DaggerfallUI.Instance != null && DaggerfallUI.UIManager != null)
                {
                    var window = DaggerfallUI.UIManager.TopWindow as DaggerfallWorkshop.Game.UserInterface.UserInterfaceWindow;
                    if (window != null)
                    {
                        var parent = window.ParentPanel;
                        string native = "";
                        var baseWindow = window as DaggerfallWorkshop.Game.UserInterfaceWindows.DaggerfallBaseWindow;
                        if (baseWindow != null && baseWindow.NativePanel != null)
                            native = string.Format(" native {0} scale {1} uimouse {2}", baseWindow.NativePanel.Rectangle,
                                baseWindow.NativePanel.LocalScale, baseWindow.NativePanel.ScaledMousePosition);
                        top = string.Format("{0} parent {1}{2}", window.GetType().Name, parent != null ? parent.Rectangle.ToString() : "-", native);
                    }
                }
                string line = string.Format("{0:u} click unity {1} input {2} screen {3}x{4} ascreen {5}x{6} orientation {7} safe {8} top {9}
",
                    DateTime.UtcNow, Input.mousePosition, InputManager.Instance != null ? InputManager.Instance.MousePosition.ToString() : "-",
                    Screen.width, Screen.height, AScreen.width, AScreen.height, Screen.orientation, Screen.safeArea, top);
                System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "DaggerPad-display.log"), line);
            }
            catch (Exception e)
            {
                Debug.LogWarning("DaggerPad click log: " + e.Message);
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveForSuspension();
        }

        private void Update()
        {
            LogClickGeometry();
            bool wantsPointerLock = GameManager.HasInstance &&
                GameManager.Instance.PlayerMouseLook != null &&
                GameManager.Instance.PlayerMouseLook.WantsPointerCapture;
            DaggerPadPointerInput.SetPointerLock(wantsPointerLock);

            if (!DaggerfallUnity.Settings.GestureCombat || Cursor.visible ||
                !GameManager.HasInstance || !GameManager.Instance.StateManager.GameInProgress)
            {
                twoFingerTapActive = false;
                return;
            }

            int activeTouchCount = 0;
            Touch firstTouch = default;
            Touch secondTouch = default;
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    continue;
                if (activeTouchCount == 0)
                    firstTouch = touch;
                else if (activeTouchCount == 1)
                    secondTouch = touch;
                activeTouchCount++;
            }

            if (!twoFingerTapActive && activeTouchCount >= 2)
            {
                twoFingerTapActive = true;
                firstFingerId = firstTouch.fingerId;
                secondFingerId = secondTouch.fingerId;
                firstFingerStart = firstTouch.position;
                secondFingerStart = secondTouch.position;
                twoFingerTapStartTime = Time.realtimeSinceStartup;
                twoFingerMaximumMovement = 0f;
            }

            if (!twoFingerTapActive)
                return;

            VirtualJoystick.SuppressForMultiTouchGesture();
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.fingerId == firstFingerId)
                    twoFingerMaximumMovement = Mathf.Max(twoFingerMaximumMovement, Vector2.Distance(firstFingerStart, touch.position));
                else if (touch.fingerId == secondFingerId)
                    twoFingerMaximumMovement = Mathf.Max(twoFingerMaximumMovement, Vector2.Distance(secondFingerStart, touch.position));
            }

            if (activeTouchCount > 0)
                return;

            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            float duration = Time.realtimeSinceStartup - twoFingerTapStartTime;
            if (duration <= 0.45f && twoFingerMaximumMovement <= dpi * 0.12f)
                TouchscreenInputManager.TriggerAction(InputManager.Actions.ReadyWeapon);

            twoFingerTapActive = false;
            firstFingerId = -1;
            secondFingerId = -1;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                SaveForSuspension();
                pausedAudio = !AudioListener.pause;
                AudioListener.pause = true;
            }
            else if (pausedAudio)
            {
                AudioListener.pause = false;
                pausedAudio = false;
            }
        }

        private void SaveForSuspension()
        {
            if (Time.realtimeSinceStartup - lastSuspensionSaveTime < SaveDebounceSeconds)
                return;
            if (!GameManager.HasInstance || !GameManager.Instance.StateManager.GameInProgress)
                return;

            SaveLoadManager saveLoadManager = GameManager.Instance.SaveLoadManager;
            if (saveLoadManager == null || !saveLoadManager.IsReady() || saveLoadManager.LoadInProgress || saveLoadManager.IsSavingPrevented)
                return;

            try
            {
                saveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, SuspensionSaveName);
                lastSuspensionSaveTime = Time.realtimeSinceStartup;
                Debug.Log("DaggerPad queued an automatic save before suspension.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DaggerPad could not save before suspension: " + ex.Message);
            }
        }
    }
}

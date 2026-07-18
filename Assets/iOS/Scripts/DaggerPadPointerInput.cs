using System.Runtime.InteropServices;
using UnityEngine;

namespace DaggerPad.iOS
{
    /// <summary>
    /// Exposes raw iPadOS mouse and trackpad state from GameController.framework.
    /// Unity's legacy Input.mousePresent is always false on iOS, so it cannot be
    /// used to decide whether an indirect pointing device is connected.
    /// </summary>
    public static class DaggerPadPointerInput
    {
        private const int ButtonCount = 3;

#if UNITY_IOS && !UNITY_EDITOR
        private static readonly bool[] previousButtons = new bool[ButtonCount];
        private static readonly bool[] currentButtons = new bool[ButtonCount];
        private static readonly bool[] buttonsDown = new bool[ButtonCount];
        private static readonly bool[] buttonsUp = new bool[ButtonCount];
        private static int lastButtonFrame = -1;

        [DllImport("__Internal")]
        private static extern int DaggerPadPointerIsConnected();

        [DllImport("__Internal")]
        private static extern void DaggerPadPointerConsumeDelta(out float deltaX, out float deltaY);

        [DllImport("__Internal")]
        private static extern int DaggerPadPointerGetButton(int button);

        [DllImport("__Internal")]
        private static extern void DaggerPadPointerSetLock(int shouldLock);
#endif

        public static bool IsConnected
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return DaggerPadPointerIsConnected() != 0;
#else
                return false;
#endif
            }
        }

        public static Vector2 ConsumeDelta()
        {
#if UNITY_IOS && !UNITY_EDITOR
            DaggerPadPointerConsumeDelta(out float deltaX, out float deltaY);
            return new Vector2(deltaX, deltaY);
#else
            return Vector2.zero;
#endif
        }

        public static bool GetMouseButton(int button)
        {
#if UNITY_IOS && !UNITY_EDITOR
            RefreshButtons();
            return IsValidButton(button) && currentButtons[button];
#else
            return false;
#endif
        }

        public static bool GetMouseButtonDown(int button)
        {
#if UNITY_IOS && !UNITY_EDITOR
            RefreshButtons();
            return IsValidButton(button) && buttonsDown[button];
#else
            return false;
#endif
        }

        public static bool GetMouseButtonUp(int button)
        {
#if UNITY_IOS && !UNITY_EDITOR
            RefreshButtons();
            return IsValidButton(button) && buttonsUp[button];
#else
            return false;
#endif
        }

        public static void SetPointerLock(bool shouldLock)
        {
#if UNITY_IOS && !UNITY_EDITOR
            DaggerPadPointerSetLock(shouldLock && IsConnected ? 1 : 0);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static void RefreshButtons()
        {
            if (lastButtonFrame == Time.frameCount)
                return;

            for (int button = 0; button < ButtonCount; button++)
            {
                previousButtons[button] = currentButtons[button];
                currentButtons[button] = DaggerPadPointerGetButton(button) != 0;
                buttonsDown[button] = currentButtons[button] && !previousButtons[button];
                buttonsUp[button] = !currentButtons[button] && previousButtons[button];
            }

            lastButtonFrame = Time.frameCount;
        }

        private static bool IsValidButton(int button)
        {
            return button >= 0 && button < ButtonCount;
        }
#endif
    }
}

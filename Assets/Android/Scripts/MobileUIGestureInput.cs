using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Converts touch-only retro UI gestures into mouse buttons without firing an early left click.
    /// Tap = left click, stationary long press = right click, two-finger tap = middle click.
    /// </summary>
    public static class MobileUIGestureInput
    {
        private const float LongPressSeconds = 0.55f;
        private const float TwoFingerTapSeconds = 0.45f;
        private const float TapMovementInches = 0.12f;

        private static readonly bool[] down = new bool[3];
        private static readonly bool[] up = new bool[3];
        private static readonly bool[] held = new bool[3];
        private static readonly List<Touch> activeTouches = new List<Touch>(3);

        private static int updatedFrame = -1;
        private static bool singleActive;
        private static bool singleConsumed;
        private static int singleFingerId = -1;
        private static Vector2 singleStartPosition;
        private static float singleStartTime;
        private static float singleMaximumMovement;

        private static bool multiActive;
        private static int firstFingerId = -1;
        private static int secondFingerId = -1;
        private static Vector2 firstStartPosition;
        private static Vector2 secondStartPosition;
        private static float multiStartTime;
        private static float multiMaximumMovement;

        public static bool IsActive
        {
            get
            {
                if (!Application.isMobilePlatform || !Input.touchSupported || !GameManager.HasInstance)
                    return false;

                StateManager.StateTypes state = GameManager.Instance.StateManager.CurrentState;
                return state == StateManager.StateTypes.UI || state == StateManager.StateTypes.Setup;
            }
        }

        public static bool GetMouseButtonDown(int button)
        {
            UpdateIfNeeded();
            return IsValidButton(button) && down[button];
        }

        public static bool GetMouseButtonUp(int button)
        {
            UpdateIfNeeded();
            return IsValidButton(button) && up[button];
        }

        public static bool GetMouseButton(int button)
        {
            UpdateIfNeeded();
            return IsValidButton(button) && held[button];
        }

        private static void UpdateIfNeeded()
        {
            if (updatedFrame == Time.frameCount)
                return;

            updatedFrame = Time.frameCount;
            for (int index = 0; index < held.Length; index++)
            {
                down[index] = false;
                up[index] = held[index];
                held[index] = false;
            }

            activeTouches.Clear();
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                    activeTouches.Add(touch);
            }

            if (activeTouches.Count >= 2)
            {
                UpdateMultipleTouches(activeTouches[0], activeTouches[1]);
                return;
            }

            if (multiActive)
            {
                if (activeTouches.Count == 0)
                    FinishMultipleTouches();
                return;
            }

            if (activeTouches.Count == 1)
                UpdateSingleTouch(activeTouches[0]);
            else if (singleActive)
                FinishSingleTouch();
        }

        private static void UpdateSingleTouch(Touch touch)
        {
            if (!singleActive || singleFingerId != touch.fingerId)
            {
                singleActive = true;
                singleConsumed = false;
                singleFingerId = touch.fingerId;
                singleStartPosition = touch.position;
                singleStartTime = Time.realtimeSinceStartup;
                singleMaximumMovement = 0f;
            }

            singleMaximumMovement = Mathf.Max(singleMaximumMovement, Vector2.Distance(singleStartPosition, touch.position));
            if (!singleConsumed && singleMaximumMovement <= TapMovementPixels &&
                Time.realtimeSinceStartup - singleStartTime >= LongPressSeconds)
            {
                Pulse(1);
                singleConsumed = true;
            }
        }

        private static void FinishSingleTouch()
        {
            if (!singleConsumed && singleMaximumMovement <= TapMovementPixels)
                Pulse(0);

            ResetSingleTouch();
        }

        private static void UpdateMultipleTouches(Touch first, Touch second)
        {
            if (!multiActive)
            {
                multiActive = true;
                multiStartTime = Time.realtimeSinceStartup;
                firstFingerId = first.fingerId;
                secondFingerId = second.fingerId;
                firstStartPosition = first.position;
                secondStartPosition = second.position;
                multiMaximumMovement = 0f;
                ResetSingleTouch();
            }

            if (first.fingerId == firstFingerId)
                multiMaximumMovement = Mathf.Max(multiMaximumMovement, Vector2.Distance(firstStartPosition, first.position));
            else if (first.fingerId == secondFingerId)
                multiMaximumMovement = Mathf.Max(multiMaximumMovement, Vector2.Distance(secondStartPosition, first.position));

            if (second.fingerId == firstFingerId)
                multiMaximumMovement = Mathf.Max(multiMaximumMovement, Vector2.Distance(firstStartPosition, second.position));
            else if (second.fingerId == secondFingerId)
                multiMaximumMovement = Mathf.Max(multiMaximumMovement, Vector2.Distance(secondStartPosition, second.position));
        }

        private static void FinishMultipleTouches()
        {
            float duration = Time.realtimeSinceStartup - multiStartTime;
            if (duration <= TwoFingerTapSeconds && multiMaximumMovement <= TapMovementPixels)
                Pulse(2);

            multiActive = false;
            firstFingerId = -1;
            secondFingerId = -1;
        }

        private static void ResetSingleTouch()
        {
            singleActive = false;
            singleConsumed = false;
            singleFingerId = -1;
        }

        private static void Pulse(int button)
        {
            down[button] = true;
            held[button] = true;
        }

        private static bool IsValidButton(int button)
        {
            return button >= 0 && button < down.Length;
        }

        private static float TapMovementPixels
        {
            get
            {
                float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
                return dpi * TapMovementInches;
            }
        }
    }
}

// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2024 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Vivian V Wing (vwing@multitude.city)
// Contributors:
// 
// Notes:
//

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace DaggerfallWorkshop.Game
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler
    {
        public RectTransform background;
        public RectTransform knob;

        [Header("Input Settings")]
        public InputManager.AxisActions horizontalAxisAction = InputManager.AxisActions.MovementHorizontal;
        public InputManager.AxisActions verticalAxisAction = InputManager.AxisActions.MovementVertical;
        public Vector2 deadzone = Vector2.zero;
        public bool isInMouseLookMode = false;
        [Tooltip("Scales the logical drag radius without changing joystick artwork size.")]
        public float inputRadiusMultiplier = 1f;

        public PointerEventData CurrentPointerEventData {get; private set;}
        public Vector2 TouchStartPos {get; private set;}
        public float TouchStartTime {get; private set;}
        public static VirtualJoystick JoystickThatIsCurrentlyMouseLooking{get; private set;} = null;
        public static bool GestureCombatActive { get; private set; }
        private static int suppressTapActionsThroughFrame = -1;
        public static bool JoystickTapsShouldActivateCenterObject 
        {
            get{ return PlayerPrefs.GetInt("JoystickTapsShouldActivateCenterObject", 1) == 1; }
            set{ PlayerPrefs.SetInt("JoystickTapsShouldActivateCenterObject", value ? 1 : 0); }
        }

        private Vector2 inputVector;
        private float joystickRadius;
        private float visualRadius;
        private bool isTouching = false;
        private Camera myCam;
        private RectTransform rootRectTF;

        private int myTouchFingerID = -1;
        private bool gestureCombatTouch;
        private bool suppressTapAction;
        private float lastLookTapTime = -1f;
        private Vector2 lastLookTapPosition;

        void Start()
        {
            // get refs
            myCam = GetComponentInParent<Canvas>().worldCamera;
            rootRectTF = transform as RectTransform;
            while (rootRectTF.parent is RectTransform)
                rootRectTF = rootRectTF.parent as RectTransform;

            // set size to half of the screen area
            UpdateSizeOfRectTF(rootRectTF.rect.width);

            // set vars
            Rect joystickRect = UnityUIUtils.GetScreenspaceRect(background, myCam);
            Rect knobRect = UnityUIUtils.GetScreenspaceRect(knob, myCam);
            visualRadius = joystickRect.width / 2f - knobRect.width/2f;
            joystickRadius = visualRadius * Mathf.Max(0.1f, inputRadiusMultiplier);

            // Initially invisible
            SetJoystickVisibility(false);

            AndroidScreenManager.ScreenResolutionChanged += AndroidScreenManager_ScreenResolutionChanged;
        }
        void OnDestroy()
        {
            EndGestureCombat();
            AndroidScreenManager.ScreenResolutionChanged -= AndroidScreenManager_ScreenResolutionChanged;
        }
        // set size to half of the screen area
        private void UpdateSizeOfRectTF(float screenWidth)
        {
            Debug.Log("VirtualJoystick: Updating size of rect tf");
            (transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenWidth / 2f);
        }
        private void LateUpdate()
        {
            if (isTouching && myTouchFingerID >= 0)
            {
                if (!TryGetTrackedTouch(out Touch myTouch))
                {
                    if (Input.touchCount == 0)
                        OnPointerUp(null);
                    return;
                }

                if (myTouch.phase == TouchPhase.Ended || myTouch.phase == TouchPhase.Canceled)
                    OnPointerUp(null);
                else if (!isInMouseLookMode)
                    UpdateInputFromPosition(myTouch.position);
            }
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            TouchStartPos = eventData.position;
            TouchStartTime = Time.time;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background.parent as RectTransform, TouchStartPos, myCam, out Vector2 backgroundPos))
            {
                background.localPosition = backgroundPos;
            }
            knob.position = background.position;
            SetJoystickVisibility(true);
            isTouching = true;
            UpdateInputFromPosition(eventData.position);
            myTouchFingerID = eventData.pointerId;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == eventData.pointerId || Vector2.Distance(touch.position, TouchStartPos) < 32f)
                {
                    myTouchFingerID = touch.fingerId;
                    break;
                }
            }
            if (isInMouseLookMode && !JoystickThatIsCurrentlyMouseLooking)
                JoystickThatIsCurrentlyMouseLooking = this;
            BeginGestureCombatIfReady();
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            EndGestureCombat();
            SetJoystickVisibility(false);
            inputVector = Vector2.zero;
            UpdateVirtualAxes(Vector2.zero);
            isTouching = false;
            if (JoystickThatIsCurrentlyMouseLooking == this)
                JoystickThatIsCurrentlyMouseLooking = null;
            myTouchFingerID = -1;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isTouching)
                return;

            CurrentPointerEventData = eventData;
            UpdateInputFromPosition(eventData.position);
        }

        private void UpdateInputFromPosition(Vector2 pointerPosition)
        {
            Vector2 direction = pointerPosition - TouchStartPos;
            inputVector = Vector2.ClampMagnitude(direction / joystickRadius, 1f);
            Vector2 knobPosScreenSpace = TouchStartPos + inputVector * visualRadius;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(knob.parent as RectTransform, knobPosScreenSpace, myCam, out Vector2 knobPos))
                knob.localPosition = knobPos;
            bool isMovementJoystick = verticalAxisAction == InputManager.AxisActions.MovementVertical;
            if (Mathf.Abs(inputVector.x) < deadzone.x && Mathf.Abs(inputVector.y) < deadzone.y)
                inputVector = Vector2.zero;
            else if (isMovementJoystick && Mathf.Abs(inputVector.x) / Mathf.Abs(inputVector.y) > 2.4142f)
                inputVector.y = 0;
            else if (isMovementJoystick && Mathf.Abs(inputVector.y) / Mathf.Abs(inputVector.x) > 2.4142f)
                inputVector.x = 0;
            UpdateVirtualAxes(inputVector);
        }

        public Vector2 GetCurrentTouchDelta()
        {
            if (TryGetTrackedTouch(out Touch touch))
                return touch.deltaPosition;
            return CurrentPointerEventData != null ? CurrentPointerEventData.delta : Vector2.zero;
        }

        private bool TryGetTrackedTouch(out Touch trackedTouch)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch touch = Input.GetTouch(index);
                if (touch.fingerId == myTouchFingerID)
                {
                    trackedTouch = touch;
                    return true;
                }
            }

            trackedTouch = default;
            return false;
        }

        private void UpdateVirtualAxes(Vector2 inputVec)
        {
            if (isInMouseLookMode)
            {
                return;
            }
            TouchscreenInputManager.SetAxis(horizontalAxisAction, inputVec.x);
            TouchscreenInputManager.SetAxis(verticalAxisAction, inputVec.y);
        }

        private void SetJoystickVisibility(bool isVisible)
        {
            background.gameObject.SetActive(isVisible && !isInMouseLookMode);
            knob.gameObject.SetActive(isVisible && !isInMouseLookMode);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.frameCount <= suppressTapActionsThroughFrame)
                return;

            if (suppressTapAction)
            {
                suppressTapAction = false;
                return;
            }

            Vector2 deltaPos = TouchStartPos - eventData.position;
            bool isWithinDeadzone = Mathf.Abs(deltaPos.x) < joystickRadius * 0.1f && Mathf.Abs(deltaPos.y) < joystickRadius * 0.1f;
            bool isQuickTap = isWithinDeadzone && Time.time - TouchStartTime < .35f;
            if (isInMouseLookMode && isQuickTap)
            {
                bool isDoubleTap = Time.time - lastLookTapTime <= .35f &&
                    Vector2.Distance(eventData.position, lastLookTapPosition) <= 80f;
                lastLookTapTime = Time.time;
                lastLookTapPosition = eventData.position;
                if (isDoubleTap)
                {
                    lastLookTapTime = -1f;
                    TouchscreenInputManager.TriggerAction(InputManager.Actions.ActivateCenterObject);
                    DaggerfallUI.AddHUDText("USE / TAKE", 0.8f);
                }
                return;
            }

            if (JoystickTapsShouldActivateCenterObject && isQuickTap)
                TouchscreenInputManager.TriggerAction(InputManager.Actions.ActivateCenterObject);
        }

        private void BeginGestureCombatIfReady()
        {
            if (!isInMouseLookMode || !DaggerfallUnity.Settings.GestureCombat || !GameManager.HasInstance ||
                GameManager.Instance.WeaponManager.Sheathed)
                return;

            KeyCode swingKey = InputManager.Instance.GetBinding(InputManager.Actions.SwingWeapon);
            TouchscreenInputManager.SetKey(swingKey, true);
            gestureCombatTouch = true;
            suppressTapAction = true;
            GestureCombatActive = true;
        }

        private void EndGestureCombat()
        {
            if (!gestureCombatTouch)
                return;

            if (InputManager.HasInstance)
            {
                KeyCode swingKey = InputManager.Instance.GetBinding(InputManager.Actions.SwingWeapon);
                TouchscreenInputManager.SetKey(swingKey, false);
            }

            gestureCombatTouch = false;
            GestureCombatActive = false;
        }

        public static void SuppressForMultiTouchGesture()
        {
            suppressTapActionsThroughFrame = Time.frameCount + 1;
            if (JoystickThatIsCurrentlyMouseLooking)
                JoystickThatIsCurrentlyMouseLooking.EndGestureCombat();
        }
        void AndroidScreenManager_ScreenResolutionChanged(Resolution newResolution)
        {
            // UpdateSizeOfRectTF(newResolution.width);
            // UpdateSizeOfRectTF(rootRectTF.rect.width);
            StartCoroutine(UpdateSizeOfRectTFCoroutine());
        }

        private IEnumerator UpdateSizeOfRectTFCoroutine()
        {
            for(int i = 0; i < 5; ++i) // one of those
            {
                UpdateSizeOfRectTF(rootRectTF.rect.width);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}

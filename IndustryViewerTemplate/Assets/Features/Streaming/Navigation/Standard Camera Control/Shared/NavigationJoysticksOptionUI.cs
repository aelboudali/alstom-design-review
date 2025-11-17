using System.Linq;
using Unity.Industry.Viewer.Streaming;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Industry.Viewer.Navigation.StandardCameraControl.Shared
{
    [DefaultExecutionOrder(101)]
    public abstract class NavigationJoysticksOptionUI : NavigationOptionUI
    {
        const string k_JoystickToggle = "JoystickToggle";
        const string k_LeftStickBackground = "left_stick_background";
        const string k_LeftStickHandle = "left_stick";
        const string k_RightStickBackground = "right_stick_background";
        const string k_RightStickHandle = "right_stick";
        const string k_CameraUI = "CameraTouchControlUI";

        [Header("Touch Control Joysticks")]
        [SerializeField]
        UIDocument m_UIDocument;

        VisualElement m_ControllerUI;
        GamepadStick m_LeftStick, m_RightStick;
        StickSettings m_LeftStickSettings, m_RightStickSettings;

        [Header("Left Stick")]
        [SerializeField]
        private bool leftStickEnabled = true;
        [SerializeField]
        private float initialLeftStickMultiplier = 1f;
        [SerializeField]
        private bool invertLeftStickYAxis = false;
        [SerializeField]
        private bool invertLeftStickXAxis = false;

        [Header("Right Stick")]
        [SerializeField]
        private bool rightStickEnabled = true;
        [SerializeField]
        private float initialRightStickMultiplier = 1f;
        [SerializeField]
        private bool invertRightStickYAxis = false;
        [SerializeField]
        private bool invertRightStickXAxis = false;

        private AppUI.UI.Toggle m_JoystickToggle;
        private bool showJoystick;
        private bool joystickToggled;
        private bool controlIsPaused;
        private bool originalJoyStickState;

        protected CameraInputSystemController m_baseCameraInputSystemController;

        protected virtual void OnEnable()
        {
            if (PlayerPrefs.HasKey(StandardCamera.JoystickEnable))
            {
                showJoystick = PlayerPrefs.GetInt(StandardCamera.JoystickEnable) == 1;
            }
            
            if (m_ControllerUI == null)
            {
                InitializeTouchControlsUI();
            }
            else
            {
                m_LeftStick?.TurnOnUI();
                m_RightStick?.TurnOnUI();
                CheckForTouchScreen();
            }
        }

        protected virtual void Start()
        {
            NavigationController.PauseCameraControl += PauseCameraControl;
            InputSystem.onDeviceChange += InputSystemOnDeviceChange;
        }

        protected virtual void OnDisable()
        {
            if (m_ControllerUI != null)
            {
                m_LeftStick?.ResetUI();
                m_RightStick?.ResetUI();
                m_ControllerUI.style.display = DisplayStyle.None;
            }
        }

        protected virtual void OnDestroy()
        {
            NavigationController.PauseCameraControl -= PauseCameraControl;
            m_LeftStick?.UnregisterCallbacks();
            m_RightStick?.UnregisterCallbacks();
            InputSystem.onDeviceChange -= InputSystemOnDeviceChange;
        }

        private void PauseCameraControl(bool pause)
        {
            controlIsPaused = pause;
            if (pause)
            {
                originalJoyStickState = showJoystick;
                if (gameObject.activeSelf)
                {
                    m_ControllerUI.style.display = DisplayStyle.None;
                }

                showJoystick = false;
            }
            else
            {
                if (gameObject.activeSelf)
                {
                    m_ControllerUI.style.display = originalJoyStickState ? DisplayStyle.Flex : DisplayStyle.None;
                }

                showJoystick = originalJoyStickState;
            }
        }

        protected override void InitialUI(VisualElement panel)
        {
            m_JoystickToggle = panel.Q<AppUI.UI.Toggle>(k_JoystickToggle);
            if (PlayerPrefs.HasKey(StandardCamera.JoystickEnable))
            {
                showJoystick = PlayerPrefs.GetInt(StandardCamera.JoystickEnable) == 1;
            }
            m_JoystickToggle.RegisterValueChangedCallback(evt =>
            {
                showJoystick = evt.newValue;
                PlayerPrefs.SetInt(StandardCamera.JoystickEnable, showJoystick ? 1 : 0);
                joystickToggled = true;
                CheckForTouchScreen();
            });

            m_JoystickToggle.SetValueWithoutNotify(showJoystick);
            m_JoystickToggle.SetEnabled(!controlIsPaused);
        }

        private void InputSystemOnDeviceChange(InputDevice arg1, InputDeviceChange arg2)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            CheckForTouchScreen();
        }

        private void InitializeTouchControlsUI()
        {
            if (m_UIDocument == null) return;

            m_ControllerUI = m_UIDocument.rootVisualElement.Q<VisualElement>(k_CameraUI);
            CheckForTouchScreen();

            if (leftStickEnabled)
            {
                m_LeftStickSettings.invertStickYAxis = invertLeftStickYAxis;
                m_LeftStickSettings.invertStickXAxis = invertLeftStickXAxis;
                m_LeftStickSettings.initialStickMultiplier = initialLeftStickMultiplier;
                m_LeftStickSettings.cameraInputSystemController = m_baseCameraInputSystemController;
                m_LeftStick = new GamepadStick(m_ControllerUI, k_LeftStickBackground, k_LeftStickHandle, false, m_LeftStickSettings);
            }

            if (rightStickEnabled)
            {
                m_RightStickSettings.invertStickYAxis = invertRightStickYAxis;
                m_RightStickSettings.invertStickXAxis = invertRightStickXAxis;
                m_RightStickSettings.initialStickMultiplier = initialRightStickMultiplier;
                m_RightStickSettings.cameraInputSystemController = m_baseCameraInputSystemController;
                m_RightStick = new GamepadStick(m_ControllerUI, k_RightStickBackground, k_RightStickHandle, true, m_RightStickSettings);
            }
        }

        private void CheckForTouchScreen()
        {
            if (controlIsPaused) return;

            if (showJoystick)
            {
                m_ControllerUI.style.display = DisplayStyle.Flex;
                joystickToggled = false;
                return;
            }

            if (!joystickToggled)
            {
                showJoystick = InputSystem.devices.Any(inputDevice => inputDevice is Touchscreen) &&
                               !PlayerPrefs.HasKey(StandardCamera.JoystickEnable);
                m_JoystickToggle?.SetValueWithoutNotify(showJoystick);
            }

            m_ControllerUI.style.display = showJoystick ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
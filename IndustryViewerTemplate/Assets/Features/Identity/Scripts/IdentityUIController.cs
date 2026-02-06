using Unity.AppUI.Core;
using Unity.AppUI.UI;
using Unity.Cloud.Identity;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using System;
using UnityEngine.SceneManagement;

namespace Unity.Industry.Viewer.Identity
{
    // This script manages the UI for user authentication in Unity.
    // It handles the display and interaction of login, logout, and guest login buttons.
    // The script updates the UI based on the authentication state and user information.
    // It includes event handlers for button clicks and authentication state changes.
    // The script integrates with the IdentityController to trigger authentication actions and update the UI accordingly.
    [DefaultExecutionOrder(-100)]
    public class IdentityUIController: MonoBehaviour
    {
        // Constants for UI element names
        private const string k_IdentityContainerName = "IdentityContainer";
        private const string k_LoginButtonName = "LoginButton";
        private const string k_LogoutButtonName = "LogoutButton";
        private const string k_GuestButtonName = "GuestButton";
        private const string k_AvatarName = "IdentityAvatar";
        private const string k_IdentityAvatarInitialLabelName = "IdentityAvatarInitialLabel";
        private const string k_GuestContainerName = "GuestContainer";
        private const string k_CursorPointerClass = "cursor--pointer";
        
        // Serialized fields for UI elements
        [SerializeField]
        private UIDocument m_appUIDocument;

        [SerializeField] private VisualTreeAsset m_IdentityPopoverTemplate;
        
        // UI elements
        private ActionButton _loginButton, _guestButton, _logoutButton;
        private VisualElement _identityContainer, _guestContainer;
        private CustomAvatar _avatar;
        private Text _identityAvatarInitialLabel;
        
        // Authentication state
        private AuthenticationState _currentState;
        private Popover _identityPopover;

        #region Localisation
        [SerializeField]
        private LocalizedString m_LocalizedLogin;
        [SerializeField]
        private LocalizedString m_LocalizedLogout;
        [SerializeField]
        private LocalizedString m_LocalizedLogoutCompletely;
        [SerializeField]
        private LocalizedString m_LocalizedCancel;
        [SerializeField]
        private LocalizedString m_LocalizedLogoutTitle;
        [SerializeField]
        private LocalizedString m_LocalizedLogoutDescription;
        [SerializeField]
        private LocalizedString m_LocalizedAwaiting;
        [SerializeField]
        private LocalizedString m_LocalizedInitializing;

        [SerializeField] private LocalizedString m_OfflineModeTitle;
        [SerializeField]
        private LocalizedString m_LoginInOfflineModeDescription;

        [SerializeField] private LocalizedString m_OK;
        #endregion

        private bool m_Initialized;

        private bool m_AttemptToLoginAfterReconnect;
        private Action m_LoginAction;
        
        // initialize the UI elements and event handlers
        private void Awake()
        {
            NetworkDetector.OnNetworkStatusChanged += OnNetworkStatusChanged;
            
            _currentState = AuthenticationState.AwaitingInitialization;
            
            _identityContainer = m_appUIDocument.rootVisualElement.Q<VisualElement>(k_IdentityContainerName);
            _guestContainer = m_appUIDocument.rootVisualElement.Q<VisualElement>(k_GuestContainerName);
            _guestContainer.style.display = DisplayStyle.None;
            
            _avatar = m_appUIDocument.rootVisualElement.Q<CustomAvatar>(k_AvatarName);
            _avatar.backgroundColor = new Optional<Color>(new Color(51f/255f, 110f/255f, 110f/255f, 1f));
            _avatar.style.display = DisplayStyle.None;
            
            _identityAvatarInitialLabel = _avatar.Q<Text>(k_IdentityAvatarInitialLabelName);
            _identityAvatarInitialLabel.style.display = DisplayStyle.None;

            _loginButton = _identityContainer.Q<ActionButton>(k_LoginButtonName);
            _loginButton.style.display = DisplayStyle.None;
            
            _guestButton = _identityContainer.Q<ActionButton>(k_GuestButtonName);
            
            IdentityController.AuthenticationStateChangedEvent += OnAuthenticationStateChanged;
            IdentityController.UserInfoUpdatedEvent += OnUserInfoUpdated;
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

        // unregister event handlers
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            NetworkDetector.OnNetworkStatusChanged -= OnNetworkStatusChanged;
            IdentityController.AuthenticationStateChangedEvent -= OnAuthenticationStateChanged;
            IdentityController.UserInfoUpdatedEvent -= OnUserInfoUpdated;
            
            if(NetworkDetector.IsOffline) return;
            if (_loginButton != null)
            {
                _loginButton.clicked -= OnLoginButtonPressed;
            }

            if (_logoutButton != null)
            {
                _logoutButton.clicked -= OnLogoutButtonPressed;
            }

            if (_guestButton != null)
            {
                _guestButton.clicked -= OnGuestButtonPressed;
            }

            _avatar.clicked -= OnAvatarClicked;
        }
        
        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene == gameObject.scene)
            {
                if (!_avatar.ClassListContains(k_CursorPointerClass))
                {
                    _avatar.AddToClassList(k_CursorPointerClass);
                }
            }
            else
            {
                if(_avatar.ClassListContains(k_CursorPointerClass))
                {
                    _avatar.RemoveFromClassList(k_CursorPointerClass);
                }
            }
        }

        private void OnNetworkStatusChanged(bool connected)
        {
            if (!m_Initialized)
            {
                m_Initialized = true;
                if (!connected)
                {
                    _loginButton.style.display = DisplayStyle.None;
                    _guestContainer.style.display = DisplayStyle.None;
                }
                else
                {
                    _loginButton.clicked += OnLoginButtonPressed;

                    _guestButton.clicked += OnGuestButtonPressed;

                    _avatar.clicked -= OnAvatarClicked;
                    _avatar.clicked += OnAvatarClicked;
                }
            }
            else
            {
                switch (_currentState)
                {
                    case AuthenticationState.LoggedOut:
                        if (SceneManager.GetActiveScene() == gameObject.scene)
                        {
                            _identityContainer.style.display = DisplayStyle.Flex;
                        }
                        OnAuthenticationStateChanged(_currentState);
                        break;
                }
            }

            if (connected && m_AttemptToLoginAfterReconnect)
            {
                m_AttemptToLoginAfterReconnect = false;
                m_LoginAction?.Invoke();
            }
        }

        // handle avatar click event
        private void OnAvatarClicked()
        {
            switch (_currentState)
            {
                case AuthenticationState.AwaitingInitialization:
                case AuthenticationState.AwaitingLogin:
                case AuthenticationState.AwaitingLogout:
                    return;
                
                default:
                    if(SceneManager.GetActiveScene() != gameObject.scene) return;
                    if (_identityPopover != null)
                    {
                        _identityPopover.Dismiss();
                        return;
                    }
                    var identityPopover = m_IdentityPopoverTemplate.Instantiate();
                    _logoutButton = identityPopover.Q<ActionButton>(k_LogoutButtonName);
                    
                    _logoutButton.clicked += OnLogoutButtonPressed;
                    
                    _identityPopover = Popover.Build(_avatar, identityPopover)
                        .SetOutsideClickDismiss(true)
                        .SetArrowVisible(false)
                        .SetPlacement(PopoverPlacement.TopRight)
                        .SetOffset(7)
                        .SetCrossOffset(-8);

                    _identityPopover.dismissed += OnPopoverDismissed;
                    _identityPopover.Show();
                    break;
            }
        }
        
        private void OnPopoverDismissed(Popover popover, DismissType type)
        {
            _identityPopover.dismissed -= OnPopoverDismissed;
            _identityPopover = null;
            _logoutButton.clicked -= OnLogoutButtonPressed;
        }

        private void OnUserInfoUpdated(IUserInfo info)
        {
            _avatar.style.display = DisplayStyle.Flex;
            _identityAvatarInitialLabel.style.display = DisplayStyle.Flex;
            _identityAvatarInitialLabel.text = IdentityController.GuestMode? "G" : IdentityController.GetInitials(info.Name);
        }

        // handle logout button click event
        private async void OnLogoutButtonPressed()
        {
            _identityPopover?.Dismiss();

            var logoutMessage = new IdentityController.LogoutMessage();
            IdentityController.GetLogoutMessage?.Invoke(logoutMessage);
            
            var logoutDialog = new AlertDialog()
            {
                title = await m_LocalizedLogoutTitle.GetTitleLocalizedStringForAppUIAsync(),
                variant = AlertSemantic.Default,
                description = string.IsNullOrEmpty(logoutMessage.Value)? await m_LocalizedLogoutDescription.GetTitleLocalizedStringForAppUIAsync(): logoutMessage.Value
            };
            var primaryActionTitle = await m_LocalizedLogout.GetTitleLocalizedStringForAppUIAsync();
            logoutDialog.SetPrimaryAction(99, primaryActionTitle, () =>
            {
                IdentityController.TriggerLogout?.Invoke(false);
            });
            var secondaryActionTitle = await m_LocalizedLogoutCompletely.GetTitleLocalizedStringForAppUIAsync();
            logoutDialog.SetSecondaryAction(98, secondaryActionTitle, () =>
            {
                //Use true to clear cache on browser too when logging out, and this will force the user to log in using the browser again on the next session
                IdentityController.TriggerLogout?.Invoke(true);
            });
            
            logoutDialog.SetCancelAction(0, await m_LocalizedCancel.GetTitleLocalizedStringForAppUIAsync());
            
            var logoutModal = Modal.Build(_logoutButton, logoutDialog);

            logoutModal.Show();
        }
        
        private void OnGuestButtonPressed()
        {
            CheckModeAndLogin(GuestLogin);
        }

        private void GuestLogin()
        {
            _identityPopover?.Dismiss();
            IdentityController.TriggerGuestLogin?.Invoke();
        }

        private void OnLoginButtonPressed()
        {
            if (_currentState == AuthenticationState.AwaitingLogin)
            {
                IdentityController.TriggerCancelLogin?.Invoke();
                return;
            }
            CheckModeAndLogin(Login);
        }

        private async void CheckModeAndLogin(Action loginAction)
        {
            if(_currentState == AuthenticationState.AwaitingInitialization || _currentState == AuthenticationState.LoggedIn) return;
            
            if (NetworkDetector.IsOffline)
            {
                if (!NetworkDetector.RequestedOfflineMode)
                {
                    return;
                }

                m_AttemptToLoginAfterReconnect = false;
                
                var offlineDialog = new AlertDialog()
                {
                    title = await m_OfflineModeTitle.GetTitleLocalizedStringForAppUIAsync(),
                    description = await m_LoginInOfflineModeDescription.GetTitleLocalizedStringForAppUIAsync(),
                    variant = AlertSemantic.Default
                };
                
                offlineDialog.SetPrimaryAction(97, await m_LocalizedLogin.GetTitleLocalizedStringForAppUIAsync(), () =>
                {
                    m_AttemptToLoginAfterReconnect = true;
                    m_LoginAction = loginAction;
                    NetworkDetector.RequestedOfflineMode = false;
                });
                
                offlineDialog.SetCancelAction(98, await m_LocalizedCancel.GetTitleLocalizedStringForAppUIAsync());
                
                var offlineModal = Modal.Build(_loginButton, offlineDialog);
                offlineModal.Show();
                
                return;
            }
            
            loginAction?.Invoke();
        }
        
        private void Login()
        {
            m_AttemptToLoginAfterReconnect = false;
            
            _identityPopover?.Dismiss();
            
            _guestContainer.style.display = DisplayStyle.None;
            
            IdentityController.TriggerLogin?.Invoke();
        }

        // update the UI based on the authentication state
        private async void OnAuthenticationStateChanged(AuthenticationState state)
        {
            _currentState = state;
            switch (state)
            {
                case AuthenticationState.LoggedOut:
                    if (SceneManager.GetActiveScene() == gameObject.scene)
                    {
                        _identityContainer.style.display = DisplayStyle.Flex;
                    }
                    _avatar.style.display = DisplayStyle.None;
                    _identityAvatarInitialLabel.style.display = DisplayStyle.None;
                    _loginButton.accent = true;
                    _loginButton.selected = true;
                    _loginButton.SetEnabled(true);
                    _loginButton.style.display = DisplayStyle.Flex;
#if ENABLE_GUEST_MODE
                    if (PlatformServices.ServiceAccountCredentials != null)
                    {
                        _guestContainer.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        Debug.LogError("You have enabled Guest Mode but the Service Account is not set up.");
                    }
#endif
                    _loginButton.label = await m_LocalizedLogin.GetTitleLocalizedStringForAppUIAsync();
                    break;
                
                case AuthenticationState.AwaitingLogin:
                    _avatar.style.display = DisplayStyle.None;
                    _loginButton.style.display = DisplayStyle.Flex;
                    //_guestContainer.style.display = DisplayStyle.None;
                    _loginButton.accent = false;
                    _loginButton.selected = false;
                    
#if UNITY_WEBGL
                    _loginButton.label = await m_LocalizedAwaiting.GetTitleLocalizedStringForAppUIAsync();
                    _loginButton.SetEnabled(false);
#else
                    _loginButton.label = await m_LocalizedCancel.GetTitleLocalizedStringForAppUIAsync();
                    _loginButton.SetEnabled(true);
#endif
                    
                    break;
                
                case AuthenticationState.AwaitingLogout:
                    _avatar.style.display = DisplayStyle.None;
                    _loginButton.style.display = DisplayStyle.None;
                    _guestContainer.style.display = DisplayStyle.None;
                    break;
                
                case AuthenticationState.LoggedIn:
                    _avatar.style.display = DisplayStyle.None;
                    _loginButton.style.display = DisplayStyle.None;
                    _guestContainer.style.display = DisplayStyle.None;
                    break;

                default:
                case AuthenticationState.AwaitingInitialization:
                    _avatar.style.display = DisplayStyle.None;
                    _loginButton.style.display = DisplayStyle.Flex;
                    _loginButton.SetEnabled(false);
                    _loginButton.accent = false;
                    _loginButton.selected = false;
                    _loginButton.label = await m_LocalizedInitializing.GetTitleLocalizedStringForAppUIAsync();
                    _guestContainer.style.display = DisplayStyle.None;
                    break;
            }
        }
    }
}

using UnityEngine;
using Unity.Services.Vivox;
using System.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using Unity.Industry.Viewer.Shared;
using Unity.Cloud.Identity;

namespace Unity.Industry.Viewer.Vivox
{
    public class VivoxWebInitializer: MonoBehaviour
    {
#if UNITY_WEBGL
        bool _initializedChannel = false;
        
        ICompositeAuthenticator _authenticator => PlatformServices.CompositeAuthenticator;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private IEnumerator Start()
        {
            while (_authenticator == null)
            {
                yield return null;
            }
            //Wait one frame to ensure that PlatformServicesInitialization has started
            _authenticator.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        private void OnAuthenticationStateChanged(AuthenticationState obj)
        {
            if (obj != AuthenticationState.LoggedIn) return;
            _authenticator.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            _ = Initialize();
        }

        private async Task Initialize()
        {
            if(_initializedChannel) return;
            if (UnityServices.State != ServicesInitializationState.Initialized &&
                UnityServices.State != ServicesInitializationState.Initializing)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();
            
            await VivoxService.Instance.InitializeAsync();

            if (!VivoxService.Instance.IsLoggedIn)
            {
                var options = new LoginOptions
                {
                    DisplayName = AuthenticationService.Instance.PlayerId,
                    EnableTTS = false
                };
                VivoxService.Instance.LoggedIn += InstanceOnLoggedIn;
                await VivoxService.Instance.LoginAsync(options);
                return;
            }
            
            _ = InitializeChannel();
            return;
            
            void InstanceOnLoggedIn()
            {
                VivoxService.Instance.LoggedIn -= InstanceOnLoggedIn;
                _ = InitializeChannel();
            }
        }

        private async Task InitializeChannel()
        {
            if(_initializedChannel)return;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            VivoxService.Instance.ChannelJoined += OnChannelJoined;
            await VivoxService.Instance.JoinGroupChannelAsync(System.Guid.NewGuid().ToString(), ChatCapability.AudioOnly,
                new ChannelOptions()
                {
                    MakeActiveChannelUponJoining = false
                });
        }

        private void OnChannelJoined(string obj)
        {
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            _initializedChannel = true;
            _ = LeaveSession();
        }

        private async Task LeaveSession()
        {
            VivoxService.Instance.ChannelLeft += LocalChannelLeft;
            await VivoxService.Instance.LeaveAllChannelsAsync();
            
            return;
            
            void LocalChannelLeft(string obj)
            {
                VivoxService.Instance.ChannelLeft -= LocalChannelLeft;
                _ = Logout();
            }

            async Task Logout()
            {
                await VivoxService.Instance.LogoutAsync();
            }
        }
#endif
    }
}

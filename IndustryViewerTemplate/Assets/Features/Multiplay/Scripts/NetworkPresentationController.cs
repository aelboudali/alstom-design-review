using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Unity.Industry.Viewer.Multiplay
{
    public class NetworkPresentationController : NetworkBehaviour
    {
        public NetworkVariable<bool> IsInPresentation = new NetworkVariable<bool>();
        public NetworkVariable<ulong> PresenterId = new NetworkVariable<ulong>();

        public override void OnNetworkSpawn()
        {
            NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            IsInPresentation.OnValueChanged += OnValueChanged;
            MultiplayController.InitializePresentationMode += InitializePresentationMode;
            MultiplayController.OnClientDisconnected += OnClientDisconnected;
            MultiplayController.EndPresentation += EndPresentation;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
            IsInPresentation.OnValueChanged -= OnValueChanged;
            MultiplayController.OnClientDisconnected -= OnClientDisconnected;
            MultiplayController.InitializePresentationMode -= InitializePresentationMode;
            MultiplayController.EndPresentation -= EndPresentation;
        }

        private void OnClientDisconnected(ulong obj)
        {
            if (IsInPresentation.Value && PresenterId.Value == obj)
            {
                StartCoroutine(WaitForRefresh());
            }

            IEnumerator WaitForRefresh()
            {
                yield return null;
                ResetPresenterRpc();
            }
        }

        private void InitializePresentationMode()
        {
            RequestPresentationRpc(NetworkManager.Singleton.LocalClientId);
        }
        
        private void EndPresentation(ulong clientToLeavePresentation)
        {
            if (!IsInPresentation.Value || PresenterId.Value != clientToLeavePresentation) return;
            ResetPresenterRpc();
        }

        [Rpc(SendTo.Authority, Delivery = RpcDelivery.Reliable)]
        private void ResetPresenterRpc()
        {
            if(!IsOwner) return;
            IsInPresentation.Value = false;
            IsInPresentation.CheckDirtyState();
            PresenterId.Value = 0;
        }
        
        private void OnValueChanged(bool previousValue, bool newValue)
        {
            if(newValue) return;
            // If the presentation is ended, reset the presenter ID
            MultiplayController.EndPresentation?.Invoke(NetworkManager.Singleton.LocalClientId);
        }

        [Rpc(SendTo.Authority, Delivery = RpcDelivery.Reliable)]
        private void RequestPresentationRpc(ulong playerId)
        {
            if(IsInPresentation.Value) return;
            IsInPresentation.Value = true;
            IsInPresentation.CheckDirtyState();
            PresenterId.Value = playerId;
            var playerControllers = FindObjectsByType<NetworkPlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var playerController in playerControllers)
            {
                if(playerController.OwnerClientId != playerId) continue;
                playerController.SetPresenterRpc();
            }
        }
        
        private void OnSessionOwnerPromoted(ulong sessionownerpromoted)
        {
            if (NetworkManager.LocalClientId == sessionownerpromoted)
            {
                NetworkObject.ChangeOwnership(sessionownerpromoted);
            }
        }
    }
}

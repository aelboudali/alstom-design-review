using System;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Industry.Viewer.Vivox
{
    public class VivoxSyncAvatar : NetworkBehaviour
    {
        private NetworkVariable<float> m_VoiceLevel = new NetworkVariable<float>();
        
        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] Transform m_HeadTransform;

        /// <summary>
        /// The head renderer.
        /// </summary>
        [SerializeField] SkinnedMeshRenderer m_HeadRend;
        
        /// <summary>
        /// The voice amplitude curve.
        /// </summary>
        [SerializeField] AnimationCurve m_VoiceCurve;
        
        /// <summary>
        /// The voice destination volume.
        /// </summary>
        float m_VoiceDestinationVolume;
        
        [SerializeField] float m_MouthBlendSmoothing = 5.0f;
        
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            VivoxController.ParticipantAudioEnergyChanged += OnParticipantAudioEnergyChanged;
        }

        private void Update()
        {
            m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_VoiceLevel.Value, Time.deltaTime * m_MouthBlendSmoothing));
            float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
            m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            VivoxController.ParticipantAudioEnergyChanged -= OnParticipantAudioEnergyChanged;
        }

        private void OnParticipantAudioEnergyChanged(double energy)
        {
            if(!IsOwner) return;
            m_VoiceLevel.Value = (float)energy;
            m_VoiceLevel.CheckDirtyState();
        }
    }
}

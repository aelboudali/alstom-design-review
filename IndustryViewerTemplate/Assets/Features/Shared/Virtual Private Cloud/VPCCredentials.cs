using UnityEngine;

namespace Unity.Industry.Viewer.Shared
{
    [CreateAssetMenu(fileName = "VPCCredentials", menuName = "IVT/Assets/VPC Credentials")]
    public class VPCCredentials : ScriptableObject
    {
        public string DomainName => domainName;
        [SerializeField] private string domainName;
        public string OpenIdConfigurationUrl => openIdConfigurationUrl;
        [SerializeField] private string openIdConfigurationUrl;
        public string PathPrefixValue => pathPrefixValue;
        [SerializeField] private string pathPrefixValue;
    }
}
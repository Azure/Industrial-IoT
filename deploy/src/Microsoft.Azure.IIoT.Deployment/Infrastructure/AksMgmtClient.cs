// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ContainerService.Fluent;
    using Microsoft.Azure.Management.ContainerService.Fluent.Models;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Azure.Management.OperationalInsights.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    using Microsoft.Graph;
    using Serilog;

    class AksMgmtClient : IDisposable {

        public const string DEFAULT_NAME_PREFIX = "aksCluster-";

        public const string NETWORK_PROFILE_SERVICE_CIDR = "10.0.0.0/16";
        public const string NETWORK_PROFILE_DNS_SERVICE_IP = "10.0.0.10";
        public const string NETWORK_PROFILE_DOCKER_BRIDGE_CIDR = "172.17.0.1/16";

        public const string KUBERNETES_VERSION_FALLBACK = "1.23.12";
        public const string KUBERNETES_VERSION_MAJ_MIN = "1.23";

        private readonly ContainerServiceManagementClient _containerServiceManagementClient;

        public AksMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _containerServiceManagementClient = new ContainerServiceManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        //public async Task<bool> CheckNameAvailabilityAsync(
        //    string aksClusterName,
        //    CancellationToken cancellationToken = default
        //) {
        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// Select latest patch version of Kubernetes with major and minor defined by versionMajorMinor.
        /// If function fails to parse provided versions then null will be returned.
        /// </summary>
        /// <param name="versionMajorMinor"> Minor and major versions in [major].[minor] format </param>
        /// <param name="kubernetesVersions"> List of available versions in [major].[minor].[patch] format </param>
        /// <returns></returns>
        public static string SelectLatestPatchVersion(
            string versionMajorMinor,
            IList<string> kubernetesVersions
        ) {
            if (kubernetesVersions is null || kubernetesVersions.Count == 0) {
                throw new ArgumentNullException(nameof(kubernetesVersions));
            }

            try {
                var latestVersion = kubernetesVersions
                    .Where(version => version.StartsWith($"{versionMajorMinor}."))
                    .OrderBy(version => Int32.Parse(version.Substring(versionMajorMinor.Length + 1)))
                    .Last();

                return latestVersion;
            }
            catch(FormatException) {
                Log.Warning($"Failed to parse provided Kubernetes versions.");
                return null;
            }
        }

        /// <summary>
        /// Get definition of default AKS cluster.
        /// </summary>
        /// <param name="kubernetesVersion"></param>
        /// <param name="resourceGroup"></param>
        /// <param name="aksApplication"></param>
        /// <param name="aksApplicationSecret"></param>
        /// <param name="aksClusterName"></param>
        /// <param name="sshCertificate"></param>
        /// <param name="virtualNetworkSubnet"></param>
        /// <param name="operationalInsightsWorkspace"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public ManagedClusterInner GetClusterDefinition(
            string kubernetesVersion,
            IResourceGroup resourceGroup,
            Application aksApplication,
            string aksApplicationSecret,
            string aksClusterName,
            X509Certificate2 sshCertificate,
            SubnetInner virtualNetworkSubnet,
            Workspace operationalInsightsWorkspace,
            IDictionary<string, string> tags = null
        ) {
            if (string.IsNullOrWhiteSpace(kubernetesVersion)) {
                throw new ArgumentNullException(nameof(kubernetesVersion));
            }
            if (resourceGroup is null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (aksApplication is null) {
                throw new ArgumentNullException(nameof(aksApplication));
            }
            if (string.IsNullOrWhiteSpace(aksApplicationSecret)) {
                throw new ArgumentNullException(nameof(aksApplicationSecret));
            }
            if (string.IsNullOrWhiteSpace(aksClusterName)) {
                throw new ArgumentNullException(nameof(aksClusterName));
            }
            if (sshCertificate is null) {
                throw new ArgumentNullException(nameof(sshCertificate));
            }
            if (virtualNetworkSubnet is null) {
                throw new ArgumentNullException(nameof(virtualNetworkSubnet));
            }
            if (operationalInsightsWorkspace is null) {
                throw new ArgumentNullException(nameof(operationalInsightsWorkspace));
            }

            tags ??= new Dictionary<string, string>();

            var aksDnsPrefix = aksClusterName + "-dns";
            var aksClusterX509CertificateOpenSshPublicKey = X509CertificateHelper.GetOpenSSHPublicKey(sshCertificate);

            var managedClusterDefinition = new ManagedClusterInner(
            //nodeResourceGroup: aksResourceGroupName // This is not propagated yet.
            ) {
                Location = resourceGroup.RegionName,
                Tags = tags,

                //ProvisioningState = null,
                KubernetesVersion = kubernetesVersion,
                DnsPrefix = aksDnsPrefix,
                //Fqdn = null,
                AgentPoolProfiles = new List<ManagedClusterAgentPoolProfile> {
                    new ManagedClusterAgentPoolProfile {
                        Type = AgentPoolType.VirtualMachineScaleSets,
                        Name = "agentpool",
                        Count = 2,
                        VmSize = ContainerServiceVMSizeTypes.StandardDS2V2,
                        OsDiskSizeGB = 100,
                        OsType = OSType.Linux,
                        VnetSubnetID = virtualNetworkSubnet.Id,
                        MaxPods = 40,
                        Mode = AgentPoolMode.System
                    }
                },
                LinuxProfile = new ContainerServiceLinuxProfile {
                    AdminUsername = "azureuser",
                    Ssh = new ContainerServiceSshConfiguration {
                        PublicKeys = new List<ContainerServiceSshPublicKey> {
                            new ContainerServiceSshPublicKey {
                                KeyData = aksClusterX509CertificateOpenSshPublicKey
                            }
                        }
                    }
                },
                ServicePrincipalProfile = new ManagedClusterServicePrincipalProfile {
                    ClientId = aksApplication.AppId,
                    Secret = aksApplicationSecret
                },
                AddonProfiles = new Dictionary<string, ManagedClusterAddonProfile> {
                    { "omsagent", new ManagedClusterAddonProfile {
                            Enabled = true,
                            Config = new Dictionary<string, string> {
                                { "logAnalyticsWorkspaceResourceID", operationalInsightsWorkspace.Id }
                            }
                        }
                    },
                    { "httpApplicationRouting", new ManagedClusterAddonProfile {
                            Enabled = false
                        }
                    }
                },
                //NodeResourceGroup = aksResourceGroupName, // This is not propagated yet.
                EnableRBAC = true,
                NetworkProfile = new ContainerServiceNetworkProfile {
                    NetworkPlugin = NetworkPlugin.Azure,
                    //PodCidr = "10.244.0.0/16",
                    ServiceCidr = NETWORK_PROFILE_SERVICE_CIDR,
                    DnsServiceIP = NETWORK_PROFILE_DNS_SERVICE_IP,
                    DockerBridgeCidr = NETWORK_PROFILE_DOCKER_BRIDGE_CIDR,
                    LoadBalancerSku = Management.ContainerService.Fluent.Models.LoadBalancerSku.Standard
                }
            };

            managedClusterDefinition.Validate();

            return managedClusterDefinition;
        }

        public async Task<ManagedClusterInner> CreateClusterAsync(
            IResourceGroup resourceGroup,
            string aksClusterName,
            ManagedClusterInner clusterDefinition,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information($"Creating Azure AKS cluster: {aksClusterName} ...");

                var cluster = await _containerServiceManagementClient
                    .ManagedClusters
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        aksClusterName,
                        clusterDefinition,
                        cancellationToken
                    );

                Log.Information($"Created Azure AKS cluster: {aksClusterName}");

                return cluster;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure AKS cluster: {aksClusterName}");
                throw;
            }
        }

        public async Task<ManagedClusterInner> GetClusterAsync(
            IResourceGroup resourceGroup,
            string aksClusterName,
            CancellationToken cancellationToken = default
        ) {
            return await _containerServiceManagementClient
                .ManagedClusters
                .GetAsync(
                    resourceGroup.Name,
                    aksClusterName,
                    cancellationToken
                );
        }

        public async Task<string> GetClusterAdminCredentialsAsync(
            IResourceGroup resourceGroup,
            string aksClusterName,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Verbose($"Fetching KubeConfig of Azure AKS cluster: {aksClusterName} ...");

                var aksAdminCredentials = await _containerServiceManagementClient
                    .ManagedClusters
                    .ListClusterAdminCredentialsAsync(
                        resourceGroup.Name,
                        aksClusterName,
                        cancellationToken
                    );

                var aksAdminCredential = aksAdminCredentials.Kubeconfigs.FirstOrDefault();
                var kubeConfigContent = Encoding.ASCII.GetString(aksAdminCredential.Value);

                Log.Verbose($"Fetched KubeConfig of Azure AKS cluster: {aksClusterName}");

                return kubeConfigContent;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to fetch KubeConfig of Azure AKS cluster: {aksClusterName}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _containerServiceManagementClient) {
                _containerServiceManagementClient.Dispose();
            }
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.Network.Fluent;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class NetworkMgmtClient : IDisposable
    {
        public const string DEFAULT_IOT_SERVICES_PREFIX = "iiotservices";

        public const string NETWORK_INTERFACE_PRIVATE_IP_ADDRESS = "10.240.0.4"; //"10.0.0.4";
        public const string VIRTUAL_NETWORK_ADDRESS_PREFIXES = "10.0.0.0/8";
        public const string VIRTUAL_NETWORK_AKS_SUBNET_NAME = "aks-subnet";
        public const string VIRTUAL_NETWORK_AKS_SUBNET_ADDRESS_PREFIXES = "10.240.0.0/16";
        public const string VIRTUAL_NETWORK_VM_SUBNET_NAME = "vm-subnet";
        public const string VIRTUAL_NETWORK_VM_SUBNET_ADDRESS_PREFIXES = "10.241.0.0/16";

        private readonly NetworkManagementClient _networkManagementClient;

        public NetworkMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _networkManagementClient = new NetworkManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateNetworkSecurityGroupName(string iotServicesPrefix = DEFAULT_IOT_SERVICES_PREFIX) {
            return iotServicesPrefix + "-nsg";
        }

        public static string GenerateRoutTableName(string iotServicesPrefix = DEFAULT_IOT_SERVICES_PREFIX) {
            return iotServicesPrefix + "-rt";
        }

        public static string GenerateVirtualNetworkName(string iotServicesPrefix = DEFAULT_IOT_SERVICES_PREFIX) {
            return iotServicesPrefix + "-vnet";
        }

        public static string GenerateNetworkInterfaceName(string iotServicesPrefix = DEFAULT_IOT_SERVICES_PREFIX) {
            return iotServicesPrefix + "-nic";
        }

        public static string GeneratePublicIPAddressName(string iotServicesPrefix = DEFAULT_IOT_SERVICES_PREFIX) {
            return iotServicesPrefix + "-public-ip";
        }

        /// <summary>
        /// Get network security group by its name.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="networkSecurityGroupName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<NetworkSecurityGroupInner> GetNetworkSecurityGroupAsync(
            IResourceGroup resourceGroup,
            string networkSecurityGroupName,
            CancellationToken cancellationToken = default
        ) {
            var networkSecurityGroup = await _networkManagementClient
                .NetworkSecurityGroups
                .GetAsync(
                    resourceGroup.Name,
                    networkSecurityGroupName,
                    cancellationToken: cancellationToken
                );

            return networkSecurityGroup;
        }

        public async Task<NetworkSecurityGroupInner> CreateNetworkSecurityGroupAsync(
            IResourceGroup resourceGroup,
            string networkSecurityGroupName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Network Security Group: {networkSecurityGroupName} ...");

                // Define Network Security Group
                var networkSecurityGroupDefinition = new NetworkSecurityGroupInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    SecurityRules = new List<SecurityRuleInner> {
                        new SecurityRuleInner {
                            Name = "UASC",
                            Protocol = SecurityRuleProtocol.Tcp,
                            SourcePortRange = "*",
                            DestinationPortRange = "4840",
                            SourceAddressPrefix = "*",
                            DestinationAddressPrefix = "*",
                            Access = SecurityRuleAccess.Allow,
                            Priority = 100,
                            Direction = SecurityRuleDirection.Inbound
                        },
                        new SecurityRuleInner {
                            Name = "HTTPS",
                            Protocol = SecurityRuleProtocol.Tcp,
                            SourcePortRange = "*",
                            DestinationPortRange = "443",
                            SourceAddressPrefix = "*",
                            DestinationAddressPrefix = "*",
                            Access = SecurityRuleAccess.Allow,
                            Priority = 101,
                            Direction = SecurityRuleDirection.Inbound
                        },
                        new SecurityRuleInner {
                            Name = "HTTP",
                            Protocol = SecurityRuleProtocol.Tcp,
                            SourcePortRange = "*",
                            DestinationPortRange = "80",
                            SourceAddressPrefix = "*",
                            DestinationAddressPrefix = "*",
                            Access = SecurityRuleAccess.Allow,
                            Priority = 102,
                            Direction = SecurityRuleDirection.Inbound
                        },
                        new SecurityRuleInner {
                            Name = "SSH",
                            Protocol = SecurityRuleProtocol.Tcp,
                            SourcePortRange = "*",
                            DestinationPortRange = "22",
                            SourceAddressPrefix = "*",
                            DestinationAddressPrefix = "*",
                            Access = SecurityRuleAccess.Deny,
                            Priority = 110,
                            Direction = SecurityRuleDirection.Inbound
                        }
                    }
                };

                networkSecurityGroupDefinition.Validate();

                var networkSecurityGroup = await _networkManagementClient
                    .NetworkSecurityGroups
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        networkSecurityGroupName,
                        networkSecurityGroupDefinition,
                        cancellationToken
                    );

                Log.Information($"Created Azure Network Security Group: {networkSecurityGroupName}");

                return networkSecurityGroup;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Network Security Group: {networkSecurityGroupName}");
                throw;
            }
        }

        public async Task<RouteTableInner> CreateRouteTableAsync(
            IResourceGroup resourceGroup,
            string routTableName,
            string networkInterfacePrivateIPAddress,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new Dictionary<string, string>();

            // Define Rout Table
            var routTableDefinition = new RouteTableInner {
                Location = resourceGroup.RegionName,
                Tags = tags,

                DisableBgpRoutePropagation = false,
                Routes = new List<RouteInner> {
                        new RouteInner {
                            Name = "aks-agentpool",
                            AddressPrefix = "10.244.0.0/24",
                            NextHopType = RouteNextHopType.VirtualAppliance,
                            NextHopIpAddress = networkInterfacePrivateIPAddress
                        }
                    }
            };

            var routeTable = await _networkManagementClient
                .RouteTables
                .CreateOrUpdateAsync(
                    resourceGroup.Name,
                    routTableName,
                    routTableDefinition,
                    cancellationToken
                );

            return routeTable;
        }

        /// <summary>
        /// Get virtual network by its name.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="virtualNetworkName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<VirtualNetworkInner> GetVirtualNetworkAsync(
            IResourceGroup resourceGroup,
            string virtualNetworkName,
            CancellationToken cancellationToken = default
        ) {
            var virtualNetwork = await _networkManagementClient
                .VirtualNetworks
                .GetAsync(
                    resourceGroup.Name,
                    virtualNetworkName,
                    cancellationToken: cancellationToken
                );

            return virtualNetwork;
        }

        public async Task<VirtualNetworkInner> CreateVirtualNetworkAsync(
            IResourceGroup resourceGroup,
            NetworkSecurityGroupInner networkSecurityGroup,
            string virtualNetworkName,
            RouteTableInner routeTable = null,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Virtual Network: {virtualNetworkName} ...");

                // Define Virtual Network
                var virtualNetworkDefinition = new VirtualNetworkInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    AddressSpace = new AddressSpace {
                        AddressPrefixes = new List<string>() {
                                VIRTUAL_NETWORK_ADDRESS_PREFIXES
                            }
                    },
                    Subnets = new List<SubnetInner> {
                            new SubnetInner {
                                Name = VIRTUAL_NETWORK_AKS_SUBNET_NAME,
                                AddressPrefix = VIRTUAL_NETWORK_AKS_SUBNET_ADDRESS_PREFIXES,
                                NetworkSecurityGroup = new SubResource {
                                    Id = networkSecurityGroup.Id
                                }
                            },
                            new SubnetInner {
                                Name = VIRTUAL_NETWORK_VM_SUBNET_NAME,
                                AddressPrefix = VIRTUAL_NETWORK_VM_SUBNET_ADDRESS_PREFIXES,
                                NetworkSecurityGroup = new SubResource {
                                    Id = networkSecurityGroup.Id
                                }
                            }
                        }
                };

                if (null != routeTable) {
                    virtualNetworkDefinition.Subnets[0].RouteTable = new SubResource {
                        Id = routeTable.Id
                    };
                }

                virtualNetworkDefinition.Validate();

                var virtualNetwork = await _networkManagementClient
                    .VirtualNetworks
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        virtualNetworkName,
                        virtualNetworkDefinition,
                        cancellationToken
                    );

                Log.Information($"Created Azure Virtual Network: {virtualNetworkName}");

                return virtualNetwork;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Virtual Network: {virtualNetworkName}");
                throw;
            }
        }

        public SubnetInner GetAksSubnet(VirtualNetworkInner virtualNetwork) {
            var aksSubnet = virtualNetwork
                .Subnets
                .Where(subnet => subnet.Name == VIRTUAL_NETWORK_AKS_SUBNET_NAME)
                .FirstOrDefault();

            return aksSubnet;
        }

        /// <summary>
        /// Get Public IP address by its name.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="publicIPAddressName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<PublicIPAddressInner> GetPublicIPAddressAsync(
            IResourceGroup resourceGroup,
            string publicIPAddressName,
            CancellationToken cancellationToken = default
        ) {
            var publicIPAddress = await _networkManagementClient
                .PublicIPAddresses.GetAsync(
                    resourceGroup.Name,
                    publicIPAddressName,
                    cancellationToken: cancellationToken
                );

            return publicIPAddress;
        }

        public async Task<PublicIPAddressInner> CreatePublicIPAddressAsync(
            IResourceGroup resourceGroup,
            string publicIPAddressName,
            string domainNameLabel,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new Dictionary<string, string>();

            // Define Public IP
            var publicIPAddressDefinition = new PublicIPAddressInner {
                Location = resourceGroup.RegionName,
                Tags = tags,

                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                DnsSettings = new PublicIPAddressDnsSettings {
                    DomainNameLabel = domainNameLabel
                },
                IdleTimeoutInMinutes = 4
            };

            publicIPAddressDefinition.Validate();

            var publicIPAddress = await _networkManagementClient
                .PublicIPAddresses
                .CreateOrUpdateAsync(
                    resourceGroup.Name,
                    publicIPAddressName,
                    publicIPAddressDefinition,
                    cancellationToken
                );

            return publicIPAddress;
        }

        public async Task<NetworkInterfaceInner> CreateNetworkInterfaceAsync(
            IResourceGroup resourceGroup,
            NetworkSecurityGroupInner networkSecurityGroup,
            SubnetInner aksSubnet,
            string networkInterfaceName,
            string networkInterfacePrivateIPAddress,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            tags ??= new Dictionary<string, string>();

            // Define Network Interface
            var networkInterfaceDefinition = new NetworkInterfaceInner {
                Location = resourceGroup.RegionName,
                Tags = tags,

                IpConfigurations = new List<NetworkInterfaceIPConfigurationInner> {
                        new NetworkInterfaceIPConfigurationInner {
                            Name = "ipconfig1",
                            PrivateIPAddress = networkInterfacePrivateIPAddress,
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            //PublicIPAddress = new SubResource {
                            //    Id = publicIPAddress.Id
                            //},
                            Subnet = new SubResource {
                                Id = aksSubnet.Id
                            }
                        }
                    },
                NetworkSecurityGroup = new SubResource {
                    Id = networkSecurityGroup.Id
                },
                EnableAcceleratedNetworking = true,
                EnableIPForwarding = true
            };

            networkInterfaceDefinition.Validate();

            var networkInterface = await _networkManagementClient
                .NetworkInterfaces
                .CreateOrUpdateAsync(
                    resourceGroup.Name,
                    networkInterfaceName,
                    networkInterfaceDefinition,
                    cancellationToken
                );

            return networkInterface;
        }

        public void Dispose() {
            if (null != _networkManagementClient) {
                _networkManagementClient.Dispose();
            }
        }
    }
}

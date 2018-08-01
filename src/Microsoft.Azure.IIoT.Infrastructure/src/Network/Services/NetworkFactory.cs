// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Network.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.Network.Fluent;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class NetworkFactory : BaseFactory, INetworkFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public NetworkFactory(ICredentialProvider creds, ILogger logger) :
            base (creds, logger) {
        }

        /// <inheritdoc/>
        public async Task<INetworkResource> GetAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            var client = await CreateClientAsync(resourceGroup);

            var network = await client.Networks.GetByResourceGroupAsync(
                resourceGroup.Name, name);
            if (network == null) {
                return null;
            }
            return new NetworkResource(this, resourceGroup, network, _logger);
        }

        /// <inheritdoc/>
        public async Task<INetworkResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name,
            string addressSpace, bool secure) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (addressSpace == null) {
                addressSpace = "172.16.0.0/16";
            }

            var client = await CreateClientAsync(resourceGroup);
            name = await client.Networks.SelectResourceNameAsync(resourceGroup.Name,
                "net", name);

            var region = await resourceGroup.Subscription.GetRegionAsync();
            _logger.Info($"Trying to create network {name}...", () => { });

            var nsg = await client.NetworkSecurityGroups
                .Define(name)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroup.Name)
                    .CreateAsync();

            var networkDefinition = client.Networks
                .Define(name)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroup.Name)
                    .WithAddressSpace(addressSpace);
            if (secure) {
                networkDefinition = networkDefinition.DefineSubnet("subnet")
                    .WithAddressPrefix(addressSpace)
                    .WithExistingNetworkSecurityGroup(nsg)
                    .Attach();
            }
            var network = await networkDefinition.CreateAsync();
            _logger.Info($"Created network {name}.", () => { });
            return new NetworkResource(this, resourceGroup, network, _logger);
        }

        /// <summary>
        /// Virtual machine resource
        /// </summary>
        private class NetworkResource : INetworkResource {

            /// <summary>
            /// Create iot hub
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="network"></param>
            public NetworkResource(NetworkFactory manager,
                IResourceGroupResource resourceGroup, INetwork network,
                ILogger logger) {

                _resourceGroup = resourceGroup;
                _network = network;
                _manager = manager;
                _logger = logger;
            }

            /// <inheritdoc/>
            public string Name => _network.Name;

            /// <inheritdoc/>
            public IEnumerable<string> AddressSpaces => _network.AddressSpaces;

            /// <inheritdoc/>
            public string Subnet => _network.Subnets.FirstOrDefault().Key;

            /// <inheritdoc/>
            public async Task TryEnableInboundPortAsync(int port) {
                foreach (var subnet in _network.Subnets) {
                    var nsg = subnet.Value.GetNetworkSecurityGroup();
                    if (nsg == null || nsg.SecurityRules.ContainsKey($"Allow{port}In")) {
                        continue;
                    }
                    try {
                        nsg = await nsg
                            .Update()
                            .DefineRule($"Allow{port}In")
                                .AllowInbound()
                                    .FromAnyAddress()
                                    .FromAnyPort()
                                    .ToAnyAddress()
                                    .ToPort(port)
                                    .WithProtocol(SecurityRuleProtocol.Tcp)
                                .WithPriority(4000)
                                .Attach()
                            .ApplyAsync();
                    }
                    catch (Exception ex) {
                        _logger.Error($"Failed to enable port {port} in {subnet.Key}",
                            () => ex);
                    }
                }
            }

            /// <inheritdoc/>
            public async Task TryDisableInboundPortAsync(int port) {
                foreach (var subnet in _network.Subnets) {
                    var nsg = subnet.Value.GetNetworkSecurityGroup();
                    if (nsg == null || !nsg.SecurityRules.ContainsKey($"Allow{port}In")) {
                        return;
                    }
                    try {
                        nsg = await nsg
                            .Update()
                                .WithoutRule($"Allow{port}In")
                            .ApplyAsync();
                    }
                    catch (Exception ex) {
                        _logger.Error($"Failed to disable port {port} in {subnet.Key}",
                            () => ex);
                    }
                }
            }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Info($"Deleting network {_network.Id}...", () => { });
                await _manager.TryDeleteNetworkAsync(_resourceGroup, _network.Id);
                _logger.Info($"Network {_network.Id} deleted.", () => { });
            }

            private readonly IResourceGroupResource _resourceGroup;
            private readonly INetwork _network;
            private readonly ILogger _logger;
            private readonly NetworkFactory _manager;
        }

        /// <summary>
        /// Delete all vm resources if possible
        /// </summary>
        /// <returns></returns>
        public async Task TryDeleteNetworkAsync(
            IResourceGroupResource resourceGroup, string id) {
            var client = await CreateClientAsync(resourceGroup);
            await Try.Async(() => client.Networks.DeleteByIdAsync(id));
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.Compute.Fluent;
    using Microsoft.Azure.Management.Compute.Fluent.Models;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class VirtualMachineFactory : IVirtualMachineFactory {


        /// <summary>
        /// Create virtual machine factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public VirtualMachineFactory(ICredentialProvider creds,
            ISubscriptionInfoSelector selector, ILogger logger) :
            this (creds, selector, null, logger) {
        }

        /// <summary>
        /// Create virtual machine factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public VirtualMachineFactory(ICredentialProvider creds,
            ISubscriptionInfoSelector selector, IShellFactory shell,
            ILogger logger) {
            _creds = creds ??
                throw new ArgumentNullException(nameof(creds));
            _shell = shell ??
                throw new ArgumentNullException(nameof(shell));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _selector = selector ??
                throw new ArgumentNullException(nameof(selector));
        }

        /// <inheritdoc/>
        public async Task<IVirtualMachineResource> GetAsync(
            IResourceGroupResource resourceGroup, string name) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            var client = await CreateClientAsync(resourceGroup);

            var vm = await client.VirtualMachines.GetByResourceGroupAsync(
                resourceGroup.Name, name);

            // TODO: Create a root user/pw to access vm using ssh.
            var user = VirtualMachineResource.kDefaultUser;
            var pw = VirtualMachineResource.kDefaultPassword;

            return new VirtualMachineResource(this, resourceGroup, vm,
                user, pw, _logger);
        }

        /// <inheritdoc/>
        public async Task<IVirtualMachineResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name, string customData) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            var client = await CreateClientAsync(resourceGroup);

            // Check name - null means we need to create one
            if (string.IsNullOrEmpty(name)) {
                while (true) {
                    name = StringEx.CreateUnique(10, "vm");
                    var exists = await client.VirtualMachines.ContainsAsync(
                        resourceGroup.Name, name);
                    if (!exists) {
                        break;
                    }
                }
            }
            else {
                var exists = await client.VirtualMachines.ContainsAsync(
                    resourceGroup.Name, name);
                if (exists) {
                    throw new ArgumentException("vm exists with this name",
                        nameof(name));
                }
            }

            // TODO: Create a root user/pw to access vm using ssh.
            var user = VirtualMachineResource.kDefaultUser;
            var pw = VirtualMachineResource.kDefaultPassword;

            var attempt = 0;
            while (true) {
                var regionAndVmSize = await SelectRegionAndVmSizeAsync(
                    resourceGroup, client);
                if (regionAndVmSize == null) {
                    throw new ExternalDependencyException("No sizes available.");
                }
                var region = regionAndVmSize.Item1;
                var vmSize = regionAndVmSize.Item2;
                try {
                    var network = client.Networks
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name)
                            .WithAddressSpace("172.16.0.0/16");

                    var publicIP = client.PublicIPAddresses
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name)
                            .WithLeafDomainLabel("a" + name);

                    var nic = client.NetworkInterfaces
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name)
                            .WithNewPrimaryNetwork(network)
                            .WithPrimaryPrivateIPAddressDynamic()
                            .WithNewPrimaryPublicIPAddress(publicIP);

                    var machine = client.VirtualMachines
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name)
                            .WithNewPrimaryNetworkInterface(nic)
                            // .WithPopularLinuxImage(
                            // KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                            .WithLatestLinuxImage("Canonical", "UbuntuServer", "16.04.0-LTS")
                                .WithRootUsername(user)
                                .WithRootPassword(pw);

                    _logger.Info($"#{attempt} trying to create vm {name}" +
                        $" on {vmSize}...", () => { });
                    IVirtualMachine vm = null;
                    if (!string.IsNullOrEmpty(customData)) {
                        vm = await machine
                                .WithCustomData(customData)
                                .WithSize(vmSize)
                            .CreateAsync();
                        _logger.Info($"Starting vm {name} ...", () => { });
                        // Restart for changes to go into effect
                        await vm.RestartAsync();
                    }
                    else {
                        vm = await machine.WithSize(vmSize).CreateAsync();
                    }
                    _logger.Info($"Created vm {name} - " +
                        $"{vm.GetPrimaryPublicIPAddress().IPAddress}.", () => { });
                    return new VirtualMachineResource(this, resourceGroup, vm,
                        user, pw, _logger);
                }
                catch (Exception ex) {
                    _logger.Info($"#{attempt} failed creating vm {name} on {vmSize}...",
                        () => ex);
                    await TryDeleteResourcesAsync(resourceGroup, name);
                    if (++attempt == 3) {
                        throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// Virtual machine resource
        /// </summary>
        private class VirtualMachineResource : IVirtualMachineResource {

            public const string kDefaultUser = "sshuser";
            public const string kDefaultPassword = "Passw0rdPassw0rd";

            /// <summary>
            /// Create iot hub
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="vm"></param>
            /// <param name="user"></param>
            /// <param name="password"></param>
            public VirtualMachineResource(VirtualMachineFactory manager,
                IResourceGroupResource resourceGroup, IVirtualMachine vm,
                string user, string password, ILogger logger) {

                _resourceGroup = resourceGroup;
                _vm = vm;
                _manager = manager;
                _logger = logger;

                User = user;
                Password = password;
            }

            /// <inheritdoc/>
            public string Name => _vm.Name;

            /// <inheritdoc/>
            public string User { get; }

            /// <inheritdoc/>
            public string Password { get; }

            /// <inheritdoc/>
            public string IPAddress =>
                _vm.GetPrimaryPublicIPAddress().IPAddress;

            /// <inheritdoc/>
            public Task<ISecureShell> OpenShellAsync(int port,
                CancellationToken ct) => _manager._shell?.OpenSecureShellAsync(
                    IPAddress, port, User, Password, ct) ??
                        Task.FromResult<ISecureShell>(null);

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Info($"Deleting VM {_vm.Id}...", () => { });
                await _manager.TryDeleteResourcesAsync(_resourceGroup, _vm.Id);
                _logger.Info($"VM {_vm.Id} deleted.", () => { });
            }

            public Task RestartAsync() =>
                _vm.RestartAsync();

            private readonly IResourceGroupResource _resourceGroup;
            private readonly IVirtualMachine _vm;
            private readonly ILogger _logger;
            private readonly VirtualMachineFactory _manager;
        }

        /// <summary>
        /// Delete all vm resources if possible
        /// </summary>
        /// <returns></returns>
        public async Task TryDeleteResourcesAsync(
            IResourceGroupResource resourceGroup, string id) {
            var client = await CreateClientAsync(resourceGroup);

            await Try.Async(() => client.VirtualMachines.DeleteByIdAsync(id));
            await Try.Async(() => client.Networks.DeleteByIdAsync(id));
            await Try.Async(() => client.PublicIPAddresses.DeleteByIdAsync(id));
        }

        /// <summary>
        /// Helper to create new client
        /// </summary>
        /// <returns></returns>
        private async Task<IAzure> CreateClientAsync(
            IResourceGroupResource resourceGroup) {
            var environment = await resourceGroup.Subscription.GetAzureEnvironmentAsync();
            var subscriptionId = await resourceGroup.Subscription.GetSubscriptionId();
            var credentials = await _creds.GetAzureCredentialsAsync(environment);
            return Azure
                .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);
        }

        /// <summary>
        /// Select virtual machine size
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task<Tuple<string, string>> SelectRegionAndVmSizeAsync(
            IResourceGroupResource resourceGroup, IAzure client) {
            var skus = await client.ComputeSkus.ListByResourceTypeAsync(
                ComputeResourceType.VirtualMachines);
            var all = skus
                .Where(s => !(s.Restrictions?.Any() ?? false))
                .Where(s => Satisfies(s.Capabilities))
                .SelectMany(s => s.Regions, Tuple.Create)
                .GroupBy(k => k.Item2.Name, k => k.Item1)
                .ToDictionary(k => k.Key, v => v);
            var selected = await resourceGroup.Subscription.GetRegion();
            while (true) {
                if (all.TryGetValue(selected, out var skuSelections)) {
                    // Region is supported select size
                    var sizes = skuSelections
                        .OrderBy(s => s.Costs?.Sum(c => c.Quantity ?? 0) ?? 0)
                        .Select(s => s.VirtualMachineSizeType.Value);
                    if (sizes.Any()) {
                        return Tuple.Create(selected, sizes.First());
                    }
                }
                // Select different region
                selected = _selector.SelectRegion(all.Keys.Distinct());
                if (selected == null) {
                    return null;
                }
            }
        }

    //  /// <summary>
    //  /// Helper to select vm size
    //  /// </summary>
    //  /// <param name="resourceGroup"></param>
    //  /// <param name="client"></param>
    //  /// <returns></returns>
    //  private static async Task<string> SelectVmSizeAsync2(
    //      IResourceGroupResource resourceGroup, IAzure client) {
    //      var skus = await client.ComputeSkus.ListbyRegionAndResourceTypeAsync(
    //          Region.Create(resourceGroup.Subscription.Region),
    //          ComputeResourceType.VirtualMachines);
    //      var skuSizeRestrictions = skus
    //          .Where(s => s.ResourceType == ComputeResourceType.VirtualMachines)
    //          .Where(s => s.Restrictions
    //              .Where(r => r.Type == ResourceSkuRestrictionsType.Location)
    //              .Any(r => r.Values.Contains(resourceGroup.Subscription.Region)))
    //          .ToDictionary(s => s.VirtualMachineSizeType.Value, s => s.Restrictions);
    //
    //      var sizes = await client.VirtualMachines.Sizes.ListByRegionAsync(
    //          resourceGroup.Subscription.Region);
    //      var candidates = sizes
    //          .Where(s => !skuSizeRestrictions.ContainsKey(s.Name))
    //          .Where(s => s.MemoryInMB >= 4 * 1024)
    //          .Where(s => s.NumberOfCores >= 4)
    //          .Where(s => s.OSDiskSizeInMB >= 256 * 1024)
    //          .OrderBy(s => s.MemoryInMB);
    //      var vmSize = candidates.FirstOrDefault();
    //      if (vmSize == null) {
    //          throw new ExternalDependencyException("No sizes available.");
    //      }
    //      return vmSize.Name;
    //    }

        /// <summary>
        /// Check whether the capabilities are satifying our configuration
        /// </summary>
        /// <param name="capabilities"></param>
        /// <returns></returns>
        private static bool Satisfies(IEnumerable<ResourceSkuCapabilities> capabilities) {
            foreach (var cap in capabilities) {
                if (!Satisfies(cap)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check whether the capability is satifying our configuration
        /// </summary>
        /// <param name="capabilitiy"></param>
        /// <returns></returns>
        private static bool Satisfies(ResourceSkuCapabilities capabilitiy) {
            switch (capabilitiy.Name) {
                case "OSVhdSizeMB":
                    return (double.Parse(capabilitiy.Value) >= 100 * 1024);
                case "vCPUs":
                    return (double.Parse(capabilitiy.Value) >= 4);
                case "MemoryGB":
                    return (double.Parse(capabilitiy.Value) >= 4);
                case "LowPriorityCapable":
                case "MaxDataDiskCount":
                case "MaxResourceVolumeMB":
                case "PremiumIO":
                case "MaxWriteAcceleratorDisksAllowed":
                    return true;
                default:
                    Console.WriteLine(capabilitiy.Name + "=" + capabilitiy.Value);
                    return true;
            }
        }


        private readonly ICredentialProvider _creds;
        private readonly IShellFactory _shell;
        private readonly ILogger _logger;
        private readonly ISubscriptionInfoSelector _selector;
    }
}

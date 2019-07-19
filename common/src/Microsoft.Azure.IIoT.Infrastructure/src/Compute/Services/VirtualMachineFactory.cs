// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Network;
    using Microsoft.Azure.IIoT.Infrastructure.Services;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Management.Compute.Fluent;
    using Microsoft.Azure.Management.Compute.Fluent.Models;
    using Microsoft.Azure.Management.Compute.Fluent.VirtualMachine.Definition;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.Network.Fluent;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Virtual machine factory implementation
    /// </summary>
    public class VirtualMachineFactory : BaseFactory, IVirtualMachineFactory {

        /// <summary>
        /// Create virtual machine factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="selector"></param>
        /// <param name="logger"></param>
        public VirtualMachineFactory(ICredentialProvider creds,
            ISubscriptionInfoSelector selector, ILogger logger) :
            this(creds, selector, null, logger) {
        }

        /// <summary>
        /// Create virtual machine factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="selector"></param>
        /// <param name="shell"></param>
        /// <param name="logger"></param>
        public VirtualMachineFactory(ICredentialProvider creds,
            ISubscriptionInfoSelector selector, IShellFactory shell,
            ILogger logger) : base(creds, logger) {
            _shell = shell ??
                throw new ArgumentNullException(nameof(shell));
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
            if (vm == null) {
                return null;
            }

            var user = VirtualMachineResource.DefaultUser;
            var pw = "Vm$" + name.ToSha1Hash().Substring(0, 3);

            return new VirtualMachineResource(this, resourceGroup, vm,
                user, pw, _logger);
        }

        /// <inheritdoc/>
        public async Task<IVirtualMachineResource> CreateAsync(
            IResourceGroupResource resourceGroup, string name,
            INetworkResource netres, VirtualMachineImage image,
            string customData) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }

            var client = await CreateClientAsync(resourceGroup);
            name = await client.VirtualMachines.SelectResourceNameAsync(resourceGroup.Name,
                "vm", name);

            var user = VirtualMachineResource.DefaultUser;
            var pw = "Vm$" + name.ToSha1Hash().Substring(0, 3);

            if (image == null) {
                image = KnownImages.Ubuntu_16_04_lts;
            }

            var attempt = 0;
            INetwork network = null;
            if (netres != null) {
                network = await client.Networks.GetByResourceGroupAsync(
                    resourceGroup.Name, netres.Name);
                if (network == null) {
                    throw new ArgumentException(nameof(netres));
                }
            }

            var originalRegion = await resourceGroup.Subscription.GetRegionAsync();
            while (true) {
                var regionAndVmSize = await SelectRegionAndVmSizeAsync(
                    resourceGroup, client);
                if (regionAndVmSize == null) {
                    throw new ExternalDependencyException(
                        "No sizes available.");
                }
                var region = regionAndVmSize.Item1;
                var vmSize = regionAndVmSize.Item2;
                try {
                    var nicDefine = client.NetworkInterfaces
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name);
                    var nic = nicDefine
                        .WithNewPrimaryNetwork(client.Networks
                            .Define(name)
                                .WithRegion(region)
                                .WithExistingResourceGroup(resourceGroup.Name)
                                .WithAddressSpace("172.16.0.0/16"))
                        .WithPrimaryPrivateIPAddressDynamic();

                    var ipName = await client.PublicIPAddresses.SelectResourceNameAsync(
                        resourceGroup.Name, "ip");
                    var publicIP = client.PublicIPAddresses
                        .Define(ipName)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name);

                    if (network != null && originalRegion == region) {
                        nic = nicDefine
                            .WithExistingPrimaryNetwork(network)
                                .WithSubnet(netres.Subnet)
                            .WithPrimaryPrivateIPAddressDynamic();
                    }

                    nic = nic.WithNewPrimaryPublicIPAddress(
                        client.PublicIPAddresses
                            .Define(name)
                                .WithRegion(region)
                                .WithExistingResourceGroup(resourceGroup.Name));

                    var withOs = client.VirtualMachines
                        .Define(name)
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroup.Name)
                            .WithNewPrimaryNetworkInterface(nic);

                    IWithFromImageCreateOptionsManaged machine;
                    if (image.IsLinux) {
                        machine = withOs
                            .WithLatestLinuxImage(image.Publisher,
                                    image.Offer, image.Sku)
                                .WithRootUsername(user)
                                .WithRootPassword(pw);
                    }
                    else {
                        machine = withOs
                           .WithLatestWindowsImage(image.Publisher,
                                   image.Offer, image.Sku)
                               .WithAdminUsername(user)
                               .WithAdminPassword(pw);
                    }

                    _logger.Information("Trying to create vm {name} on {vmSize}...",
                        name, vmSize);

                    IVirtualMachine vm = null;
                    if (!string.IsNullOrEmpty(customData)) {
                        vm = await machine
                                .WithCustomData(customData)
                                .WithSize(vmSize)
                            .CreateAsync();
                        _logger.Information("Starting vm {name} ...", name);
                        // Restart for changes to go into effect
                        await vm.RestartAsync();
                    }
                    else {
                        vm = await machine.WithSize(vmSize).CreateAsync();
                    }
                    _logger.Information("Created vm {name}.", name);
                    return new VirtualMachineResource(this, resourceGroup, vm,
                        user, pw, _logger);
                }
                catch (Exception ex) {
                    _logger.Information(ex,
                        "#{attempt} failed creating VM {name} as {vmSize}...",
                            attempt, name, vmSize);
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

            public const string DefaultUser = "sshuser";

            /// <summary>
            /// Create resource
            /// </summary>
            /// <param name="manager"></param>
            /// <param name="resourceGroup"></param>
            /// <param name="vm"></param>
            /// <param name="user"></param>
            /// <param name="password"></param>
            /// <param name="logger"></param>
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
            public string PublicIPAddress =>
                _vm.GetPrimaryPublicIPAddress()?.IPAddress ?? "";

            /// <inheritdoc/>
            public async Task AddPublicIPAddressAsync() {
                if (!string.IsNullOrEmpty(PublicIPAddress)) {
                    return;
                }
                var client = await _manager.CreateClientAsync(_resourceGroup);
                var publicIP = client.PublicIPAddresses
                    .Define(_vm.Name)
                        .WithRegion(_vm.Region.Name)
                        .WithExistingResourceGroup(_resourceGroup.Name);
                await _vm.GetPrimaryNetworkInterface().Update()
                    .WithNewPrimaryPublicIPAddress(publicIP)
                    .ApplyAsync();
                _logger.Information("Added public IP {address} to {name}...",
                    PublicIPAddress, _vm.Name);
            }

            /// <inheritdoc/>
            public async Task RemovePublicIPAddressAsync() {
                if (string.IsNullOrEmpty(PublicIPAddress)) {
                    return;
                }
                await _vm.GetPrimaryNetworkInterface().Update()
                    .WithoutPrimaryPublicIPAddress()
                    .ApplyAsync();
                _logger.Information("Removed public IP from {name}...", _vm.Name);
            }

            /// <inheritdoc/>
            public async Task<ISecureShell> OpenShellAsync(int port,
                CancellationToken ct) {
                if (_manager._shell != null) {
                    while (!ct.IsCancellationRequested) {
                        await Try.Async(AddPublicIPAddressAsync);
                        await Try.Async(() => EnableInboundPortAsync(port));
                        return await Try.Async(() => _manager._shell.OpenSecureShellAsync(
                               PublicIPAddress, port, User, Password, ct));
                    }
                }
                ct.ThrowIfCancellationRequested();
                throw new ExternalDependencyException("Failed to open shell");
            }

            /// <inheritdoc/>
            public async Task DeleteAsync() {
                _logger.Information("Deleting VM {vm}...", _vm.Id);
                await _manager.TryDeleteResourcesAsync(_resourceGroup, _vm.Id);
                _logger.Information("VM {vm} deleted.", _vm.Id);
            }


            /// <summary>
            /// Enable inbound port on any nic nsg
            /// </summary>
            /// <param name="port"></param>
            /// <returns></returns>
            public async Task EnableInboundPortAsync(int port) {
                var nsg = _vm.GetPrimaryNetworkInterface().GetNetworkSecurityGroup();
                if (nsg == null || nsg.SecurityRules.ContainsKey($"Allow{port}In")) {
                    return;
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

                    await _vm.GetPrimaryNetworkInterface().Update()
                        .WithExistingNetworkSecurityGroup(nsg).ApplyAsync();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to enable port {port}", port);
                }
            }

            /// <summary>
            /// Disable inbound port on any nic nsg
            /// </summary>
            /// <param name="port"></param>
            /// <returns></returns>
            public async Task DisableInboundPortAsync(int port) {
                var nsg = _vm.GetPrimaryNetworkInterface().GetNetworkSecurityGroup();
                if (nsg == null || !nsg.SecurityRules.ContainsKey($"Allow{port}In")) {
                    return;
                }
                try {
                    nsg = await nsg
                        .Update()
                            .WithoutRule($"Allow{port}In")
                        .ApplyAsync();

                    await _vm.GetPrimaryNetworkInterface().Update()
                        .WithExistingNetworkSecurityGroup(nsg).ApplyAsync();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Failed to disable port {port}", port);
                }
            }

            public Task RestartAsync() {
                return _vm.RestartAsync();
            }

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
            var selected = await resourceGroup.Subscription.GetRegionAsync();
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
                    return double.Parse(capabilitiy.Value) >= 100 * 1024;
                case "vCPUs":
                    return double.Parse(capabilitiy.Value) >= 4;
                case "MemoryGB":
                    return double.Parse(capabilitiy.Value) >= 4;
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

        private readonly IShellFactory _shell;
        private readonly ISubscriptionInfoSelector _selector;
    }
}

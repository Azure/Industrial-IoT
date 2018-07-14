// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation.Azure {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Management;
    using Microsoft.Azure.IIoT.Management.Auth;
    using Microsoft.Azure.Management.Compute.Fluent;
    using Microsoft.Azure.Management.Compute.Fluent.Models;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;
    using Renci.SshNet;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An azure vm based simulator
    /// </summary>
    public class AzureBasedSimulator : ISimulator {

        /// <summary>
        /// Create simulator
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="registry"></param>
        /// <param name="infra"></param>
        /// <param name="logger"></param>
        public AzureBasedSimulator(IConfigProvider infra, ICredentialProvider creds,
            IIoTHubTwinServices registry, ILogger logger) {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));
            _infra = infra ?? throw new ArgumentNullException(nameof(infra));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                if (_rg == null) {
                    return;
                }
                foreach (var simulation in _simulations.Values) {
                    try {
                        await simulation.CloseAsync();
                    }
                    catch {
                        continue;
                    }
                }
                _simulations.Clear();
                _logger.Info($"Deleting simulation resource group {_rg.Name}...",
                    () => { });
                await _az.ResourceGroups.DeleteByNameAsync(_rg.Name);
                _logger.Info($"Deleted simulation resource group {_rg.Name}.",
                    () => { });
            }
            finally {
                _rg = null;
                _context = null;
                _az = null;

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_rg != null) {
                    throw new InvalidOperationException("Already started");
                }

                var name = CreateUniqueName(15, "simulator-");

                _context = await _infra.GetContextAsync();

                _az = Azure
                    .Configure()
                        .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(_creds.Credentials)
                    .WithSubscription(_context.SubscriptionId);

                _logger.Info($"Creating simulation resource group {name} in " +
                    $"{_context.Region} using subscription {_context.SubscriptionId}...",
                        () => { });

                _rg = await _az.ResourceGroups
                    .Define(name)
                    .WithRegion(_context.Region)
                    .CreateAsync();

                _logger.Info($"Created simulation resource group {_rg.Name}.",
                    () => { });

                // TODO: Create iot hub

                // ----
            }
            catch {
                _rg = null;
                _context = null;
                _az = null;
                throw;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ISimulation> CreateAsync(
            Dictionary<string, JToken> tags) {
            await _lock.WaitAsync();
            try {
                if (_rg == null) {
                    throw new InvalidOperationException("Not started");
                }

                var deviceId = CreateUniqueName(12, "edge-");

                // Create identity in iothub
                var id = await _registry.CreateOrUpdateAsync(
                    new DeviceTwinModel { Id = deviceId, Tags = tags });
                var cs = await _registry.GetConnectionStringAsync(id.Id);
                _logger.Info($"Created simulation edge device {deviceId} .",
                    () => { });

                //
                // Create a root user/pw to access vm using ssh
                // Note that this is a temporary simulation and
                // not production, or else we would not choose
                // password authentication of course.
                //
                var user = "root";
                var pw = CreateUniqueName(8, "pass");
                var name = CreateUniqueName(12, "edge");

                var sizes = await _az.VirtualMachines.Sizes.ListByRegionAsync(
                    _context.Region);
                var candidates = sizes
                    .Where(s => s.MemoryInMB >= 4 * 1024)
                    .Where(s => s.NumberOfCores >= 4)
                    .Where(s => s.OSDiskSizeInMB >= 256 * 1024)
                    .OrderBy(s => s.MemoryInMB);
                try {
                    var vmSize = candidates.FirstOrDefault();
                    _logger.Info($"Creating simulation vm {deviceId} on {vmSize.Name}...",
                        () => vmSize);
                    // Create ubuntu xenial server vm
                    var vm = await _az.VirtualMachines
                        .Define(deviceId)
                           .WithRegion(_context.Region)
                           .WithExistingResourceGroup(_rg)
                           .WithNewPrimaryNetwork("10.0.0.0/28") // TODO:
                           .WithPrimaryPrivateIPAddressDynamic()
                           .WithNewPrimaryPublicIPAddress(name)
                           .WithPopularLinuxImage(
                                KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                           .WithRootUsername(user)
                           .WithRootPassword(pw)
                           .WithCustomData(CreateInitScript(cs.ToString()))
                           .WithSize(vmSize.Name)
                       .CreateAsync();
                    _logger.Info($"Created simulation vm {deviceId} .",
                        () => { });

                    // Restart for changes to go into effect
                    await vm.RestartAsync();
                    _logger.Info($"Started simulation vm {deviceId} .",
                        () => { });

                    return new VmBasedSimulation(this, deviceId, vm, user, pw);
                }
                catch (Exception ex) {
                    _logger.Error("Exception creating simulation.",
                        () => ex);
                    // Cleanup
                    await _registry.DeleteAsync(deviceId);
                    throw;
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<string>> ListAsync() =>
            Task.FromResult(_simulations.Values.Select(vm => vm.Id));

        /// <inheritdoc/>
        public Task<ISimulation> GetAsync(string id) {
            _simulations.TryGetValue(id, out var simulation);
            return Task.FromResult<ISimulation>(simulation);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id) {
            if (_simulations.TryRemove(id, out var simulation)) {
                await simulation.CloseAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose() => StopAsync().Wait();

        /// <summary>
        /// Handle to the simulation
        /// </summary>
        private class VmBasedSimulation : ISimulation {

            /// <summary>
            /// Create simulation
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="id"></param>
            /// <param name="vm"></param>
            /// <param name="user"></param>
            /// <param name="pw"></param>
            public VmBasedSimulation(AzureBasedSimulator outer,
                string id, IVirtualMachine vm, string user, string pw) {
                _outer = outer;
                _vm = vm;
                EdgeDeviceId = id;
                SshConnectionInfo = new ConnectionInfo(
                    vm.GetPrimaryPublicIPAddress().IPAddress, user,
                    new PasswordAuthenticationMethod(user, pw));

                _outer._simulations.TryAdd(_vm.Id, this);
            }

            /// <inheritdoc/>
            public string EdgeDeviceId { get; }

            /// <inheritdoc/>
            public ConnectionInfo SshConnectionInfo { get; }

            /// <inheritdoc/>
            public string Id => _vm.Id;

            /// <inheritdoc/>
            public ISimulatedDevice CreateDevice(DeviceType type,
                IConfiguration configuration) {
                throw new System.NotImplementedException();
            }

            /// <inheritdoc/>
            public Task ResetGatewayAsync() {
                return _vm.RestartAsync(); // TODO
            }

            /// <inheritdoc/>
            public Task ResetAsync() => _vm.RestartAsync();

            /// <summary>
            /// Close simulation
            /// </summary>
            /// <returns></returns>
            internal async Task CloseAsync() {
                if (_closed) {
                    return;
                }
                try {
                    await _vm.DeallocateAsync();
                    _outer._logger.Info($"simulation vm {Id} deallocated.",
                        () => { });
                    await _outer._registry.DeleteAsync(EdgeDeviceId);
                }
                catch {
                    return;
                }
                finally {
                    _closed = true;
                }
            }

            private readonly AzureBasedSimulator _outer;
            private readonly IVirtualMachine _vm;
            private bool _closed;
        }

        /// <summary>
        /// Helper to create init script
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static string CreateInitScript(string connectionString) {
            var script =
$@"#cloud-config
ssh_pwauth: yes
chpasswd:
    expire: false
ssh_deletekeys: false
apt:
  sources:
    source1: 
      source: ""deb [arch=amd64] https://packages.microsoft.com/ubuntu/16.04/prod xenial main""
      key: |
        -----BEGIN PGP PUBLIC KEY BLOCK-----
        Version: GnuPG v1.4.7 (GNU/Linux)

        mQENBFYxWIwBCADAKoZhZlJxGNGWzqV+1OG1xiQeoowKhssGAKvd+buXCGISZJwT
        LXZqIcIiLP7pqdcZWtE9bSc7yBY2MalDp9Liu0KekywQ6VVX1T72NPf5Ev6x6DLV
        7aVWsCzUAF+eb7DC9fPuFLEdxmOEYoPjzrQ7cCnSV4JQxAqhU4T6OjbvRazGl3ag
        OeizPXmRljMtUUttHQZnRhtlzkmwIrUivbfFPD+fEoHJ1+uIdfOzZX8/oKHKLe2j
        H632kvsNzJFlROVvGLYAk2WRcLu+RjjggixhwiB+Mu/A8Tf4V6b+YppS44q8EvVr
        M+QvY7LNSOffSO6Slsy9oisGTdfE39nC7pVRABEBAAG0N01pY3Jvc29mdCAoUmVs
        ZWFzZSBzaWduaW5nKSA8Z3Bnc2VjdXJpdHlAbWljcm9zb2Z0LmNvbT6JATUEEwEC
        AB8FAlYxWIwCGwMGCwkIBwMCBBUCCAMDFgIBAh4BAheAAAoJEOs+lK2+EinPGpsH
        /32vKy29Hg51H9dfFJMx0/a/F+5vKeCeVqimvyTM04C+XENNuSbYZ3eRPHGHFLqe
        MNGxsfb7C7ZxEeW7J/vSzRgHxm7ZvESisUYRFq2sgkJ+HFERNrqfci45bdhmrUsy
        7SWw9ybxdFOkuQoyKD3tBmiGfONQMlBaOMWdAsic965rvJsd5zYaZZFI1UwTkFXV
        KJt3bp3Ngn1vEYXwijGTa+FXz6GLHueJwF0I7ug34DgUkAFvAs8Hacr2DRYxL5RJ
        XdNgj4Jd2/g6T9InmWT0hASljur+dJnzNiNCkbn9KbX7J/qK1IbR8y560yRmFsU+
        NdCFTW7wY0Fb1fWJ+/KTsC4=
        =J6gs
        -----END PGP PUBLIC KEY BLOCK-----

packages:
 - moby-engine
 - moby-cli
 - iotedge

write_files:
 - path: /etc/iotedge/config.yaml
   content: |
        provisioning:
          source: ""manual""
          device_connection_string: ""{connectionString}""
        ...
";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
        }

        /// <summary>
        /// Helper to create a unique name
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private string CreateUniqueName(int len, string prefix = "") =>
            (prefix + Guid.NewGuid().ToString("N"))
                .Substring(0, Math.Min(len, 32 + prefix.Length));

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private IManagementConfig _context;
        private IResourceGroup _rg;
        private IAzure _az;

        private readonly ConcurrentDictionary<string, VmBasedSimulation> _simulations =
            new ConcurrentDictionary<string, VmBasedSimulation>();
        private readonly ICredentialProvider _creds;
        private readonly ILogger _logger;
        private readonly IIoTHubTwinServices _registry;
        private readonly IConfigProvider _infra;
    }
}
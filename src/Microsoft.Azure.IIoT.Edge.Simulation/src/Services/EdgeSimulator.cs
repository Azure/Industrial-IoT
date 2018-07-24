// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Edge.Deployment;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Infrastructure;
    using Microsoft.Azure.IIoT.Infrastructure.Compute;
    using Microsoft.Azure.IIoT.Infrastructure.Hub;
    using Microsoft.Azure.IIoT.Infrastructure.Runtime;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Rest.Azure;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge simulator factory
    /// </summary>
    public class EdgeSimulator : ISimulationProvider {

        /// <summary>
        /// Create simulator
        /// </summary>
        /// <param name="groupFactory"></param>
        /// <param name="hubFactory"></param>
        /// <param name="vmFactory"></param>
        /// <param name="persistence"></param>
        /// <param name="logger"></param>
        public EdgeSimulator(IResourceGroupFactory groupFactory,
            IIoTHubFactory hubFactory, IVirtualMachineFactory vmFactory,
            IPersistenceProvider persistence, ILogger logger) {

            _groupFactory = groupFactory ??
                throw new ArgumentNullException(nameof(groupFactory));
            _hubFactory = hubFactory ??
                throw new ArgumentNullException(nameof(hubFactory));
            _vmFactory = vmFactory ??
                throw new ArgumentNullException(nameof(vmFactory));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _persistence = persistence ??
                throw new ArgumentNullException(nameof(persistence));
        }

        /// <inheritdoc/>
        public async Task<ISimulator> CreateOrGetAsync(string name,
            bool closeOnDispose) {
            await _lock.WaitAsync();
            try {
                if (!_simulators.TryGetValue(name, out var simulator)) {
                    simulator = new AzureBasedSimulator(name, closeOnDispose,
                        this, _logger);

                    // Start simulator
                    await simulator.StartAsync();
                    _simulators.Add(name, simulator);
                }
                return simulator;
            }
            finally {
                _lock.Release();
            }
        }

        internal class AzureBasedSimulator : ISimulator {

            /// <inheritdoc/>
            public IEdgeDeploymentFactory Deployments { get; private set; }

            private IResourceGroupFactory GroupFactory => _outer._groupFactory;
            private IVirtualMachineFactory VmFactory => _outer._vmFactory;
            private IPersistenceProvider Persistence => _outer._persistence;
            private IIoTHubFactory HubFactory => _outer._hubFactory;

            /// <summary>
            /// Create new simulator
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="name"></param>
            /// <param name="logger"></param>
            public AzureBasedSimulator(string name, bool closeOnDispose,
                EdgeSimulator outer, ILogger logger) {

                _name = name ??
                    throw new ArgumentNullException(nameof(name));
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _logger = logger ??
                    throw new ArgumentNullException(nameof(logger));

                _closeOnDispose = closeOnDispose;
            }

            /// <inheritdoc/>
            public async Task CloseAsync() {
                await _lock.WaitAsync();
                try {
                    if (_group == null) {
                        // Already stopped
                        return;
                    }
                    _logger.Info($"Closing simulator {_name}...", () => { });
                    foreach (var simulation in _simulations.Values) {
                        _logger.Info($"Deleting simulation {simulation.Name}...",
                            () => { });
                        await TryDeleteAsync(simulation);
                    }
                    _simulations.Clear();
                    _logger.Info($"Deleting simulator resource group {_group.Name}...",
                        () => { });
                    await Try.Async(_group.DeleteAsync);
                    _logger.Info($"Deleted simulator resource group {_group.Name}.",
                        () => { });
                    await ClearStateAsync();
                    _logger.Info($"Simulator {_name} closed.", () => { });
                }
                finally {
                    _group = null;
                    _hub = null;
                    _client = null;
                    Deployments = null;
                    _lock.Release();
                }

                // Remove from factory
                await _outer.RemoveAsync(_name);
            }

            /// <inheritdoc/>
            public async Task<ISimulation> CreateAsync(Dictionary<string, JToken> tags) {
                await _lock.WaitAsync();
                try {
                    if (_group == null) {
                        throw new InvalidOperationException("Not started");
                    }
                    var cs = await CreateEdgeDeviceAsync(tags);
                    try {
                        // Create vm
                        var vm = await VmFactory.CreateAsync(_group, cs.DeviceId);
                        _logger.Info($"Created simulation vm {cs.DeviceId}.",
                            () => { });

                        var simulation = new EdgeSimulation(vm, cs, _client, _logger);
                        _simulations.TryAdd(cs.DeviceId, simulation);

                        // Update state
                        await WriteStateAsync();
                        return simulation;
                    }
                    catch (Exception ex) {
                        _logger.Error("Exception creating simulation.",
                            () => ex);
                        // Cleanup
                        await DeleteEdgeDeviceAsync(cs.DeviceId);
                        throw;
                    }
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public Task<IEnumerable<string>> ListAsync() =>
                Task.FromResult(_simulations.Values.Select(vm => vm.Name));

            /// <inheritdoc/>
            public Task<ISimulation> GetAsync(string id) {
                _simulations.TryGetValue(id, out var simulation);
                return Task.FromResult<ISimulation>(simulation);
            }

            /// <inheritdoc/>
            public async Task DeleteAsync(string id) {
                if (_simulations.TryRemove(id, out var simulation)) {
                    await TryDeleteAsync(simulation);
                    await WriteStateAsync();
                }
            }

            /// <summary>
            /// Start simulator
            /// </summary>
            /// <returns></returns>
            public async Task StartAsync() {
                await _lock.WaitAsync();
                try {
                    if (_group != null) {
                        throw new InvalidOperationException("Already started");
                    }
                    // Try read state
                    await ReadStateAsync();
                    if (_group != null && _hub != null) {
                        return;
                    }

                    // Create new
                    _group = await GroupFactory.CreateAsync(false);
                    try {
                        _hub = await HubFactory.CreateAsync(_group);
                        // Try write state
                        await WriteStateAsync();

                        _client = new IoTHubServiceClient(
                            _hub.PrimaryConnectionString.ToIoTHubConfig(), _logger);
                        Deployments = new EdgeDeploymentFactory(_client, _logger);
                    }
                    catch {
                        await _group.DeleteAsync();
                        throw;
                    }
                }
                catch {
                    _group = null;
                    _hub = null;
                    _client = null;
                    Deployments = null;
                    throw;
                }
                finally {
                    _lock.Release();
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                if (_closeOnDispose) {
                    CloseAsync().Wait();
                }
            }

            /// <summary>
            /// Cleanup simulation resources
            /// </summary>
            /// <param name="simulation"></param>
            /// <returns></returns>
            private async Task TryDeleteAsync(EdgeSimulation simulation) {
                await Try.Async(() => DeleteEdgeDeviceAsync(
                    simulation.EdgeDeviceId));
                await Try.Async(() => simulation.Vm.DeleteAsync());
            }

            /// <summary>
            /// Helper to create device
            /// </summary>
            /// <param name="tags"></param>
            /// <returns></returns>
            private async Task<ConnectionString> CreateEdgeDeviceAsync(
                Dictionary<string, JToken> tags) {
                var deviceId = StringEx.CreateUnique(15, "edge-sim-");
                // Create identity in our iothub
                var id = await _client.CreateOrUpdateAsync(
                    new DeviceTwinModel {
                        Id = deviceId,
                        Tags = tags,
                        Capabilities = new DeviceCapabilitiesModel {
                            IoTEdge = true
                        }
                    });
                var cs = await _client.GetConnectionStringAsync(id.Id);
                _logger.Info($"Created simulation edge device {deviceId} .",
                    () => { });
                return cs;
            }

            /// <summary>
            /// Helper to delete device
            /// </summary>
            /// <param name="deviceId"></param>
            /// <returns></returns>
            private async Task DeleteEdgeDeviceAsync(string deviceId) {
                try {
                    var registry = new IoTHubServiceClient(
                        _hub.PrimaryConnectionString.ToIoTHubConfig(), _logger);
                    // Create identity in our iothub
                    await registry.DeleteAsync(deviceId);
                    _logger.Info($"Simulation edge device {deviceId} deleted.",
                        () => { });
                }
                catch (Exception ex) {
                    _logger.Error($"Failed to cleanup edge device {deviceId}",
                        () => ex);
                    return;
                }
            }

            /// <summary>
            /// Try writing state
            /// </summary>
            /// <returns></returns>
            private async Task WriteStateAsync() {
                try {
                    // Save state
                    await Persistence.WriteAsync(new Dictionary<string, dynamic> {
                        [_name] = new {
                            Environment = await _group.Subscription.GetEnvironment(),
                            SubscriptionId = await _group.Subscription.GetSubscriptionId(),
                            Region = await _group.Subscription.GetRegion(),
                            Group = _group.Name,
                            Hub = _hub.Name,
                            Vms = _simulations.Values
                                .Select(s => s.EdgeCs.ToString())
                                .ToArray()
                        }
                    });
                }
                catch (StorageException ex) {
                    _logger.Debug("Failed to save state. Continue...", () => ex);
                }
                catch (Exception ex) {
                    _logger.Error("Fatal error saving state.", () => ex);
                    throw ex;
                }
            }

            /// <summary>
            /// Try clear state
            /// </summary>
            /// <returns></returns>
            private async Task ClearStateAsync() {
                try {
                    // Clear state
                    await Persistence.WriteAsync(new Dictionary<string, dynamic> {
                        [_name] = null
                    });
                }
                catch (StorageException) {}
                catch (Exception ex) {
                    _logger.Error("Fatal error clearing state.", () => ex);
                    throw ex;
                }
            }

            /// <summary>
            /// Try and recreate state
            /// </summary>
            /// <returns></returns>
            private async Task ReadStateAsync() {
                try {
                    var state = await Persistence.ReadAsync(_name);
                    if (state == null) {
                        return;
                    }

                    _group = await GroupFactory.GetAsync((string)state.Group,
                        new SubscriptionInfo(
                            (string)state.Environment,
                            (string)state.SubscriptionId,
                            (string)state.Region
                        ));
                    _hub = await HubFactory.GetAsync(_group, (string)state.Hub);

                    if (_group == null || _hub == null) {
                        return;
                    }

                    _client = new IoTHubServiceClient(
                        _hub.PrimaryConnectionString.ToIoTHubConfig(), _logger);
                    Deployments = new EdgeDeploymentFactory(_client, _logger);

                    foreach (var entry in state.Vms) {

                        var cs = ConnectionString.Parse((string)entry);
                        var vm = await VmFactory.GetAsync(_group, cs.DeviceId);
                        if (vm == null) {
                            continue;
                        }
                        _simulations.TryAdd(cs.DeviceId, new EdgeSimulation(vm, cs,
                            _client, _logger));
                    }

                    await WriteStateAsync();
                }
                catch (StorageException ex) {
                    _logger.Debug("Failed to recreate previous state. Clearing state...",
                        () => ex);
                    await ClearStateAsync();
                }
                catch (CloudException ex) {
                    _logger.Debug("Cloud exception during recreation. Clearing state...",
                        () => ex);
                    await ClearStateAsync();
                }
                catch (Exception ex) {
                    _logger.Error("Fatal error recreating state.", () => ex);
                    throw ex;
                }
            }

            private IResourceGroupResource _group;
            private IIoTHubResource _hub;
            private IoTHubServiceClient _client;
            private readonly ILogger _logger;
            private readonly bool _closeOnDispose;
            private readonly string _name;
            private readonly EdgeSimulator _outer;
            private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

            private readonly ConcurrentDictionary<string, EdgeSimulation> _simulations =
                new ConcurrentDictionary<string, EdgeSimulation>();
        }

        /// <summary>
        /// Remove simulator under lock
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task RemoveAsync(string name) {
            await _lock.WaitAsync();
            try {
                _simulators.Remove(name);
            }
            finally {
                _lock.Release();
            }
        }

        private readonly IResourceGroupFactory _groupFactory;
        private readonly IIoTHubFactory _hubFactory;
        private readonly IVirtualMachineFactory _vmFactory;
        private readonly ILogger _logger;
        private readonly IPersistenceProvider _persistence;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly Dictionary<string, AzureBasedSimulator> _simulators =
           new Dictionary<string, AzureBasedSimulator>();
    }
}

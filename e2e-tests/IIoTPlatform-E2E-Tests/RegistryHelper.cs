// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests
{
    using IIoTPlatformE2ETests.Deploy;
    using IIoTPlatformE2ETests.TestExtensions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Helper for managing IoT Hub device registry.
    /// </summary>
    public sealed class RegistryHelper : IDisposable
    {
        /// <summary>
        /// Constructor of RegistryHelper class.
        /// </summary>
        /// <param name="context"> Shared context for E2E tests </param>
        public RegistryHelper(IIoTPlatformTestContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RegistryManager = RegistryManager.CreateFromConnectionString(context.IoTHubConfig.IoTHubConnectionString);
        }

        /// <summary>
        /// Wait until IIoT modules are in Connected state.
        /// </summary>
        /// <param name="deviceId"> IoT Edge device id </param>
        /// <param name="ct"> Cancellation token </param>
        /// <param name="moduleNames"> List of modules to wait for, defaults to ModuleNamesDefault if not specified </param>
        /// <returns></returns>
        public async Task WaitForIIoTModulesConnectedAsync(
            string deviceId,
            CancellationToken ct,
            IReadOnlyList<string> moduleNames = null
        )
        {
            moduleNames ??= ModuleNamesDefault;
            var sw = Stopwatch.StartNew();
            try
            {
                while (true)
                {
                    var modules = (await RegistryManager.GetModulesOnDeviceAsync(deviceId, ct).ConfigureAwait(false)).ToList();
                    var connectedModulesCount = modules
                        .Where(m => moduleNames.Contains(m.Id))
                        .Count(m => m.ConnectionState == DeviceConnectionState.Connected);

                    if (connectedModulesCount == moduleNames.Count)
                    {
                        _context.OutputHelper.WriteLine($"All required IoT Edge modules are connected! (took {sw.Elapsed})");
                        return;
                    }

                    var m = modules.Count == 0 ? "No modules" : modules
                        .Select(m => $"{m.Id}({m.ConnectionState})")
                        .Aggregate((a, b) => a + ", " + b);
                    //_context.OutputHelper.WriteLine($"Waiting for IoT Edge modules: {m} on {deviceId}");
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _context.OutputHelper.WriteLine($"Waiting for IoT Edge modules to be loaded timeout timeout after {sw.Elapsed} - please check iot edge device for details");
                throw;
            }
            catch (Exception e)
            {
                _context.OutputHelper.WriteLine($"Error {e.Message} occurred while waiting for edge Modules to be loaded after {sw.Elapsed}");
                throw;
            }
        }

        /// <summary>
        /// Wait until IIoT modules do not exist anymore on edge device
        /// </summary>
        /// <param name="deviceId"> IoT Edge device id </param>
        /// <param name="ct"> Cancellation token </param>
        /// <param name="moduleNames"> List of modules to wait for, defaults to ModuleNamesDefault if not specified </param>
        /// <returns></returns>
        public async Task WaitForIIoTModulesRemovedAsync(
            string deviceId,
            CancellationToken ct,
            IReadOnlyList<string> moduleNames = null
        )
        {
            moduleNames ??= ModuleNamesDefault;
            var sw = Stopwatch.StartNew();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var modules = (await RegistryManager.GetModulesOnDeviceAsync(deviceId, ct).ConfigureAwait(false)).ToList();
                    if (!modules.Any(m => moduleNames.Contains(m.Id)))
                    {
                        _context.OutputHelper.WriteLine($"All IoT Edge modules were removed (took {sw.Elapsed})");
                        return;
                    }

                    var m = modules
                        .Select(m => $"{m.Id}({m.ConnectionState})")
                        .Aggregate((a, b) => a + ", " + b);
                    _context.OutputHelper.WriteLine($"Waiting for IoT Edge modules to undeploy: {m} on {deviceId}");
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _context.OutputHelper.WriteLine($"Waiting for IoT Edge modules to be removed timeout timeout after {sw.Elapsed} - please check iot edge device for details");
                throw;
            }
            catch (Exception e)
            {
                _context.OutputHelper.WriteLine($"Error {e.Message} occurred while waiting for edge Modules to be removed after {sw.Elapsed}");
                throw;
            }
        }

        /// <summary>
        /// Undeploy publisher
        /// </summary>
        /// <param name="messagingMode"></param>
        /// <param name="fullRedeploy"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RestartStandalonePublisherAsync(
            MessagingMode messagingMode = MessagingMode.Samples, bool fullRedeploy = false,
            CancellationToken ct = default)
        {
            var publisher = new IoTHubPublisherDeployment(_context, messagingMode);
            if (fullRedeploy)
            {
                // Delete layered edge deployment.
                await publisher.DeleteLayeredDeploymentAsync(ct);
                await TestHelper.SwitchToStandaloneModeAsync(_context, ct);

                await _context.RegistryHelper.WaitForIIoTModulesRemovedAsync(_context.DeviceConfig.DeviceId, ct,
                   new string[] { publisher.ModuleName });
            }
            else
            {
                await TestHelper.CleanPublishedNodesJsonFilesAsync(_context, ct);
                await TestHelper.RestartAsync(_context, publisher.ModuleName, ct);
            }
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context, ct);
        }

        /// <summary>
        /// Create publisher deployment
        /// </summary>
        /// <param name="messagingMode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string> DeployStandalonePublisherAsync(
            MessagingMode messagingMode = MessagingMode.Samples,
            CancellationToken ct = default)
        {
            // Create base edge deployment.
            var edgeBase = new IoTHubEdgeBaseDeployment(_context);
            var baseDeploymentResult = await edgeBase.CreateOrUpdateLayeredDeploymentAsync(ct);
            Assert.True(baseDeploymentResult, "Failed to create/update new edge base deployment.");
            _context.OutputHelper.WriteLine("Created/Updated new edge base deployment.");

            await RestartStandalonePublisherAsync(messagingMode, false, ct);
            await TestHelper.SwitchToStandaloneModeAsync(_context, ct);
            await TestHelper.CleanPublishedNodesJsonFilesAsync(_context, ct);

            // Create new layered edge deployment.
            var publisher = new IoTHubPublisherDeployment(_context, messagingMode);
            var layeredDeploymentResult = await publisher.CreateOrUpdateLayeredDeploymentAsync(ct);
            Assert.True(layeredDeploymentResult, "Failed to create/update layered deployment for publisher module.");
            _context.OutputHelper.WriteLine("Created/Updated layered deployment for publisher module.");

            // We will wait for module to be deployed.
            await _context.RegistryHelper.WaitForSuccessfulDeploymentAsync(publisher.GetDeploymentConfiguration(), ct);
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, ct,
                new string[] { publisher.ModuleName });

            // We've observed situations when even after the above waits the module did not yet restart.
            // That leads to situations where the publishing of nodes happens just before the restart to apply
            // new container creation options. After restart persisted nodes are picked up, but on the telemetry side
            // the restart causes dropped messages to be detected. That happens because just before the restart OPC Publisher
            // manages to send some telemetry. This wait makes sure that we do not run the test while restart is happening.
            // await Task.Delay(TestConstants.AwaitInitInMilliseconds, ct);

            _context.OutputHelper.WriteLine("OPC Publisher module is up and running.");

            return publisher.ModuleName;
        }

        /// <summary>
        /// Wait until one successful deployment is reported.
        /// </summary>
        /// <param name="deploymentConfiguration"></param>
        /// <param name="ct"></param>
        public async Task WaitForSuccessfulDeploymentAsync(
            Configuration deploymentConfiguration,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            Configuration lastConfiguration = null;
            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var activeConfiguration = await RegistryManager.GetConfigurationAsync(deploymentConfiguration.Id, ct)
                        .ConfigureAwait(false);
                    if (activeConfiguration != null)
                    {
                        lastConfiguration = activeConfiguration;
                        if (Equals(activeConfiguration, deploymentConfiguration)
#if SYSTEM_METRICS_BUG
                            && activeConfiguration.SystemMetrics.Results.TryGetValue("reportedSuccessfulCount", out var value)
                            && value >= 1
#endif
                                                    )
                        {
                            _context.OutputHelper.WriteLine($"All required IoT Edge modules are deployed! (took {sw.Elapsed})");
                            return;
                        }
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _context.OutputHelper.WriteLine($"Waiting for IoT Edge modules to be loaded timeout after {sw.Elapsed} - please check iot edge device for details");
                throw;
            }
            catch (Exception e)
            {
                _context.OutputHelper.WriteLine($"Error {e.Message} occurred while waiting for edge Modules after {sw.Elapsed}");
                throw;
            }
            finally
            {
                _context.OutputHelper.WriteLine($"Waiting for IoT Edge module got configuration: {JsonSerializer.Serialize(lastConfiguration, kIndented)}...");
            }
        }
        private static readonly JsonSerializerOptions kIndented = new() { WriteIndented = true };

        /// <summary>
        /// Delete deployment configuration
        /// </summary>
        /// <param name="configurationId"></param>
        /// <param name="ct"> Cancellation token </param>
        public async Task DeleteConfigurationAsync(string configurationId, CancellationToken ct = default)
        {
            while (true)
            {
                var getConfig = await RegistryManager.GetConfigurationAsync(configurationId, ct).ConfigureAwait(false);
                if (getConfig == null)
                {
                    return;
                }
                // First try create configuration
                try
                {
                    await RegistryManager.RemoveConfigurationAsync(configurationId, ct).ConfigureAwait(false);
                    _context.OutputHelper.WriteLine($"Deleted configuration {configurationId}");
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Create or update deployment configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="ct"> Cancellation token </param>
        public async Task<Configuration> CreateOrUpdateConfigurationAsync(
            Configuration configuration,
            CancellationToken ct = default
        )
        {
            try
            {
                var getConfig = await RegistryManager.GetConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);

                if (getConfig == null)
                {
                    // First try create configuration
                    try
                    {
                        _context.OutputHelper.WriteLine("Add new IoT Hub device configuration");
                        return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
                    }
                    catch (DeviceAlreadyExistsException)
                    {
                        // Technically update below should now work but for some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        _context.OutputHelper.WriteLine("IoT Hub device configuration already existed, remove and recreate it");
                        await RegistryManager.RemoveConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);
                        return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
                    }
                }

                if (Equals(configuration, getConfig))
                {
                    return getConfig;
                }

                _context.
                OutputHelper.WriteLine("Existing IoT Hub device configuration is different, remove and recreate it");
                await RegistryManager.RemoveConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);
                return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _context.OutputHelper.WriteLine("Error while creating or updating IoT Hub device configuration! {0}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Check equality of two deployment configurations.
        /// </summary>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        public static bool Equals(Configuration c0, Configuration c1)
        {
            if (c0.Id != c1.Id)
            {
                return false;
            }

            if (c0.TargetCondition != c1.TargetCondition)
            {
                return false;
            }

            if (c0.Priority != c1.Priority)
            {
                return false;
            }

            var c0ModulesContentCount = c0.Content?.ModulesContent?.Count ?? 0;
            var c1ModulesContentCount = c1.Content?.ModulesContent?.Count ?? 0;

            if (c0ModulesContentCount == 0 && c1ModulesContentCount == 0)
            {
                return true;
            }
            else if (c0ModulesContentCount != c1ModulesContentCount)
            {
                return false;
            }

            // After the previous checks we know that both have the same non-zero number of module contents.
            foreach (var moduleName in c1.Content.ModulesContent.Keys)
            {
                if (c0.Content.ModulesContent.TryGetValue(moduleName, out var moduleContent0))
                {
                    var moduleContent1 = c1.Content.ModulesContent[moduleName];

                    var diffCount = moduleContent0
                        .Count(entry => moduleContent1[entry.Key].ToString() != entry.Value.ToString());

                    if (diffCount > 0)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RegistryManager.Dispose();
        }

        /// <summary>
        /// Access To Registry Manager
        /// </summary>
        public RegistryManager RegistryManager { get; }

        /// <summary>
        /// Default value for IIoT module names.
        /// </summary>
        public static IReadOnlyList<string> ModuleNamesDefault { get; } = new string[] { "publisher" };

        private readonly IIoTPlatformTestContext _context;
    }
}

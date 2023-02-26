// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting
{
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Module host implementation
    /// </summary>
    public sealed class ModuleHost : IModuleHost, IClientAccessor
    {
        /// <inheritdoc/>
        public IClient Client { get; private set; }

        /// <summary>
        /// Create module host
        /// </summary>
        /// <param name="router"></param>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        public ModuleHost(IMethodRouter router, ISettingsRouter settings,
            IClientFactory factory, IJsonSerializer serializer, ILogger logger,
            IMetricsContext metrics)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics)))
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task StopAsync()
        {
            if (Client != null)
            {
                try
                {
                    await _lock.WaitAsync().ConfigureAwait(false);
                    if (Client != null)
                    {
                        _logger.LogInformation("Stopping Module Host...");
                        try
                        {
                            await Client.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        catch (IotHubCommunicationException) { }
                        catch (DeviceNotFoundException) { }
                        catch (UnauthorizedException) { }
                        catch (Exception se)
                        {
                            _logger.LogError(se, "Module Host not cleanly disconnected.");
                        }
                    }
                    _logger.LogInformation("Module Host stopped.");
                }
                catch (Exception ce)
                {
                    _logger.LogError(ce, "Module Host stopping caused exception.");
                }
                finally
                {
                    Client?.Dispose();
                    Client = null;
                    _reported?.Clear();
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(string type, string productInfo, string version,
            IProcessControl control)
        {
            if (Client == null)
            {
                try
                {
                    await _lock.WaitAsync().ConfigureAwait(false);
                    if (Client == null)
                    {
                        // Create client
                        _logger.LogDebug("Starting Module Host...");
                        Client = await _factory.CreateAsync(productInfo + "_" + version, _metrics, control).ConfigureAwait(false);
                        // Register callback to be called when a method request is received
                        await Client.SetMethodHandlerAsync((request, _) =>
                            _router.InvokeMethodAsync(request)).ConfigureAwait(false);

                        await InitializeTwinAsync().ConfigureAwait(false);

                        // Register callback to be called when settings change ...
                        await Client.SetDesiredPropertyUpdateCallbackAsync(
                            (settings, _) => ProcessSettingsAsync(settings)).ConfigureAwait(false);

                        // Report type of service, chosen site, and connection state
                        var twinSettings = new TwinCollection
                        {
                            [TwinProperty.Type] = type
                        };

                        // Set version information
                        twinSettings[TwinProperty.Version] = version;
                        await Client.UpdateReportedPropertiesAsync(twinSettings).ConfigureAwait(false);

                        _logger.LogInformation("Module Host started.");
                        return;
                    }
                }
                catch (Exception)
                {
                    _logger.LogError("Module Host failed to start.");
                    if (Client != null)
                    {
                        await Try.Async(() => Client.DisposeAsync().AsTask()).ConfigureAwait(false);
                        Client.Dispose();
                        Client = null;
                    }
                    _reported?.Clear();
                    throw;
                }
                finally
                {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Client != null)
            {
                StopAsync().Wait();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Reads the twin including desired and reported settings and applies them to the
        /// settings controllers.  updates the twin for any changes resulting from the
        /// update.  Reported values are cached until user calls Refresh.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeTwinAsync()
        {
            System.Diagnostics.Debug.Assert(_lock.CurrentCount == 0);

            // Process initial setting snapshot from twin
            var twin = await Client.GetTwinAsync().ConfigureAwait(false);
            if (twin == null)
            {
                return;
            }

            var desired = new Dictionary<string, VariantValue>();
            var reported = new Dictionary<string, VariantValue>();

            // Start with reported values which we desire to be re-applied
            _reported.Clear();
            foreach (KeyValuePair<string, dynamic> property in twin.Properties.Reported)
            {
                var value = (VariantValue)_serializer.FromObject(property.Value);
                if (value.IsObject &&
                    value.TryGetProperty("status", out var val) &&
                    value.PropertyNames.Count() == 1)
                {
                    // Clear status properties from twin
                    _reported.AddOrUpdate(property.Key, null);
                    continue;
                }
                if (!ProcessEdgeHostSettings(property.Key, value))
                {
                    _reported.AddOrUpdate(property.Key, value);
                }
            }
            // Apply desired values on top.
            foreach (KeyValuePair<string, dynamic> property in twin.Properties.Desired)
            {
                var value = (VariantValue)_serializer.FromObject(property.Value);
                if (!ProcessEdgeHostSettings(property.Key, value, reported))
                {
                    desired[property.Key] = value;
                }
            }

            // Process settings on controllers
            _logger.LogInformation("Applying initial desired state.");
            await _settings.ProcessSettingsAsync(desired).ConfigureAwait(false);

            // Synchronize all controllers with reported
            _logger.LogInformation("Reporting currently initial state.");
            await ReportControllerStateAsync(twin, reported).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronize controllers with current reported twin state
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="reported"></param>
        /// <returns></returns>
        private async Task ReportControllerStateAsync(Twin twin,
            Dictionary<string, VariantValue> reported)
        {
            var processed = await _settings.GetSettingsStateAsync().ConfigureAwait(false);

            // If there are changes, update what should be reported back.
            foreach (var property in processed)
            {
                var exists = twin.Properties.Reported.Contains(property.Key);
                if (property.Value.IsNull())
                {
                    if (exists)
                    {
                        // If exists as reported, remove
                        reported.AddOrUpdate(property.Key, null);
                        _reported.Remove(property.Key);
                    }
                }
                else
                {
                    if (exists)
                    {
                        // If exists and same as property value, continue
                        var r = (VariantValue)this._serializer.FromObject(
                            twin.Properties.Reported[property.Key]);
                        if (r == property.Value)
                        {
                            continue;
                        }
                    }
                    else if (property.Value.IsNull())
                    {
                        continue;
                    }

                    // Otherwise, add to reported properties
                    reported[property.Key] = property.Value;
                    _reported.AddOrUpdate(property.Key, property.Value);
                }
            }
            if (reported.Count > 0)
            {
                _logger.LogDebug("Reporting controller state...");
                var collection = new TwinCollection();
                foreach (var item in reported)
                {
                    collection[item.Key] = item.Value?.ConvertTo<object>();
                }
                await Client.UpdateReportedPropertiesAsync(collection).ConfigureAwait(false);
                _logger.LogDebug("Complete controller state reported (properties: {@settings}).",
                    reported.Keys);
            }
        }

        /// <summary>
        /// Update device client settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task ProcessSettingsAsync(TwinCollection settings)
        {
            if (settings.Count > 0)
            {
                try
                {
                    await _lock.WaitAsync().ConfigureAwait(false);

                    // Patch existing reported properties
                    var desired = new Dictionary<string, VariantValue>();
                    var reporting = new Dictionary<string, VariantValue>();

                    foreach (KeyValuePair<string, dynamic> property in settings)
                    {
                        var value = (VariantValue)_serializer.FromObject(property.Value);
                        if (!ProcessEdgeHostSettings(property.Key, value, reporting))
                        {
                            desired.AddOrUpdate(property.Key, value);
                        }
                    }

                    if (reporting != null && reporting.Count != 0)
                    {
                        var collection = new TwinCollection();
                        foreach (var item in reporting)
                        {
                            collection[item.Key] = item.Value?.ConvertTo<object>();
                        }
                        await Client.UpdateReportedPropertiesAsync(collection).ConfigureAwait(false);
                        _logger.LogDebug("Internal state updated...", reporting);
                    }

                    // Any controller properties left?
                    if (desired.Count == 0)
                    {
                        return;
                    }

                    _logger.LogDebug("Processing new settings...");
                    var reported = await _settings.ProcessSettingsAsync(desired).ConfigureAwait(false);

                    if (reported != null && reported.Count != 0)
                    {
                        _logger.LogDebug("Reporting setting results...");
                        var collection = new TwinCollection();
                        foreach (var item in reported)
                        {
                            collection[item.Key] = item.Value?.ConvertTo<object>();
                        }
                        await Client.UpdateReportedPropertiesAsync(collection).ConfigureAwait(false);
                        foreach (var item in reported)
                        {
                            _reported.AddOrUpdate(item.Key, item.Value);
                        }
                    }
                    _logger.LogInformation("New settings processed.");
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Process default settings
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="processed"></param>
        /// <returns></returns>
        private static bool ProcessEdgeHostSettings(string key, VariantValue value,
            IDictionary<string, VariantValue> processed = null)
        {
            switch (key.ToLowerInvariant())
            {
                case TwinProperty.Version:
                case TwinProperty.Type:
                    break;
                default:
                    return false;
            }
            if (processed != null)
            {
                processed[key] = value;
            }
            return true;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private ModuleHost(IMetricsContext metrics)
        {
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_module_start",
                () => new Measurement<int>(Client != null ? 1 : 0, metrics.TagList), "Starts",
                "Module starts.");
            _metrics = metrics;
        }

        private readonly IMetricsContext _metrics;
        private readonly IMethodRouter _router;
        private readonly ISettingsRouter _settings;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IClientFactory _factory;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Dictionary<string, VariantValue> _reported =
            new();
    }
}

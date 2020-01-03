// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Text;

    /// <summary>
    /// Module host implementation
    /// </summary>
    public sealed class ModuleHost : IModuleHost, ITwinProperties, IEventEmitter,
        IBlobUpload, IJsonMethodClient {

        /// <inheritdoc/>
        public int MaxMethodPayloadCharacterCount => 120 * 1024;

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, dynamic> Reported => _reported;

        /// <inheritdoc/>
        public string DeviceId { get; private set; }

        /// <inheritdoc/>
        public string ModuleId { get; private set; }

        /// <inheritdoc/>
        public string SiteId { get; private set; }

        /// <summary>
        /// Create module host
        /// </summary>
        /// <param name="router"></param>
        /// <param name="settings"></param>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public ModuleHost(IMethodRouter router, ISettingsRouter settings,
            IClientFactory factory, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (_client != null) {
                try {
                    await _lock.WaitAsync();
                    if (_client != null) {
                        _logger.Information("Stopping Module Host...");
                        try {
                            var twinSettings = new TwinCollection {
                                [TwinProperty.Connected] = false
                            };
                            await _client.UpdateReportedPropertiesAsync(twinSettings);
                            await _client.CloseAsync();
                        }
                        catch (OperationCanceledException) { }
                        catch (IotHubCommunicationException) { }
                        catch (DeviceNotFoundException) { }
                        catch (UnauthorizedException) { }
                        catch (Exception se) {
                            _logger.Error(se, "Module Host not cleanly disconnected.");
                        }
                        _client.Dispose();
                    }
                    _logger.Information("Module Host stopped.");
                }
                catch (Exception ce) {
                    _logger.Error(ce, "Module Host stopping caused exception.");
                }
                finally {
                    _client = null;
                    _reported?.Clear();
                    DeviceId = null;
                    ModuleId = null;
                    SiteId = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(string type, string siteId, string serviceInfo,
            IProcessControl reset) {
            if (_client == null) {
                try {
                    await _lock.WaitAsync();
                    if (_client == null) {
                        // Create client
                        _logger.Debug("Starting Module Host...");
                        _client = await _factory.CreateAsync(serviceInfo, reset);
                        DeviceId = _factory.DeviceId;
                        ModuleId = _factory.ModuleId;

                        // Register callback to be called when a method request is received
                        await _client.SetMethodDefaultHandlerAsync((request, _) =>
                            _router.InvokeMethodAsync(request), null);

                        await InitializeTwinAsync();

                        // Register callback to be called when settings change ...
                        await _client.SetDesiredPropertyUpdateCallbackAsync(
                            (settings, _) => ProcessSettingsAsync(settings), null);

                        // Report type of service, chosen site, and connection state
                        var twinSettings = new TwinCollection {
                            [TwinProperty.Type] = type,
                            [TwinProperty.Connected] = true
                        };

                        // Set site if provided
                        if (string.IsNullOrEmpty(SiteId)) {
                            SiteId = siteId;
                            twinSettings[TwinProperty.SiteId] = SiteId;
                        }
                        await _client.UpdateReportedPropertiesAsync(twinSettings);

                        // Done...
                        _logger.Information("Module Host started.");
                        return;
                    }
                }
                catch (Exception ex) {
                    _client?.Dispose();
                    _client = null;
                    _reported?.Clear();
                    DeviceId = null;
                    ModuleId = null;
                    SiteId = null;
                    throw ex;
                }
                finally {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public async Task RefreshAsync() {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    _twin = await _client.GetTwinAsync();
                    _reported.Clear();
                    foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                        _reported.Add(property.Key, property.Value);
                    }

                    var changes = await _settings.GetSettingsChangesAsync();
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendEventAsync(IEnumerable<byte[]> batch, string contentType,
            string eventSchema, string contentEncoding) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    var messages = batch
                        .Select(ev =>
                             CreateMessage(ev, contentEncoding, contentType, eventSchema,
                                DeviceId, ModuleId))
                        .ToList();
                    try {
                        await _client.SendEventBatchAsync(messages);
                    }
                    finally {
                        messages.ForEach(m => m?.Dispose());
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendEventAsync(byte[] data, string contentType, string eventSchema,
            string contentEncoding) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    using (var msg = CreateMessage(data, contentEncoding, contentType,
                        eventSchema, DeviceId, ModuleId)) {
                        await _client.SendEventAsync(msg);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ReportAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    var collection = new TwinCollection();
                    foreach (var property in properties) {
                        collection[property.Key] = property.Value;
                    }
                    await _client.UpdateReportedPropertiesAsync(collection);
                    foreach (var property in properties) {
                        _reported.Remove(property.Key);
                        _reported.Add(property.Key, property.Value);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ReportAsync(string propertyId, dynamic value) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    var collection = new TwinCollection {
                        [propertyId] = value
                    };
                    await _client.UpdateReportedPropertiesAsync(collection);
                    _reported.Remove(propertyId);
                    _reported.Add(propertyId, value);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendFileAsync(string fileName, Stream stream, string contentType) {
            await _client.UploadToBlobAsync(
                $"{contentType.UrlEncode()}/{fileName.UrlEncode().TrimEnd('/')}",
                    stream);
        }

        /// <inheritdoc/>
        public async Task<string> CallMethodAsync(string deviceId, string moduleId,
            string method, string payload, TimeSpan? timeout, CancellationToken ct) {
            var request = new MethodRequest(method, Encoding.UTF8.GetBytes(payload),
                timeout, null);
            MethodResponse response;
            if (string.IsNullOrEmpty(moduleId)) {
                response = await _client.InvokeMethodAsync(deviceId, request, ct);
            }
            else {
                response = await _client.InvokeMethodAsync(deviceId, moduleId, request, ct);
            }
            if (response.Status != 200) {
                throw new MethodCallStatusException(
                    response.ResultAsJson, response.Status);
            }
            return response.ResultAsJson;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_client != null) {
                StopAsync().Wait();
            }
            _lock.Dispose();
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="eventSchema"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private static Message CreateMessage(byte[] data, string contentEncoding,
            string contentType, string eventSchema, string deviceId, string moduleId) {
            var msg = new Message(data) {
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(contentEncoding)) {
                msg.Properties.Add(CommonProperties.ContentEncoding, contentEncoding);
            }
            if (!string.IsNullOrEmpty(eventSchema)) {
                msg.Properties.Add(CommonProperties.EventSchemaType, eventSchema);
            }
            if (!string.IsNullOrEmpty(deviceId)) {
                msg.Properties.Add(CommonProperties.DeviceId, deviceId);
            }
            if (!string.IsNullOrEmpty(moduleId)) {
                msg.Properties.Add(CommonProperties.ModuleId, moduleId);
            }
            msg.Properties.Add(CommonProperties.CreationTimeUtc,
                msg.CreationTimeUtc.ToString());
            return msg;
        }

        /// <summary>
        /// Reads the twin including desired and reported settings and applies them to the
        /// settings controllers.  updates the twin for any changes resulting from the
        /// update.  Reported values are cached until user calls Refresh.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeTwinAsync() {

            System.Diagnostics.Debug.Assert(_lock.CurrentCount == 0);

            // Process initial setting snapshot from twin
            _twin = await _client.GetTwinAsync();

            if (!string.IsNullOrEmpty(_twin.DeviceId)) {
                DeviceId = _twin.DeviceId;
            }
            if (!string.IsNullOrEmpty(_twin.ModuleId)) {
                ModuleId = _twin.ModuleId;
            }
            _logger.Information("Initialize device twin for {deviceId} - {moduleId}",
                DeviceId, ModuleId ?? "standalone");

            var desired = new TwinCollection();
            var reported = new TwinCollection();

            // Start with reported values which we desire to be re-applied
            _reported.Clear();
            foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                if (property.Value is JObject obj &&
                    obj.TryGetValue("status", StringComparison.InvariantCultureIgnoreCase,
                        out var val) &&
                    obj.Children().Count() == 1) {
                    // Clear status properties from twin
                    _reported[property.Key] = null;
                    continue;
                }
                if (!ProcessEdgeHostSettings(property.Key, property.Value)) {
                    desired[property.Key] = property.Value;
                    _reported[property.Key] = property.Value;
                }
            }
            // Apply desired values on top.
            foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Desired) {
                if (!ProcessEdgeHostSettings(property.Key, property.Value, reported)) {
                    desired[property.Key] = property.Value;
                }
            }

            // Process settings on controllers
            _logger.Debug("Applying initial state.");
            var processed = await _settings.ProcessSettingsAsync(desired);

            // If there are changes, update what should be reported back.
            foreach (KeyValuePair<string, dynamic> property in processed) {
                var exists = _twin.Properties.Reported.Contains(property.Key);
                if (property.Value == null) {
                    if (exists) {
                        // If exists as reported, remove
                        reported[property.Key] = null;
                        _reported.Remove(property.Key);
                    }
                }
                else {
                    // If exists and same as property value, continue
                    if (exists && _twin.Properties.Reported[property.Key]
                        .Equals(property.Value)) {
                        continue;
                    }
                    // Otherwise, add to reported properties
                    reported[property.Key] = property.Value;
                    _reported[property.Key] = property.Value;
                }
            }
            if (reported.Count > 0) {
                _logger.Debug("Reporting initial state.");
                await _client.UpdateReportedPropertiesAsync(reported);
            }
        }

        /// <summary>
        /// Update device client settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task ProcessSettingsAsync(TwinCollection settings) {
            if (settings.Count > 0) {
                try {
                    await _lock.WaitAsync();

                    // Patch existing reported properties
                    var desired = new TwinCollection();
                    var reporting = new TwinCollection();
                    foreach (KeyValuePair<string, dynamic> property in settings) {
                        if (!ProcessEdgeHostSettings(property.Key, property.Value,
                            reporting)) {
                            desired[property.Key] = property.Value;
                        }
                    }

                    if (reporting != null && reporting.Count != 0) {
                        await _client.UpdateReportedPropertiesAsync(reporting);
                        _logger.Debug("Internal state updated...", reporting);
                    }

                    // Any controller properties left?
                    if (desired.Count == 0) {
                        return;
                    }

                    _logger.Debug("Processing new settings...");
                    reporting = await _settings.ProcessSettingsAsync(desired);

                    if (reporting != null && reporting.Count != 0) {
                        _logger.Debug("Reporting setting results...");
                        await _client.UpdateReportedPropertiesAsync(reporting);
                        foreach (KeyValuePair<string, dynamic> property in reporting) {
                            _reported.Remove(property.Key);
                            _reported.Add(property.Key, property.Value);
                        }
                    }
                    _logger.Information("New settings processed.");
                }
                finally {
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
        private bool ProcessEdgeHostSettings(string key, dynamic value,
            TwinCollection processed = null) {
            switch (key.ToLowerInvariant()) {
                case TwinProperty.Connected:
                case TwinProperty.Type:
                    break;
                case TwinProperty.SiteId:
                    SiteId = (string)value;
                    break;
                default:
                    return false;
            }
            if (processed != null) {
                processed[key] = value;
            }
            return true;
        }

        private IClient _client;
        private Twin _twin;

        private readonly IMethodRouter _router;
        private readonly ISettingsRouter _settings;
        private readonly ILogger _logger;
        private readonly IClientFactory _factory;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, dynamic> _reported =
            new Dictionary<string, dynamic>();
    }
}

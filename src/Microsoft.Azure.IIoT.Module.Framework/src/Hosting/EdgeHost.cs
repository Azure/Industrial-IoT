// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge host implementation
    /// </summary>
    public class EdgeHost : IEdgeHost, ITwinProperties, IEventEmitter, IBlobUpload {

        /// <summary>
        /// Twin properties
        /// </summary>
        public IReadOnlyDictionary<string, dynamic> Reported => _reported;

        /// <summary>
        /// Device id events are emitted on
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Module id events are emitted on
        /// </summary>
        public string ModuleId { get; private set; }

        /// <summary>
        /// Site id of the module
        /// </summary>
        public string SiteId { get; private set; }

        /// <summary>
        /// Create edge service module
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        /// <param name="router"></param>
        public EdgeHost(IMethodRouter router, ISettingsRouter settings, IClientFactory factory,
            ILogger logger) {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Stop service
        /// </summary>
        public async Task StopAsync() {
            if (_client != null) {
                try {
                    await _lock.WaitAsync();
                    if (_client != null) {
                        try {
                            var twinSettings = new TwinCollection {
                                [kConnectedProp] = false
                            };
                            await _client.UpdateReportedPropertiesAsync(twinSettings);
                            await _client.CloseAsync();
                        }
                        catch (OperationCanceledException) { }
                        catch (IotHubCommunicationException) { }
                        catch (UnauthorizedException) { }
                        catch (Exception se) {
                            _logger.Error("Edge Host not cleanly disconnected.",
                                () => se);
                        }
                        _client.Dispose();
                    }
                    _logger.Info($"Edge Host stopped @{SiteId}.", () => { });
                }
                catch (Exception ce) {
                    _logger.Error("Edge Host stopping caused exception.",
                        () => ce);
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

        /// <summary>
        /// Start service
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync(string type, string siteId) {
            if (_client == null) {
                try {
                    await _lock.WaitAsync();
                    if (_client == null) {
                        // Create client
                        _logger.Debug("Starting Edge Host...", () => { });
                        _client = await _factory.CreateAsync();
                        DeviceId = _factory.DeviceId;
                        ModuleId = _factory.ModuleId;

                        // Register callback to be called when a method request is received
                        await _client.SetMethodDefaultHandlerAsync((request, _) =>
                            _router.InvokeMethodAsync(request), null);

                        await InitializeTwinAsync();

                        // Register callback to be called when settings change ...
                        await _client.SetDesiredPropertyUpdateCallbackAsync(
                            (settings, _) => ProcessSettingsAsync(settings), null);

                        // Set default site if not already provided
                        if (string.IsNullOrEmpty(SiteId)) {
                            SiteId = siteId ?? ModuleId;
                        }

                        // Report type of service, chosen site, and connection state
                        var twinSettings = new TwinCollection {
                            [kTypeProp] = type,
                            [kSiteIdProp] = SiteId,
                            [kConnectedProp] = true
                        };
                        await _client.UpdateReportedPropertiesAsync(twinSettings);

                        // Done...
                        _logger.Info($"Edge Host started @{SiteId}).", () => { });
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

        /// <summary>
        /// Refresh twin
        /// </summary>
        /// <returns></returns>
        public async Task RefreshAsync() {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    _twin = await _client.GetTwinAsync();
                    _reported.Clear();
                    foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                        _reported.Add(property.Key, property.Value);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send events as batch
        /// </summary>
        /// <returns></returns>
        public async Task SendAsync(IEnumerable<byte[]> batch, string contentType) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    await _client.SendEventBatchAsync(batch.Select(ev =>
                        CreateMessage(ev, contentType, DeviceId, ModuleId)));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send event
        /// </summary>
        /// <returns></returns>
        public async Task SendAsync(byte[] data, string contentType) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    await _client.SendEventAsync(
                        CreateMessage(data, contentType, DeviceId, ModuleId));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send a list of updated property
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task SendAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
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

        /// <summary>
        /// Send an updated property
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SendAsync(string propertyId, dynamic value) {
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

        /// <summary>
        /// Upload blob
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async Task SendFileAsync(string fileName, string contentType) {
            using (var file = new FileStream(fileName, FileMode.Open)) {
                await _client.UploadToBlobAsync(fileName, file);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            if (_client != null) {
                StopAsync().Wait();
            }
        }

        /// <summary>
        /// Create pcs/central conform message
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private static Message CreateMessage(byte[] data, string contentType,
            string deviceId, string moduleId) {
            var msg = new Message(data) {
                ContentType = contentType,
                CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(contentType)) {
                msg.Properties.Add(CommonProperties.kContentType, contentType);
            }
            if (!string.IsNullOrEmpty(deviceId)) {
                msg.Properties.Add(CommonProperties.kDeviceId, deviceId);
            }
            if (!string.IsNullOrEmpty(moduleId)) {
                msg.Properties.Add(CommonProperties.kModuleId, moduleId);
            }
            msg.Properties.Add(CommonProperties.kCreationTimeUtc,
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

            Contract.Invariant(_lock.CurrentCount == 0);

            // Process initial setting snapshot from twin
            _twin = await _client.GetTwinAsync();

            if (!string.IsNullOrEmpty(_twin.DeviceId)) {
                DeviceId = _twin.DeviceId;
            }
            if (!string.IsNullOrEmpty(_twin.ModuleId)) {
                ModuleId = _twin.ModuleId;
            }
            _logger.Info($"Initialize twin for {DeviceId} - {ModuleId ?? "standalone"}",
                () => { });

            if (_twin.Properties.Reported.Count > 0 || _twin.Properties.Desired.Count > 0) {

                var desired = new TwinCollection();
                var reported = new TwinCollection();

                // Start with reported values which we desire to be re-applied
                _reported.Clear();
                foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                    if (property.Value is JObject obj &&
                        obj.TryGetValue("Status", out var val) &&
                        obj.Children().Count() == 1) {
                        // Clear status properties from twin
                        reported[property.Key] = null;
                        continue;
                    }
                    if (!ProcessEdgeHostSettings(property.Key, property.Value)) {
                        desired[property.Key] = property.Value;
                        _reported.Add(property.Key, property.Value);
                    }
                }
                // Apply desired values on top.
                foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Desired) {
                    if (!ProcessEdgeHostSettings(property.Key, property.Value, reported)) {
                        desired[property.Key] = property.Value;
                    }
                }

                // Process settings on controllers
                _logger.Debug("Applying initial state.", () => desired);
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
                        _reported.Remove(property.Key);
                        _reported.Add(property.Key, property.Value);
                    }
                }
                if (reported.Count > 0) {
                    _logger.Debug("Reporting initial state.", () => reported);
                    await _client.UpdateReportedPropertiesAsync(reported);
                }
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
                        _logger.Debug("Internal state updated...",
                            () => reporting);
                    }

                    // Any controller properties left?
                    if (desired.Count == 0) {
                        return;
                    }

                    _logger.Debug("Applying desired state.", () => desired);
                    reporting = await _settings.ProcessSettingsAsync(desired);

                    if (reporting != null && reporting.Count != 0) {
                        _logger.Debug("Reporting new state.", () => reporting);
                        await _client.UpdateReportedPropertiesAsync(reporting);
                        foreach (KeyValuePair<string, dynamic> property in reporting) {
                            _reported.Remove(property.Key);
                            _reported.Add(property.Key, property.Value);
                        }
                    }
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
        /// <returns></returns>
        private bool ProcessEdgeHostSettings(string key, dynamic value,
            TwinCollection processed = null) {
            switch(key.ToLowerInvariant()) {
                case kConnectedProp:
                case kTypeProp:
                    break;
                case kSiteIdProp:
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

        private const string kConnectedProp = "__connected__";
        private const string kTypeProp = "__type__";
        private const string kSiteIdProp = "__siteid__";

        private IClient _client;
        private Twin _twin;

        private readonly IMethodRouter _router;
        private readonly ISettingsRouter _settings;
        private readonly ILogger _logger;
        private readonly IClientFactory _factory;
        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1);
        private readonly Dictionary<string, dynamic> _reported =
            new Dictionary<string, dynamic>();
    }
}

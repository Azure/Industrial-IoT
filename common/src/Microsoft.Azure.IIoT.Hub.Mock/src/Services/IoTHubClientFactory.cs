// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Common;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable factory that creates clients from device sdk
    /// </summary>
    public class IoTHubClientFactory : IClientFactory {

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public string ModuleId { get; }

        /// <inheritdoc />
        public string Gateway { get; }

        /// <inheritdoc />
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create sdk factory
        /// </summary>
        /// <param name="hub">Outer hub abstraction</param>
        /// <param name="config">Module framework configuration</param>
        public IoTHubClientFactory(IIoTHub hub, IModuleConfig config) {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            if (string.IsNullOrEmpty(config.EdgeHubConnectionString)) {
                throw new InvalidConfigurationException(
                    "Must have connection string or module id to create clients.");
            }
            var cs = IotHubConnectionStringBuilder.Create(config.EdgeHubConnectionString);
            if (string.IsNullOrEmpty(cs.DeviceId)) {
                throw new InvalidConfigurationException(
                    "Connection string is not a device or module connection string.");
            }
            DeviceId = cs.DeviceId;
            ModuleId = cs.ModuleId;
            Gateway = cs.GatewayHostName;
        }

        /// <inheritdoc/>
        public Task<IClient> CreateAsync(string product, IProcessControl ctrl) {
            var client = new IoTHubClient(ctrl);
            var connection = _hub.Connect(DeviceId, ModuleId, client);
            client.Connection = connection ??
                throw new CommunicationException("Failed to connect to fake hub");
            return Task.FromResult<IClient>(client);
        }

        /// <summary>
        /// A test client
        /// </summary>
        public sealed class IoTHubClient : IClient, IIoTClientCallback {

            /// <summary>
            /// Whether the client is closed
            /// </summary>
            public bool IsClosed { get; internal set; }

            /// <inheritdoc />
            public int MaxBodySize => 256 * 1024;

            /// <summary>
            /// Connection to iot hub
            /// </summary>
            public IIoTHubConnection Connection { get; internal set; }

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="ctrl"></param>
            internal IoTHubClient(IProcessControl ctrl) {
                _ctrl = ctrl;
            }

            /// <inheritdoc />
            public ValueTask DisposeAsync() {
                Connection.Close();
                Connection = null;
                IsClosed = true;
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc />
            public Task SendEventAsync(ITelemetryEvent message) {
                if (!IsClosed) {
                    Connection.SendEvent(message);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(MethodCallback methodHandler) {
                if (!IsClosed) {
                    _methods = methodHandler;
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback) {
                if (!IsClosed) {
                    _properties = callback;
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<Twin> GetTwinAsync() {
                return Task.FromResult(IsClosed ? null : Connection.GetTwin());
            }

            /// <inheritdoc />
            public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) {
                if (!IsClosed) {
                    Connection.UpdateReportedProperties(reportedProperties);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return Task.FromResult(IsClosed ? null : Connection.Call(deviceId,
                    string.IsNullOrEmpty(moduleId) ? null : moduleId, methodRequest));
            }

            /// <inheritdoc />
            public void SetDesiredProperties(TwinCollection desiredProperties) {
                _properties?.Invoke(desiredProperties, null);
            }

            /// <inheritdoc />
            public MethodResponse Call(MethodRequest methodRequest) {
                var cb = _methods;
                if (cb == null) {
                    return new MethodResponse(500);
                }
                try {
                    return cb(methodRequest, null).Result;
                }
                catch {
                    return new MethodResponse(500);
                }
            }

            /// <inheritdoc />
            public void Dispose() {
                if (!IsClosed) {
                    throw new ThreadStateException("Dispose but still open.");
                }
            }

            /// <inheritdoc />
            public void RemoteDisconnect() {
                Connection = null;
                IsClosed = true;
            }

            /// <inheritdoc />
            public ITelemetryEvent CreateTelemetryEvent() {
                return new TelemetryMessage();
            }

            /// <summary>
            /// Message wrapper
            /// </summary>
            internal sealed class TelemetryMessage : ITelemetryEvent {

                /// <inheritdoc/>
                public DateTime Timestamp { get; set; }

                /// <inheritdoc/>
                public string ContentType {
                    get {
                        return _msg.ContentType;
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.ContentType = value;
                            _msg.Properties.AddOrUpdate(SystemProperties.MessageSchema, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public string ContentEncoding {
                    get {
                        return _msg.ContentEncoding;
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.ContentEncoding = value;
                            _msg.Properties.AddOrUpdate(CommonProperties.ContentEncoding, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public string MessageSchema {
                    get {
                        return _msg.Properties.GetValueOrDefault(CommonProperties.EventSchemaType);
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.Properties.AddOrUpdate(CommonProperties.EventSchemaType, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public string RoutingInfo {
                    get {
                        return _msg.Properties.GetValueOrDefault(CommonProperties.RoutingInfo);
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.Properties.AddOrUpdate(CommonProperties.RoutingInfo, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public string DeviceId {
                    get {
                        return _msg.Properties.GetValueOrDefault(CommonProperties.DeviceId);
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.Properties.AddOrUpdate(CommonProperties.DeviceId, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public string ModuleId {
                    get {
                        return _msg.Properties.GetValueOrDefault(CommonProperties.ModuleId);
                    }
                    set {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            _msg.Properties.AddOrUpdate(CommonProperties.ModuleId, value);
                        }
                    }
                }

                /// <inheritdoc/>
                public IReadOnlyList<byte[]> Payload { get; set; }

                /// <inheritdoc/>
                public string OutputName { get; set; }
                /// <inheritdoc/>
                public bool Retain { get; set; }
                /// <inheritdoc/>
                public TimeSpan Ttl { get; set; }

                /// <inheritdoc/>
                public void Dispose() {
                    // TODO: Return to pool
                    _msg.Dispose();
                }

                Message _msg = new Message();
            }

            private MethodCallback _methods;
            private DesiredPropertyUpdateCallback _properties;
            private readonly IProcessControl _ctrl;
        }

        private readonly IIoTHub _hub;
    }
}

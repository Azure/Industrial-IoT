// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            public int MaxMessageSize => 256 * 1024;

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
            public Task CloseAsync() {
                Connection.Close();
                Connection = null;
                IsClosed = true;
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) {
                // Add event to telemetry list
                if (!IsClosed) {
                    Connection.SendEvent(message);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SendEventAsync(string outputName, Message message) {
                // Add event to telemetry list
                if (!IsClosed) {
                    Connection.SendEvent(outputName, message);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) {
                if (!IsClosed) {
                    foreach (var message in messages) {
                        Connection.SendEvent(message);
                    }
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages) {
                if (!IsClosed) {
                    foreach (var message in messages) {
                        Connection.SendEvent(outputName, message);
                    }
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(string methodName,
                MethodCallback methodHandler, object userContext) {
                if (!IsClosed) {
                    _methods.AddOrUpdate(methodName, (methodHandler, userContext));
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetMethodDefaultHandlerAsync(
                MethodCallback methodHandler, object userContext) {
                if (!IsClosed) {
                    _methods.AddOrUpdate("$default", (methodHandler, userContext));
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback, object userContext) {
                if (!IsClosed) {
                    _properties.Add((callback, userContext));
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
            public Task UploadToBlobAsync(string blobName, Stream source) {
                if (!IsClosed) {
                    Connection.SendBlob(blobName, source.ReadAsBuffer());
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return Task.FromResult(IsClosed ? null :
                    Connection.Call(deviceId, moduleId, methodRequest));
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return Task.FromResult(IsClosed ? null :
                    Connection.Call(deviceId, null, methodRequest));
            }

            /// <inheritdoc />
            public Task SetStreamsDefaultHandlerAsync(StreamCallback streamHandler,
                object userContext) {
                throw new NotSupportedException("Test client does not support streams yet");
            }

            /// <inheritdoc />
            public Task SetStreamHandlerAsync(string streamName, StreamCallback
                streamHandler, object userContext) {
                throw new NotSupportedException("Test client does not support streams yet");
            }

            /// <inheritdoc />
            public Task<Stream> CreateStreamAsync(string streamName, string hostName,
                ushort port, CancellationToken cancellationToken) {
                throw new NotSupportedException("Test client does not support streams yet");
            }

            /// <inheritdoc />
            public void SetDesiredProperties(TwinCollection desiredProperties) {
                foreach (var (cb, ctx) in _properties) {
                    cb(desiredProperties, ctx);
                }
            }

            /// <inheritdoc />
            public MethodResponse Call(MethodRequest methodRequest) {
                if (!_methods.TryGetValue(methodRequest.Name, out var item)) {
                    if (!_methods.TryGetValue("$default", out item)) {
                        return new MethodResponse(500);
                    }
                }
                try {
                    var (cb, ctx) = item;
                    return cb(methodRequest, ctx).Result;
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

            private readonly Dictionary<string, (MethodCallback, object)> _methods =
                new Dictionary<string, (MethodCallback, object)>();
            private readonly List<(DesiredPropertyUpdateCallback, object)> _properties =
                new List<(DesiredPropertyUpdateCallback, object)>();
#pragma warning disable IDE0052 // Remove unread private members
            private readonly IProcessControl _ctrl;
#pragma warning restore IDE0052 // Remove unread private members
        }

        private readonly IIoTHub _hub;
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock
{
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Furly.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable factory that creates clients from device sdk
    /// </summary>
    public class IoTHubClientFactory : IClientFactory
    {
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
        public IoTHubClientFactory(IIoTHub hub, IModuleConfig config)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            if (string.IsNullOrEmpty(config.EdgeHubConnectionString))
            {
                throw new InvalidConfigurationException(
                    "Must have connection string or module id to create clients.");
            }
            var cs = IotHubConnectionStringBuilder.Create(config.EdgeHubConnectionString);
            if (string.IsNullOrEmpty(cs.DeviceId))
            {
                throw new InvalidConfigurationException(
                    "Connection string is not a device or module connection string.");
            }
            DeviceId = cs.DeviceId;
            ModuleId = cs.ModuleId;
            Gateway = cs.GatewayHostName;
        }

        /// <inheritdoc/>
        public Task<IClient> CreateAsync(string product,
            IMetricsContext metrics, IProcessControl ctrl)
        {
            var client = new IoTHubClient();
            var connection = _hub.Connect(DeviceId, ModuleId, client);
            client.Connection = connection ??
                throw new IOException("Failed to connect to fake hub");
            return Task.FromResult<IClient>(client);
        }

        /// <summary>
        /// A test client
        /// </summary>
        public sealed class IoTHubClient : IClient, IIoTClientCallback
        {
            /// <summary>
            /// Whether the client is closed
            /// </summary>
            public bool IsClosed { get; internal set; }

            /// <inheritdoc />
            public int MaxEventPayloadSizeInBytes => 256 * 1024;

            /// <summary>
            /// Connection to iot hub
            /// </summary>
            public IIoTHubConnection Connection { get; internal set; }

            /// <summary>
            /// Create client
            /// </summary>
            internal IoTHubClient()
            {
            }

            /// <inheritdoc />
            public ValueTask DisposeAsync()
            {
                Connection.Close();
                Connection = null;
                IsClosed = true;
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(MethodCallback methodHandler)
            {
                if (!IsClosed)
                {
                    _methods = methodHandler;
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback)
            {
                if (!IsClosed)
                {
                    _properties = callback;
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<Twin> GetTwinAsync()
            {
                return Task.FromResult(IsClosed ? null : Connection.GetTwin());
            }

            /// <inheritdoc />
            public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
            {
                if (!IsClosed)
                {
                    Connection.UpdateReportedProperties(reportedProperties);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken)
            {
                return Task.FromResult(IsClosed ? null : Connection.CallMethod(deviceId,
                    string.IsNullOrEmpty(moduleId) ? null : moduleId, methodRequest));
            }

            /// <inheritdoc />
            public void SetDesiredProperties(TwinCollection desiredProperties)
            {
                _properties?.Invoke(desiredProperties, null);
            }

            /// <inheritdoc />
            public MethodResponse CallMethod(MethodRequest methodRequest)
            {
                var cb = _methods;
                if (cb == null)
                {
                    return new MethodResponse(500);
                }
                try
                {
                    return cb(methodRequest, null).Result;
                }
                catch
                {
                    return new MethodResponse(500);
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (!IsClosed)
                {
                    throw new ThreadStateException("Dispose but still open.");
                }
            }

            /// <inheritdoc />
            public IEvent CreateEvent()
            {
                return new TelemetryMessage(this);
            }

            /// <summary>
            /// Message wrapper
            /// </summary>
            internal sealed class TelemetryMessage : IEvent
            {
                private DateTime timestamp;

                /// <inheritdoc/>
                public DateTime GetTimestamp()
                {
                    return timestamp;
                }

                /// <inheritdoc/>
                public IEvent SetTimestamp(DateTime value)
                {
                    timestamp = value;
                    return this;
                }

                /// <inheritdoc/>
                public string GetContentType()
                {
                    return _msg.ContentType;
                }

                /// <inheritdoc/>
                public IEvent SetContentType(string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _msg.ContentType = value;
                        _msg.Properties.AddOrUpdate(SystemProperties.MessageSchema, value);
                    }
                    return this;
                }

                /// <inheritdoc/>
                public string GetContentEncoding()
                {
                    return _msg.ContentEncoding;
                }

                /// <inheritdoc/>
                public IEvent SetContentEncoding(string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _msg.ContentEncoding = value;
                        _msg.Properties.AddOrUpdate(CommonProperties.ContentEncoding, value);
                    }
                    return this;
                }

                /// <inheritdoc/>
                public string GetMessageSchema()
                {
                    if (_msg.Properties.TryGetValue(CommonProperties.EventSchemaType, out var value))
                    {
                        return value;
                    }
                    return null;
                }

                /// <inheritdoc/>
                public IEvent SetMessageSchema(string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _msg.Properties.AddOrUpdate(CommonProperties.EventSchemaType, value);
                    }
                    return this;
                }

                /// <inheritdoc/>
                public string GetRoutingInfo()
                {
                    if (_msg.Properties.TryGetValue(CommonProperties.RoutingInfo, out var value))
                    {
                        return value;
                    }
                    return null;
                }

                /// <inheritdoc/>
                public IEvent SetRoutingInfo(string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _msg.Properties.AddOrUpdate(CommonProperties.RoutingInfo, value);
                    }
                    return this;
                }

                private IReadOnlyList<ReadOnlyMemory<byte>> buffers;

                /// <inheritdoc/>
                public IReadOnlyList<ReadOnlyMemory<byte>> GetBuffers()
                {
                    return buffers;
                }

                /// <inheritdoc/>
                public IEvent AddBuffers(IReadOnlyList<ReadOnlyMemory<byte>> value)
                {
                    buffers = value;
                    return this;
                }

                private string topic;

                /// <inheritdoc/>
                public string GetTopic()
                {
                    return topic;
                }

                /// <inheritdoc/>
                public IEvent SetTopic(string value)
                {
                    topic = value;
                    return this;
                }

                private bool retain;

                /// <inheritdoc/>
                public bool GetRetain()
                {
                    return retain;
                }

                /// <inheritdoc/>
                public IEvent SetRetain(bool value)
                {
                    retain = value;
                    return this;
                }

                private TimeSpan ttl;

                /// <inheritdoc/>
                public TimeSpan GetTtl()
                {
                    return ttl;
                }

                /// <inheritdoc/>
                public IEvent SetTtl(TimeSpan value)
                {
                    ttl = value;
                    return this;
                }

                /// <summary>
                /// Create event
                /// </summary>
                /// <param name="outer"></param>
                public TelemetryMessage(IoTHubClient outer)
                {
                    _outer = outer;
                }

                /// <inheritdoc/>
                public void Dispose()
                {
                    // TODO: Return to pool
                    _msg.Dispose();
                }

                /// <inheritdoc/>
                public Task SendAsync(CancellationToken ct = default)
                {
                    if (!_outer.IsClosed)
                    {
                        _outer.Connection.SendEvent(this);
                    }
                    return Task.CompletedTask;
                }

                private readonly Message _msg = new();
                private readonly IoTHubClient _outer;
            }

            private MethodCallback _methods;
            private DesiredPropertyUpdateCallback _properties;
        }

        private readonly IIoTHub _hub;
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;

    public sealed partial class IoTSdkFactory {
        /// <summary>
        /// Adapts device client to interface
        /// </summary>
        public sealed class DeviceClientAdapter : IClient {

            /// <summary>
            /// Whether the client is closed
            /// </summary>
            public bool IsClosed { get; internal set; }
            
            /// <summary>
            /// 
            /// </summary>
            public string DeviceId { get; }

            /// <summary>
            /// 
            /// </summary>
            public string ModuleId => null;

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="client"></param>
            /// <param name="deviceId"></param>
            internal DeviceClientAdapter(DeviceClient client, string deviceId) {
                _client = client ??
                    throw new ArgumentNullException(nameof(client));
                DeviceId = deviceId;
            }

            /// <summary>
            /// Factory
            /// </summary>
            /// <param name="product"></param>
            /// <param name="cs"></param>
            /// <param name="deviceId"></param>
            /// <param name="transportSetting"></param>
            /// <param name="timeout"></param>
            /// <param name="retry"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<IClient> CreateAsync(string product,
                IotHubConnectionStringBuilder cs, string deviceId,
                ITransportSettings transportSetting, TimeSpan timeout,
                IRetryPolicy retry, Action onConnectionLost, ILogger logger) {
                var client = Create(cs, transportSetting);
                var adapter = new DeviceClientAdapter(client, deviceId);

                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) =>
                    adapter.OnConnectionStatusChange(deviceId, onConnectionLost, logger, s, r));
                if (retry != null) {
                    client.SetRetryPolicy(retry);
                }
                client.DiagnosticSamplingPercentage = 5;
                client.ProductInfo = product;

                await client.OpenAsync();
                return adapter;
            }

            /// <inheritdoc />
            public Task CloseAsync() {
                _client.OperationTimeoutInMilliseconds = 3000;
                _client.SetRetryPolicy(new NoRetry());
                return IsClosed ? Task.CompletedTask : _client.CloseAsync();
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) {
                return IsClosed ? Task.CompletedTask : _client.SendEventAsync(message);
            }

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) {
                return IsClosed ? Task.CompletedTask : _client.SendEventBatchAsync(messages);
            }

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(string methodName,
                MethodCallback methodHandler, object userContext) {
                return _client.SetMethodHandlerAsync(methodName, methodHandler, userContext);
            }

            /// <inheritdoc />
            public Task SetMethodDefaultHandlerAsync(
                MethodCallback methodHandler, object userContext) {
                return _client.SetMethodDefaultHandlerAsync(methodHandler, userContext);
            }

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback, object userContext) {
                return _client.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
            }

            /// <inheritdoc />
            public Task<Twin> GetTwinAsync() {
                return _client.GetTwinAsync();
            }

            /// <inheritdoc />
            public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) {
                return IsClosed ? Task.CompletedTask : _client.UpdateReportedPropertiesAsync(reportedProperties);
            }

            /// <inheritdoc />
            public Task UploadToBlobAsync(string blobName, Stream source) {
                return IsClosed ? Task.CompletedTask : _client.UploadToBlobAsync(blobName, source);
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                throw new NotSupportedException("Device client does not support methods");
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                throw new NotSupportedException("Device client does not support methods");
            }

            /// <inheritdoc />
            public Task SetStreamsDefaultHandlerAsync(StreamCallback streamHandler,
                object userContext) {
                throw new NotSupportedException("Device client does not support streams");
            }

            /// <inheritdoc />
            public Task SetStreamHandlerAsync(string streamName, StreamCallback
                streamHandler, object userContext) {
                throw new NotSupportedException("Device client does not support streams");
            }

            /// <inheritdoc />
            public Task<Stream> CreateStreamAsync(string streamName, string hostName,
                ushort port, CancellationToken cancellationToken) {
                throw new NotSupportedException("Device client does not support streams");
            }

            /// <inheritdoc />
            public void Dispose() {
                _client?.Dispose();
            }

            /// <summary>
            /// Handle status change event
            /// </summary>
            /// <param name="deviceId"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <param name="status"></param>
            /// <param name="reason"></param>
            private void OnConnectionStatusChange(string deviceId,
                Action onConnectionLost, ILogger logger, ConnectionStatus status,
                ConnectionStatusChangeReason reason) {

                if (status == ConnectionStatus.Connected) {
                    logger.Information("{counter}: Device {deviceId} reconnected " +
                        "due to {reason}.", _reconnectCounter, deviceId, reason);
                    _reconnectCounter++;
                    return;
                }
                logger.Information("{counter}: Device {deviceId} disconnected " +
                    "due to {reason} - now {status}...", _reconnectCounter, deviceId,
                        reason, status);
                if (IsClosed) {
                    // Already closed - nothing to do
                    return;
                }
                if (status == ConnectionStatus.Disconnected ||
                    status == ConnectionStatus.Disabled) {
                    // Force
                    IsClosed = true;
                    onConnectionLost?.Invoke();
                }
            }

            /// <summary>
            /// Helper to create device client
            /// </summary>
            /// <param name="cs"></param>
            /// <param name="transportSetting"></param>
            /// <returns></returns>
            private static DeviceClient Create(IotHubConnectionStringBuilder cs,
                ITransportSettings transportSetting) {
                if (cs == null) {
                    throw new ArgumentNullException(nameof(cs));
                }
                if (transportSetting != null) {
                    return DeviceClient.CreateFromConnectionString(cs.ToString(),
                        new ITransportSettings[] { transportSetting });
                }
                return DeviceClient.CreateFromConnectionString(cs.ToString());
            }

            private readonly DeviceClient _client;
            private int _reconnectCounter;
        }
    }
}

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
        /// Adapts module client to interface
        /// </summary>
        public sealed class ModuleClientAdapter : IClient {

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
            public string ModuleId { get; }

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="client"></param>
            /// <param name="deviceId"></param>
            /// <param name="moduleId"></param>
            internal ModuleClientAdapter(ModuleClient client, string deviceId, string moduleId) {
                _client = client ??
                    throw new ArgumentNullException(nameof(client));
                DeviceId = deviceId;
                ModuleId = moduleId;
            }

            /// <summary>
            /// Factory
            /// </summary>
            /// <param name="product"></param>
            /// <param name="cs"></param>
            /// <param name="deviceId"></param>
            /// <param name="moduleId"></param>
            /// <param name="transportSetting"></param>
            /// <param name="timeout"></param>
            /// <param name="retry"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<IClient> CreateAsync(string product,
                IotHubConnectionStringBuilder cs, string deviceId, string moduleId,
                ITransportSettings transportSetting,
                TimeSpan timeout, IRetryPolicy retry, Action onConnectionLost,
                ILogger logger) {

                if (cs == null) {
                    logger.Information("Running in iotedge context.");
                }
                else {
                    logger.Information("Running outside iotedge context.");
                }

                var client = await CreateAsync(cs, transportSetting);
                var adapter = new ModuleClientAdapter(client, deviceId, moduleId);

                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) =>
                    adapter.OnConnectionStatusChange(deviceId, moduleId, onConnectionLost,
                        logger, s, r));
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
                throw new NotSupportedException("Module client does not support upload");
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return _client.InvokeMethodAsync(deviceId, moduleId, methodRequest, cancellationToken);
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return _client.InvokeMethodAsync(deviceId, methodRequest, cancellationToken);
            }

            /// <inheritdoc />
            public Task SetStreamsDefaultHandlerAsync(StreamCallback streamHandler,
                object userContext) {
                throw new NotSupportedException("Module client does not support streams");
            }

            /// <inheritdoc />
            public Task SetStreamHandlerAsync(string streamName, StreamCallback
                streamHandler, object userContext) {
                throw new NotSupportedException("Module client does not support streams");
            }

            /// <inheritdoc />
            public Task<Stream> CreateStreamAsync(string streamName, string hostName,
                ushort port, CancellationToken cancellationToken) {
                throw new NotSupportedException("Module client does not support streams");
            }

            /// <inheritdoc />
            public void Dispose() {
                _client?.Dispose();
            }

            /// <summary>
            /// Handle status change event
            /// </summary>
            /// <param name="deviceId"></param>
            /// <param name="moduleId"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <param name="status"></param>
            /// <param name="reason"></param>
            private void OnConnectionStatusChange(string deviceId, string moduleId,
                Action onConnectionLost, ILogger logger, ConnectionStatus status,
                ConnectionStatusChangeReason reason) {

                if (status == ConnectionStatus.Connected) {
                    logger.Information("{counter}: Module {deviceId}_{moduleId} reconnected " +
                        "due to {reason}.", _reconnectCounter, deviceId, moduleId, reason);
                    _reconnectCounter++;
                    return;
                }
                logger.Information("{counter}: Module {deviceId}_{moduleId} disconnected " +
                    "due to {reason} - now {status}...", _reconnectCounter, deviceId, moduleId,
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
            /// Helper to create module client
            /// </summary>
            /// <param name="cs"></param>
            /// <param name="transportSetting"></param>
            /// <returns></returns>
            private static async Task<ModuleClient> CreateAsync(IotHubConnectionStringBuilder cs,
                ITransportSettings transportSetting) {
                if (transportSetting == null) {
                    if (cs == null) {
                        return await ModuleClient.CreateFromEnvironmentAsync();
                    }
                    return ModuleClient.CreateFromConnectionString(cs.ToString());
                }
                var ts = new ITransportSettings[] { transportSetting };
                if (cs == null) {
                    return await ModuleClient.CreateFromEnvironmentAsync(ts);
                }
                return ModuleClient.CreateFromConnectionString(cs.ToString(), ts);
            }

            private readonly ModuleClient _client;
            private int _reconnectCounter;
        }
    }
}

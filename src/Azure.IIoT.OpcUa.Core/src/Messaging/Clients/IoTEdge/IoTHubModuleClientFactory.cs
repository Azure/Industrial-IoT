// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using global::IoTHubby;
    using global::IoTHubby.Edge;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Default IoTHubby module-client factory.
    /// </summary>
    internal sealed class IoTHubModuleClientFactory : IIoTHubModuleClientFactory
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static IIoTHubModuleClientFactory Instance { get; } =
            new IoTHubModuleClientFactory();

        /// <inheritdoc/>
        public IIoTHubModuleClient Create(IoTEdgeClientOptions options,
            Action<IoTHubClientOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(configure);

            var client = string.IsNullOrEmpty(options.EdgeHubConnectionString)
                ? EdgeModuleClient.CreateFromEnvironmentAsync(configure)
                    .GetAwaiter().GetResult()
                : IoTHubModuleClient.CreateFromConnectionString(
                    options.EdgeHubConnectionString, configure);
            return new Adapter(client);
        }

        private IoTHubModuleClientFactory()
        {
        }

        internal sealed class Adapter : IIoTHubModuleClient
        {
            public Adapter(IoTHubModuleClient client)
            {
                Client = client ?? throw new ArgumentNullException(nameof(client));
            }

            public IoTHubModuleClient Client { get; }

            public IoTHubConnectionState State => Client.State;

            public event EventHandler<IoTHubConnectionStateChangedEventArgs>?
                ConnectionStateChanged
            {
                add => Client.ConnectionStateChanged += value;
                remove => Client.ConnectionStateChanged -= value;
            }

            public Task ConnectAsync(CancellationToken ct)
            {
                return Client.ConnectAsync(ct);
            }

            public ValueTask SendTelemetryAsync(TelemetryMessage message,
                CancellationToken ct)
            {
                return Client.SendTelemetryAsync(message, ct);
            }

            public ValueTask SendToOutputAsync(string outputName,
                TelemetryMessage message, CancellationToken ct)
            {
                return Client.SendToOutputAsync(outputName, message, ct);
            }

            public IAsyncEnumerable<CloudToDeviceMessage> ReceiveInputMessagesAsync(
                string inputName, CancellationToken ct)
            {
                return Client.ReceiveInputMessagesAsync(inputName, ct);
            }

            public Task SetMethodHandlerAsync(
                Func<DirectMethodRequest, CancellationToken,
                    ValueTask<DirectMethodResponse>>? handler,
                CancellationToken ct = default)
            {
                return Client.SetMethodHandlerAsync(handler, ct);
            }

            public Task<Twin> GetTwinAsync(CancellationToken ct)
            {
                return Client.GetTwinAsync(ct);
            }

            public Task<long?> UpdateReportedPropertiesAsync(string json,
                CancellationToken ct)
            {
                return Client.UpdateReportedPropertiesAsync(json, ct);
            }

            public ValueTask DisposeAsync()
            {
                return Client.DisposeAsync();
            }
        }
    }
}

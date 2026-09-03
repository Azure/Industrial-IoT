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

            if (string.IsNullOrEmpty(options.EdgeHubConnectionString))
            {
                return new Adapter(EdgeModuleClient
                    .CreateFromEnvironmentAsync(configure).GetAwaiter().GetResult());
            }
            if (IsDeviceConnectionString(options.EdgeHubConnectionString))
            {
#pragma warning disable CA2000 // Ownership transfers to DeviceAdapter.
                var client = IoTHubDeviceClient.CreateFromConnectionString(
                    options.EdgeHubConnectionString, configure);
#pragma warning restore CA2000
                return new DeviceAdapter(client);
            }
#pragma warning disable CA2000 // Ownership transfers to Adapter.
            var moduleClient = IoTHubModuleClient.CreateFromConnectionString(
                options.EdgeHubConnectionString, configure);
#pragma warning restore CA2000
            return new Adapter(moduleClient);
        }

        private IoTHubModuleClientFactory()
        {
        }

        private static bool IsDeviceConnectionString(string connectionString)
        {
            foreach (var segment in connectionString.Split(';',
                StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = segment.IndexOf('=', StringComparison.Ordinal);
                if (separator >= 0 &&
                    segment[..separator].Trim().Equals("ModuleId",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
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

        internal sealed class DeviceAdapter : IIoTHubModuleClient
        {
            public DeviceAdapter(IoTHubDeviceClient client)
            {
                _client = client ?? throw new ArgumentNullException(nameof(client));
            }

            public IoTHubConnectionState State => _client.State;

            public event EventHandler<IoTHubConnectionStateChangedEventArgs>?
                ConnectionStateChanged
            {
                add => _client.ConnectionStateChanged += value;
                remove => _client.ConnectionStateChanged -= value;
            }

            public Task ConnectAsync(CancellationToken ct)
            {
                return _client.ConnectAsync(ct);
            }

            public ValueTask SendTelemetryAsync(TelemetryMessage message,
                CancellationToken ct)
            {
                return _client.SendTelemetryAsync(message, ct);
            }

            public ValueTask SendToOutputAsync(string outputName,
                TelemetryMessage message, CancellationToken ct)
            {
                // Device identities have no module outputs. Stamping the name
                // onto the message would generate a module-shaped MQTT topic.
                _ = outputName;
                return _client.SendTelemetryAsync(message, ct);
            }

            public IAsyncEnumerable<CloudToDeviceMessage> ReceiveInputMessagesAsync(
                string inputName, CancellationToken ct)
            {
                _ = inputName;
                return _client.ReceiveCloudToDeviceMessagesAsync(ct);
            }

            public Task SetMethodHandlerAsync(
                Func<DirectMethodRequest, CancellationToken,
                    ValueTask<DirectMethodResponse>>? handler,
                CancellationToken ct = default)
            {
                return _client.SetMethodHandlerAsync(handler, ct);
            }

            public Task<Twin> GetTwinAsync(CancellationToken ct)
            {
                return _client.GetTwinAsync(ct);
            }

            public Task<long?> UpdateReportedPropertiesAsync(string json,
                CancellationToken ct)
            {
                return _client.UpdateReportedPropertiesAsync(json, ct);
            }

            public ValueTask DisposeAsync()
            {
                return _client.DisposeAsync();
            }

            private readonly IoTHubDeviceClient _client;
        }
    }
}

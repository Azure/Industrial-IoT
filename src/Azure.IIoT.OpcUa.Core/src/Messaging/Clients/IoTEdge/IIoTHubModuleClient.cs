// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using global::IoTHubby;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Narrow module-client operations used by the IoT Edge messaging stack.
    /// </summary>
    public interface IIoTHubModuleClient : IAsyncDisposable
    {
        /// <summary>
        /// Connection state.
        /// </summary>
        IoTHubConnectionState State { get; }

        /// <summary>
        /// Connection state changed.
        /// </summary>
        event EventHandler<IoTHubConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <summary>
        /// Connect.
        /// </summary>
        Task ConnectAsync(CancellationToken ct);

        /// <summary>
        /// Send telemetry.
        /// </summary>
        ValueTask SendTelemetryAsync(TelemetryMessage message, CancellationToken ct);

        /// <summary>
        /// Send telemetry to output.
        /// </summary>
        ValueTask SendToOutputAsync(string outputName, TelemetryMessage message,
            CancellationToken ct);

        /// <summary>
        /// Receive input messages.
        /// </summary>
        IAsyncEnumerable<CloudToDeviceMessage> ReceiveInputMessagesAsync(
            string inputName, CancellationToken ct);

        /// <summary>
        /// Set method handler.
        /// </summary>
        Task SetMethodHandlerAsync(Func<DirectMethodRequest, CancellationToken,
            ValueTask<DirectMethodResponse>>? handler, CancellationToken ct = default);

        /// <summary>
        /// Get twin.
        /// </summary>
        Task<Twin> GetTwinAsync(CancellationToken ct);

        /// <summary>
        /// Update reported properties.
        /// </summary>
        Task<long?> UpdateReportedPropertiesAsync(string json, CancellationToken ct);
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using Microsoft.Extensions.Logging;
    using System;

    /// <summary>
    /// Source-generated logging for <see cref="MqttClientTransport"/>.
    /// </summary>
    internal static partial class MqttClientTransportLogging
    {
        private const int EventClass = 300;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Debug,
            Message = "Retry call after timeout...")]
        public static partial void RetryCallAfterTimeout(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Result (Payload too large => {Length})")]
        public static partial void PayloadTooLarge(this ILogger logger, long length);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "Failed to publish rpc response")]
        public static partial void InvokerExecutionFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "Client failed to connect to {Host}:{Port}: {Reason}")]
        public static partial void ConnectFailed(this ILogger logger, string host,
            int port, string reason);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "Failed to stop rpc server handler")]
        public static partial void RpcServerStopFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Error,
            Message = "Failed to dispose mqtt client")]
        public static partial void ClientDisposeFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Debug,
            Message = "Failed to handle inbound message")]
        public static partial void MessageHandlingFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Warning,
            Message = "Mqtt client partitioning ({Partitions}) is not supported; " +
                "using a single client.")]
        public static partial void PartitioningNotSupported(this ILogger logger, int partitions);
    }
}

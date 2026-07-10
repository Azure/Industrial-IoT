// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using System;

    /// <summary>
    /// Legacy IoT Edge transport selector values. IoTHubby is MQTT-only.
    /// </summary>
    public enum TransportOption
    {
        /// <summary>No transport selected.</summary>
        None,
        /// <summary>MQTT.</summary>
        Mqtt,
        /// <summary>MQTT over TCP.</summary>
        MqttOverTcp,
        /// <summary>MQTT over web sockets.</summary>
        MqttOverWebsocket,
        /// <summary>AMQP over TCP (unsupported by IoTHubby).</summary>
        AmqpOverTcp,
        /// <summary>AMQP over web sockets (unsupported by IoTHubby).</summary>
        AmqpOverWebsocket
    }

    /// <summary>
    /// IoT Edge client configuration.
    /// </summary>
    public sealed class IoTEdgeClientOptions
    {
        /// <summary>
        /// EdgeHub connection string for local/dev bootstrap.
        /// </summary>
        public string? EdgeHubConnectionString { get; set; }

        /// <summary>
        /// Product name to use in the IoT Hub MQTT username.
        /// </summary>
        public string? Product { get; set; }

        /// <summary>
        /// MQTT keep-alive period in seconds.
        /// </summary>
        public int? KeepAlivePeriodSeconds { get; set; }

        /// <summary>
        /// Method call timeout.
        /// </summary>
        public TimeSpan? DefaultMethodCallTimeout { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;

    /// <summary>
    /// Mqtt transport configuration. This is a System.Text.Json / Mqtt.Client
    /// based subset of the former Legacy.Extensions.Mqtt options record - only the
    /// members bound by the publisher module configuration are retained.
    /// </summary>
    public record class MqttOptions
    {
        /// <summary>
        /// Client identity
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Protocol to use (default is v5)
        /// </summary>
        public MqttVersion Protocol { get; set; }

        /// <summary>
        /// Host name of broker
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Broker port
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Credential
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Password file
        /// </summary>
        public string? PasswordFile { get; set; }

        /// <summary>
        /// Quality of service
        /// </summary>
        public QoS? QoS { get; set; }

        /// <summary>
        /// Whether to use tls
        /// </summary>
        public bool? UseTls { get; set; }

        /// <summary>
        /// Whether to accept any certificate
        /// </summary>
        public bool? AllowUntrustedCertificates { get; set; }

        /// <summary>
        /// Path to use if web socket should be used
        /// </summary>
        public string? WebSocketPath { get; set; }

        /// <summary>
        /// Max payload sizes
        /// </summary>
        public int? MaxPayloadSize { get; set; }

        /// <summary>
        /// Reconnection delay
        /// </summary>
        public TimeSpan? ReconnectDelay { get; set; }

        /// <summary>
        /// Default method call timeout.
        /// </summary>
        public TimeSpan? DefaultMethodCallTimeout { get; set; }

        /// <summary>
        /// How many times to retry on method call timeouts
        /// before throwing exception.
        /// </summary>
        public int? MethodCallTimeoutRetries { get; set; }

        /// <summary>
        /// Keep alive timer duration.
        /// </summary>
        public TimeSpan? KeepAlivePeriod { get; set; }

        /// <summary>
        /// Number of clients to create to partition topics across a broker's
        /// load balanced nodes. (Currently only a single client is created; a
        /// value greater than one is treated as one.)
        /// </summary>
        public int? NumberOfClientPartitions { get; set; }

        /// <summary>
        /// Clean start
        /// </summary>
        public bool? CleanStart { get; set; }

        /// <summary>
        /// Session expiry
        /// </summary>
        public TimeSpan? SessionExpiry { get; set; }

        /// <summary>
        /// Max receive
        /// </summary>
        public ushort? ReceiveMaximum { get; set; }

        /// <summary>
        /// Max request queue - default unbounded
        /// </summary>
        public int? MaxRequestQueue { get; set; }

        /// <summary>
        /// Hook to rewrite the message topic when publishing a schema. The
        /// action receives the telemetry message topic and can replace it with
        /// the schema topic. Mirrors the former Legacy ConfigureSchemaMessage.
        /// </summary>
        public Action<MqttSchemaMessage>? ConfigureSchemaMessage { get; set; }
    }

    /// <summary>
    /// A mutable schema message topic holder passed to
    /// <see cref="MqttOptions.ConfigureSchemaMessage"/>.
    /// </summary>
    public sealed class MqttSchemaMessage
    {
        /// <summary>
        /// Topic of the message. Set to rewrite where the schema is published.
        /// </summary>
        public string Topic { get; set; } = string.Empty;
    }
}

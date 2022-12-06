// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using System;

    /// <summary>
    /// This class represent the MQTT client connection string
    /// </summary>
    public sealed class MqttClientConnectionString {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientConnectionString"/> class.
        /// </summary>
        public MqttClientConnectionString(MqttClientConnectionStringBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            HostName = builder.HostName;
            Port = builder.Port;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;
            Username = builder.Username;
            Password = builder.Password;
            StateFile = builder.StateFile;
            MessageExpiryInterval = builder.MessageExpiryInterval;
        }

        /// <summary>
        /// Gets the value of the fully-qualified DNS hostname of the MQTT server.
        /// </summary>
        public string HostName { get; }

        /// <summary>
        /// Gets the port number of the MQTT server.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Gets the module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the user name to connect to the MQTT server.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the password to connect to the MQTT server.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Gets the state file path to be used to persist the MQTT client state.
        /// </summary>
        public string StateFile { get; }

        /// <summary>
        /// Gets the period of time (seconds) for the broker to store the message for any subscribers that are not yet connected.
        /// </summary>
        public uint? MessageExpiryInterval { get; }
    }
}

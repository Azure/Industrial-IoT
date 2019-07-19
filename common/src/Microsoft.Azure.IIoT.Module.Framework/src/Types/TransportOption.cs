// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System;

    /// <summary>
    /// Transport host should use
    /// </summary>
    [Flags]
    public enum TransportOption {

        /// <summary>
        /// Amqp over ssl/tcp
        /// </summary>
        AmqpOverTcp = 0x1,

        /// <summary>
        /// Amqp over websocket
        /// </summary>
        AmqpOverWebsocket = 0x2,

        /// <summary>
        /// Mqtt over ssl/tcp
        /// </summary>
        MqttOverTcp = 0x4,

        /// <summary>
        /// Mqtt over websocket
        /// </summary>
        MqttOverWebsocket = 0x8,

        /// <summary>
        /// Amqp over ssl or websocket
        /// </summary>
        Amqp = AmqpOverTcp | AmqpOverWebsocket,

        /// <summary>
        /// Mqtt over ssl or websocket
        /// </summary>
        Mqtt = MqttOverTcp | MqttOverWebsocket,

        /// <summary>
        /// Use all possible transports
        /// </summary>
        Any = AmqpOverWebsocket | MqttOverWebsocket
    }
}

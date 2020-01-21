// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System;

    /// <summary>
    /// Transport types the client adapter should use
    /// </summary>
    [Flags]
    public enum TransportOption {

        /// <summary>
        /// Amqp over tcp/ssl
        /// </summary>
        AmqpOverTcp = 0x1,

        /// <summary>
        /// Amqp over websocket
        /// </summary>
        AmqpOverWebsocket = 0x2,

        /// <summary>
        /// Mqtt over tcp/ssl
        /// </summary>
        MqttOverTcp = 0x4,

        /// <summary>
        /// Mqtt over websocket
        /// </summary>
        MqttOverWebsocket = 0x8,

        /// <summary>
        /// Amqp over tcp/ssl or websocket
        /// </summary>
        Amqp = AmqpOverTcp | AmqpOverWebsocket,

        /// <summary>
        /// Mqtt over tcp/ssl or websocket
        /// </summary>
        Mqtt = MqttOverTcp | MqttOverWebsocket,

        /// <summary>
        /// Tcp only
        /// </summary>
        Tcp = AmqpOverTcp | MqttOverTcp,

        /// <summary>
        /// Websocket only
        /// </summary>
        Websocket = AmqpOverWebsocket | MqttOverWebsocket,

        /// <summary>
        /// Use all possible transports
        /// </summary>
        Any = Amqp | Mqtt,
    }
}

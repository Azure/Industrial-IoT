// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    /// <summary>
    /// Mqtt versions (mirror of the former Furly.Extensions.Mqtt.MqttVersion so
    /// that <c>MqttProtocolVersion</c> parsing from configuration keeps working).
    /// </summary>
    public enum MqttVersion
    {
        /// <summary>
        /// Version is v5 (default)
        /// </summary>
        v5,

        /// <summary>
        /// Mqtt version is v3.1.1
        /// </summary>
        v311
    }
}

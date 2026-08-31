// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Fills default mqtt option values. Mirror of the former internal
    /// Legacy.Extensions.Mqtt.Runtime.MqttConfig defaults.
    /// </summary>
    public sealed class MqttConfig : IPostConfigureOptions<MqttOptions>
    {
        /// <inheritdoc/>
        public void PostConfigure(string? name, MqttOptions options)
        {
            if (string.IsNullOrEmpty(options.HostName))
            {
                options.HostName = "localhost";
            }
            var useWebSocket = options.WebSocketPath != null;
            options.UseTls ??= options.Port != null &&
                options.Port != (useWebSocket ? 80 : 1883);
            options.Port ??= useWebSocket
                ? (options.UseTls == true ? 443 : 80)
                : (options.UseTls == true ? 8883 : 1883);
            options.QoS ??= QoS.AtMostOnce;
            options.ClientId ??= Guid.NewGuid().ToString();
            if (options.ReconnectDelay == TimeSpan.Zero)
            {
                options.ReconnectDelay = null;
            }
        }
    }
}

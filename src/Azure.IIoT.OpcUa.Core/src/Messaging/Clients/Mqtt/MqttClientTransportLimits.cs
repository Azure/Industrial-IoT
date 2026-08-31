// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;

    internal static class MqttClientTransportLimits
    {
        internal const int kMqttMaximumPacketSize = 268435455;

        internal static int GetPayloadSizeLimit(int? maxPayloadSize)
        {
            return maxPayloadSize is > 0
                ? Math.Min(maxPayloadSize.Value, kMqttMaximumPacketSize)
                : kMqttMaximumPacketSize;
        }
    }
}

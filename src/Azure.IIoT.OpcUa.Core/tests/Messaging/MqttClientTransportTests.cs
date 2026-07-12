// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using FluentAssertions;
    using Xunit;

    /// <summary>
    /// Locks the MQTT payload-size policy used by the transport for advertised
    /// method and event limits.
    /// </summary>
    public sealed class MqttClientTransportTests
    {
        public static TheoryData<int?, int> PayloadSizeLimitCases { get; } = new()
        {
            { null, MqttClientTransportLimits.kMqttMaximumPacketSize },
            { -1, MqttClientTransportLimits.kMqttMaximumPacketSize },
            { 0, MqttClientTransportLimits.kMqttMaximumPacketSize },
            { MqttClientTransportLimits.kMqttMaximumPacketSize - 1,
                MqttClientTransportLimits.kMqttMaximumPacketSize - 1 },
            { MqttClientTransportLimits.kMqttMaximumPacketSize,
                MqttClientTransportLimits.kMqttMaximumPacketSize },
            { MqttClientTransportLimits.kMqttMaximumPacketSize + 1,
                MqttClientTransportLimits.kMqttMaximumPacketSize }
        };

        [Theory]
        [MemberData(nameof(PayloadSizeLimitCases))]
        public void GetPayloadSizeLimitReturnsExpectedValue(int? configuredLimit, int expectedLimit)
        {
            MqttClientTransportLimits.GetPayloadSizeLimit(configuredLimit)
                .Should().Be(expectedLimit);
        }
    }
}

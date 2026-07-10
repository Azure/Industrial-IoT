// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using global::Mqtt.Client;
    using FluentAssertions;
    using System;
    using Xunit;

    /// <summary>
    /// Locks the MQTT direct-method (rpc) request/response wire protocol - topic
    /// strings, correlation scheme and status envelope - that must remain
    /// byte-for-byte compatible with the former Furly.Extensions.Mqtt client.
    /// </summary>
    public sealed class MqttRpcProtocolTests
    {
        [Fact]
        public void V5ResponseTopicUsesResponsesSuffix()
        {
            MqttRpcProtocol.V5ResponseTopic("client-1")
                .Should().Be("client-1/responses");
        }

        [Fact]
        public void V5RequestTopicIsTargetSlashMethod()
        {
            MqttRpcProtocol.V5RequestTopic("device/module", "Publish_V1")
                .Should().Be("device/module/Publish_V1");
        }

        [Fact]
        public void V311RequestTopicCarriesRequestIdInPath()
        {
            var rid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            MqttRpcProtocol.V311RequestTopic("device/module", "Publish_V1", rid)
                .Should().Be("device/module/Publish_V1/?$rid=11111111-2222-3333-4444-555555555555");
        }

        [Fact]
        public void V311ResponseFilterMatchesStatusAndRequestId()
        {
            MqttRpcProtocol.V311ResponseFilter("client-1")
                .Should().Be("client-1/res/+/+");
        }

        [Fact]
        public void V311ResponseTopicEncodesStatusAndRequestId()
        {
            var rid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            MqttRpcProtocol.V311ResponseTopic("client-1", 200, rid)
                .Should().Be("client-1/res/200/?$rid=11111111-2222-3333-4444-555555555555");
        }

        [Theory]
        [InlineData(200)]
        [InlineData(400)]
        [InlineData(500)]
        public void V311ResponseTopicRoundTripsThroughParse(int status)
        {
            var rid = Guid.NewGuid();
            var topic = MqttRpcProtocol.V311ResponseTopic("client-1", status, rid);

            MqttRpcProtocol.TryParseV311Response(topic, "client-1",
                out var parsedStatus, out var parsedRid).Should().BeTrue();
            parsedStatus.Should().Be(status);
            parsedRid.Should().Be(rid);
        }

        [Fact]
        public void TryParseV311ResponseRejectsMalformedTopic()
        {
            MqttRpcProtocol.TryParseV311Response("client-1/res/notanumber/?$rid=x",
                "client-1", out _, out _).Should().BeFalse();
        }

        [Fact]
        public void ParseMessageClassifiesV5RequestByResponseTopic()
        {
            var rid = Guid.NewGuid();
            var topic = MqttRpcProtocol.V5RequestTopic("device/module", "Publish_V1");

            MqttRpcProtocol.ParseMessage(topic, rid.ToByteArray(),
                responseTopic: "client-1/responses", out var isRequest,
                out var parsedRid, out var method, out _).Should().BeTrue();

            isRequest.Should().BeTrue();
            method.Should().Be("Publish_V1");
            parsedRid.Should().Be(rid);
        }

        [Fact]
        public void ParseMessageClassifiesV5ResponseByAbsentResponseTopic()
        {
            var rid = Guid.NewGuid();
            var topic = "client-1/responses/Publish_V1";

            MqttRpcProtocol.ParseMessage(topic, rid.ToByteArray(),
                responseTopic: null, out var isRequest, out var parsedRid,
                out _, out _).Should().BeTrue();

            isRequest.Should().BeFalse();
            parsedRid.Should().Be(rid);
        }

        [Fact]
        public void ParseMessageClassifiesV311Request()
        {
            var rid = Guid.NewGuid();
            var topic = MqttRpcProtocol.V311RequestTopic("device/module", "Publish_V1", rid);

            MqttRpcProtocol.ParseMessage(topic, correlationData: null,
                responseTopic: null, out var isRequest, out var parsedRid,
                out var method, out var topicRoot).Should().BeTrue();

            isRequest.Should().BeTrue();
            method.Should().Be("Publish_V1");
            parsedRid.Should().Be(rid);
            topicRoot.Should().Be("device/module");
        }

        [Fact]
        public void ParseMessageClassifiesV311Response()
        {
            var rid = Guid.NewGuid();
            var topic = MqttRpcProtocol.V311ResponseTopic("client-1", 200, rid);

            MqttRpcProtocol.ParseMessage(topic, correlationData: null,
                responseTopic: null, out var isRequest, out var parsedRid,
                out var method, out var topicRoot).Should().BeTrue();

            isRequest.Should().BeFalse();
            method.Should().BeNull();
            parsedRid.Should().Be(rid);
            topicRoot.Should().Be("client-1");
        }

        [Fact]
        public void ParseMessageRejectsNonRpcTopic()
        {
            MqttRpcProtocol.ParseMessage("device/module/telemetry",
                correlationData: null, responseTopic: null, out _, out _,
                out _, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData(QoS.AtMostOnce, MqttQoS.AtMostOnce)]
        [InlineData(QoS.AtLeastOnce, MqttQoS.AtLeastOnce)]
        [InlineData(QoS.ExactlyOnce, MqttQoS.ExactlyOnce)]
        public void QoSCastMatchesMqttQoS(QoS coreQoS, MqttQoS expected)
        {
            // The transport maps QoS to MqttQoS via a direct numeric cast; assert
            // the enum values stay aligned so the wire QoS is preserved.
            ((MqttQoS)coreQoS).Should().Be(expected);
        }
    }
}

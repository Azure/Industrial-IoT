// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using FluentAssertions;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
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

        [Fact]
        public void ConstructorRejectsNullDependencies()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MqttClientTransport(null!, NullLogger<MqttClientTransport>.Instance));
            Assert.Throws<ArgumentNullException>(() =>
                new MqttClientTransport(Options.Create(new MqttOptions()), null!));
        }

        [Theory]
        [InlineData(MqttVersion.v311, false,
            EventClientCapabilities.Payload |
            EventClientCapabilities.Topic |
            EventClientCapabilities.QualityOfService |
            EventClientCapabilities.Retain |
            EventClientCapabilities.TransportSecurity |
            EventClientCapabilities.Authentication)]
        [InlineData(MqttVersion.v5, false,
            EventClientCapabilities.Payload |
            EventClientCapabilities.Topic |
            EventClientCapabilities.QualityOfService |
            EventClientCapabilities.Retain |
            EventClientCapabilities.TimeToLive |
            EventClientCapabilities.ContentType |
            EventClientCapabilities.ContentEncoding |
            EventClientCapabilities.CustomProperties |
            EventClientCapabilities.CloudEvents |
            EventClientCapabilities.TransportSecurity |
            EventClientCapabilities.Authentication)]
        [InlineData(MqttVersion.v5, true,
            EventClientCapabilities.Payload |
            EventClientCapabilities.Topic |
            EventClientCapabilities.QualityOfService |
            EventClientCapabilities.Retain |
            EventClientCapabilities.TimeToLive |
            EventClientCapabilities.ContentType |
            EventClientCapabilities.ContentEncoding |
            EventClientCapabilities.CustomProperties |
            EventClientCapabilities.CloudEvents |
            EventClientCapabilities.Schema |
            EventClientCapabilities.TransportSecurity |
            EventClientCapabilities.Authentication)]
        public void GetCapabilitiesReflectsProtocolAndSchemaSupport(
            MqttVersion version, bool supportsSchema,
            EventClientCapabilities expected)
        {
            var capabilities = MqttClientTransport.GetCapabilities(
                version, supportsSchema);

            Assert.Equal(expected, capabilities);
        }

        [Theory]
        [InlineData(null, null, null, 1883, false)]
        [InlineData(null, null, true, 8883, true)]
        [InlineData("/mqtt", null, null, 80, false)]
        [InlineData("/mqtt", null, true, 443, true)]
        [InlineData("/mqtt", 80, null, 80, false)]
        [InlineData("/mqtt", 443, null, 443, true)]
        public void PostConfigureUsesTransportSpecificDefaults(string? webSocketPath,
            int? port, bool? useTls, int expectedPort, bool expectedTls)
        {
            var options = new MqttOptions
            {
                WebSocketPath = webSocketPath,
                Port = port,
                UseTls = useTls
            };

            new MqttConfig().PostConfigure(null, options);

            options.Port.Should().Be(expectedPort);
            options.UseTls.Should().Be(expectedTls);
        }

        [Theory]
        [InlineData(null, null, null, false, "mqtt://broker:1883", false, false)]
        [InlineData(null, 8883, null, false, "mqtts://broker:8883", true, false)]
        [InlineData(false, 8883, null, true, "mqtt://broker:8883", false, false)]
        [InlineData(true, null, null, true, "mqtts://broker:8883", true, true)]
        [InlineData(false, null, "/mqtt", false, "ws://broker:80", false, false)]
        [InlineData(true, null, "/mqtt", false, "wss://broker:443", true, false)]
        [InlineData(false, 8080, "/mqtt", false, "ws://broker:8080", false, false)]
        [InlineData(true, 8443, "/mqtt", false, "wss://broker:8443", true, false)]
        public void ConnectionSettingsMakeTlsBehaviorExplicit(bool? useTls, int? port,
            string? webSocketPath, bool allowUntrusted, string expectedEndpoint,
            bool expectedTls, bool expectedAllowUntrusted)
        {
            var settings = MqttClientTransport.GetConnectionSettings(new MqttOptions
            {
                HostName = "broker",
                Port = port,
                UseTls = useTls,
                WebSocketPath = webSocketPath,
                AllowUntrustedCertificates = allowUntrusted
            });

            settings.Endpoint.Should().Be(expectedEndpoint);
            settings.UseTls.Should().Be(expectedTls);
            settings.AllowUntrustedCertificates.Should().Be(expectedAllowUntrusted);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitWebSocketPort1883FailsRatherThanBeingRewritten(bool useTls)
        {
            var action = () => MqttClientTransport.GetConnectionSettings(new MqttOptions
            {
                HostName = "broker",
                Port = 1883,
                UseTls = useTls,
                WebSocketPath = "/mqtt"
            });

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*rewrites WebSocket port 1883*");
        }

        [Fact]
        public void WssRejectsUnsupportedCustomCertificateValidation()
        {
            var action = () => MqttClientTransport.GetConnectionSettings(new MqttOptions
            {
                HostName = "broker",
                UseTls = true,
                WebSocketPath = "/mqtt",
                AllowUntrustedCertificates = true
            });

            action.Should().Throw<NotSupportedException>()
                .WithMessage("*does not apply custom certificate validation*");
        }

        [Theory]
        [InlineData("ws://broker:80", 80,
            global::Mqtt.Client.MqttTransportType.WebSocket)]
        [InlineData("wss://broker:443", 443,
            global::Mqtt.Client.MqttTransportType.WebSocketSecure)]
        [InlineData("ws://broker:8080", 8080,
            global::Mqtt.Client.MqttTransportType.WebSocket)]
        [InlineData("wss://broker:8443", 8443,
            global::Mqtt.Client.MqttTransportType.WebSocketSecure)]
        public async Task PinnedClientBuildPreservesSupportedWebSocketPortsAsync(
            string endpoint, int expectedPort,
            global::Mqtt.Client.MqttTransportType expectedTransport)
        {
            global::Mqtt.Client.MqttClientOptions? actual = null;
            await using var client = global::Mqtt.Client.MqttClient.CreateBuilder()
                .ConnectTo(endpoint)
                .Configure(options =>
                {
                    options.WebSocketPath = "/mqtt";
                    actual = options;
                })
                .Build();

            actual.Should().NotBeNull();
            actual!.Port.Should().Be(expectedPort);
            actual.Transport.Should().Be(expectedTransport);
        }

        [Fact]
        public void MissingUserNameDoesNotLoadOrSendCredentials()
        {
            var credentials = MqttClientTransport.GetCredentials(new MqttOptions
            {
                Password = "ignored",
                PasswordFile = "missing-password-file"
            });

            credentials.Should().BeNull();
        }

        [Fact]
        public void InlinePasswordTakesPrecedenceOverPasswordFile()
        {
            var credentials = MqttClientTransport.GetCredentials(new MqttOptions
            {
                UserName = "publisher",
                Password = "inline",
                PasswordFile = "missing-password-file"
            });

            credentials.HasValue.Should().BeTrue();
            var actual = credentials.GetValueOrDefault();
            actual.UserName.Should().Be("publisher");
            actual.Password.Should().Equal(Encoding.UTF8.GetBytes("inline"));
        }

        [Fact]
        public void UserNameWithoutPasswordUsesEmptyPassword()
        {
            var credentials = MqttClientTransport.GetCredentials(new MqttOptions
            {
                UserName = "publisher"
            });

            credentials.HasValue.Should().BeTrue();
            credentials.GetValueOrDefault().Password.Should().BeEmpty();
        }

        [Fact]
        public void PasswordFileIsUsedWhenInlinePasswordIsAbsent()
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                $"mqtt-password-{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllBytes(path, Encoding.UTF8.GetBytes("from-file"));

                var credentials = MqttClientTransport.GetCredentials(new MqttOptions
                {
                    UserName = "publisher",
                    PasswordFile = path
                });

                credentials.HasValue.Should().BeTrue();
                var actual = credentials.GetValueOrDefault();
                actual.UserName.Should().Be("publisher");
                actual.Password.Should().Equal(
                    Encoding.UTF8.GetBytes("from-file"));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.Tests {
    using Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient;
    using Moq;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Packets;
    using MQTTnet.Protocol;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [Collection("MqttClientTests")]
    public class MqttClientAdapterTests : MqttClientConnectionStringBuilderTestsBase {
        private readonly ILogger _logger;
        private readonly MqttClientConnectionStringBuilder _mqttClientConnectionStringBuilder;

        public MqttClientAdapterTests() {
            _logger = new Mock<ILogger>().Object;
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            _mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create("HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860;StateFile=file1;Protocol=v500");
        }

        [Fact]
        public async Task ConnectTest() {
            var mock = new Mock<IManagedMqttClient>();
            mock.SetupGet(x => x.IsStarted).Returns(false);
            mock.SetupGet(x => x.InternalClient).Returns(new Mock<IMqttClient>().Object);
            _ = await MqttClientAdapter.CreateAsync(mock.Object, _mqttClientConnectionStringBuilder, "device1", "/topic/{device_id}", TimeSpan.Zero, _logger);

            mock.VerifyAdd(x => x.ConnectedAsync += It.IsAny<Func<MqttClientConnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectingFailedAsync += It.IsAny<Func<ConnectingFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectionStateChangedAsync += It.IsAny<Func<EventArgs, Task>>());
            mock.VerifyAdd(x => x.SynchronizingSubscriptionsFailedAsync += It.IsAny<Func<ManagedProcessFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.DisconnectedAsync += It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>());
            mock.Verify(x => x.StartAsync(
                It.Is<ManagedMqttClientOptions>(x =>
                    x.ClientOptions.ChannelOptions is MqttClientTcpOptions &&
                    x.ClientOptions.ProtocolVersion == MqttProtocolVersion.V500 &&
                    string.Equals(x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().Server, "hub1.azure-devices.net") &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().Port == 8883 &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().BufferSize == GetExpectedBufferSize() &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions != null &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.UseTls &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.Certificates.Count == 1 &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.SslProtocol == SslProtocols.Tls12 &&
                   // !string.IsNullOrWhiteSpace(x.ClientOptions.Credentials.GetUserName()) &&
                   // x.ClientOptions.Credentials.GetPassword().Length > 0 &&
                    x.Storage is ManagedMqttClientStorage)));
            mock.Verify(x => x.SubscribeAsync(
                It.Is<ICollection<MqttTopicFilter>>(x =>
                    x.Count == 3 &&
                    string.Equals(x.First().Topic, "$iothub/twin/res/#") &&
                    string.Equals(x.Skip(1).First().Topic, "$iothub/twin/PATCH/properties/desired/#") &&
                    string.Equals(x.Last().Topic, "$iothub/methods/#"))));
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendIoTHubEventTest() {
            const string payload = @"{ ""key"": ""value"" }";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var mock = new Mock<IManagedMqttClient>();

            mock.SetupGet(x => x.IsStarted).Returns(true);
            var mqttClientAdapter = await MqttClientAdapter.CreateAsync(mock.Object, _mqttClientConnectionStringBuilder,
                "device1", "/topic/{device_id}", TimeSpan.FromMinutes(5), _logger);

            var message = mqttClientAdapter.CreateTelemetryEvent();
            message.Payload = new[] { payloadBytes };
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";
            message.OutputName = "testoutput";
            message.Ttl = TimeSpan.FromSeconds(1234);
            message.Retain = true;

            await mqttClientAdapter.SendEventAsync(message);

            mock.Verify(x => x.EnqueueAsync(
                It.Is<MqttApplicationMessage>(x =>
                    string.Equals(x.ContentType, "application/json") &&
                    string.Equals(x.Topic, "devices/device1/messages/events/iothub-content-type=application%2Fjson&iothub-message-schema=application%2Fjson&iothub-content-encoding=utf-8&%24%24ContentEncoding=utf-8/") &&
                    x.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce &&
                    string.Equals(Encoding.UTF8.GetString(x.Payload), payload) &&
                    x.Retain == true &&
                    x.MessageExpiryInterval == 1234)), Times.Once);
        }

        [Fact]
        public async Task SendBrokerEventTest1() {
            const string payload = @"{ ""key"": ""value"" }";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var mock = new Mock<IManagedMqttClient>();

            mock.SetupGet(x => x.IsStarted).Returns(true);
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create("HostName=localhost;DeviceId=deviceId;Username=user;Password=SAHEh7J7dPzpIh;Protocol=v500");
            var mqttClientAdapter = await MqttClientAdapter.CreateAsync(mock.Object, mqttClientConnectionStringBuilder,
                "device1", "/topic/{device_id}", TimeSpan.FromMinutes(5), _logger);

            var message = mqttClientAdapter.CreateTelemetryEvent();
            message.Payload = new[] { payloadBytes, payloadBytes, payloadBytes };
            message.ContentType = "application/json";
            message.OutputName = "testoutput";
            message.ContentEncoding = "utf-8";
            message.Ttl = TimeSpan.FromSeconds(1234);
            message.Retain = true;

            await mqttClientAdapter.SendEventAsync(message);

            mock.Verify(x => x.EnqueueAsync(
                It.Is<MqttApplicationMessage>(x =>
                    string.Equals(x.ContentType, "application/json") &&
                    string.Equals(x.Topic, "/topic/device1/testoutput") &&
                    x.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce &&
                    string.Equals(Encoding.UTF8.GetString(x.Payload), payload) &&
                    x.Retain == true &&
                    x.MessageExpiryInterval == 1234)), Times.Exactly(3));
        }

        [Fact]
        public async Task SendBrokerEventTest2() {
            const string payload = @"{ ""key"": ""value"" }";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var mock = new Mock<IManagedMqttClient>();

            mock.SetupGet(x => x.IsStarted).Returns(true);
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create("HostName=localhost;DeviceId=deviceId;Username=user;Password=SAHEh7J7dPzpIh;Protocol=v500");
            var mqttClientAdapter = await MqttClientAdapter.CreateAsync(mock.Object, mqttClientConnectionStringBuilder,
                "device1", "/topic/{output_name}/super", TimeSpan.FromMinutes(5), _logger);

            var message = mqttClientAdapter.CreateTelemetryEvent();
            message.Payload = new[] { payloadBytes, null, payloadBytes };
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";

            await mqttClientAdapter.SendEventAsync(message);

            mock.Verify(x => x.EnqueueAsync(
                It.Is<MqttApplicationMessage>(x =>
                    string.Equals(x.ContentType, "application/json") &&
                    string.Equals(x.Topic, "/topic/super") &&
                    x.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce &&
                    string.Equals(Encoding.UTF8.GetString(x.Payload), payload) &&
                    x.Retain == false &&
                    x.MessageExpiryInterval == 0)), Times.Exactly(2));
        }

        [Fact]
        public async Task GetTwinTest() {
            var mock = new Mock<IManagedMqttClient>();
            mock.SetupGet(x => x.IsStarted).Returns(true);
            var mqttClientAdapter = await MqttClientAdapter.CreateAsync(mock.Object, _mqttClientConnectionStringBuilder, "device1", "/topic/{device_id}", TimeSpan.FromSeconds(0), _logger);
            await mqttClientAdapter.GetTwinAsync();

            mock.Verify(x => x.EnqueueAsync(
                It.Is<MqttApplicationMessage>(x =>
                    string.Equals(x.ContentType, "application/json") &&
                    x.Topic.StartsWith("$iothub/twin/GET/?$rid="))));
        }

        [Fact]
        public async Task IsClosedTest() {
            const string payload = @"{ ""key"": ""value"" }";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            var mock = new Mock<IManagedMqttClient>();
            mock.SetupGet(x => x.IsStarted).Returns(false);
            mock.SetupGet(x => x.InternalClient).Returns(new Mock<IMqttClient>().Object);

            using var mqttClientAdapter = await MqttClientAdapter.CreateAsync(mock.Object,
                _mqttClientConnectionStringBuilder, "device1", "/topic/{device_id}", TimeSpan.Zero, _logger);
            var message = mqttClientAdapter.CreateTelemetryEvent();
            message.Payload = new[] { payloadBytes };
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";

            await mqttClientAdapter.DisposeAsync();
            await mqttClientAdapter.SendEventAsync(message);

            mock.VerifyAdd(x => x.ConnectedAsync += It.IsAny<Func<MqttClientConnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectingFailedAsync += It.IsAny<Func<ConnectingFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectionStateChangedAsync += It.IsAny<Func<EventArgs, Task>>());
            mock.VerifyAdd(x => x.SynchronizingSubscriptionsFailedAsync += It.IsAny<Func<ManagedProcessFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.DisconnectedAsync += It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>());
            mock.Verify(x => x.StartAsync(It.IsAny<ManagedMqttClientOptions>()));
            mock.Verify(x => x.SubscribeAsync(It.IsAny<ICollection<MqttTopicFilter>>()));
            mock.Verify(x => x.StopAsync(It.IsAny<bool>()));
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ConnectCanceledTest() {
            var mock = new Mock<IManagedMqttClient>();
            mock.SetupGet(x => x.IsStarted).Returns(false);
            mock.SetupGet(x => x.InternalClient).Returns(new Mock<IMqttClient>().Object);
            mock.Setup(x => x.StartAsync(It.IsAny<ManagedMqttClientOptions>())).Returns(() => { throw new TaskCanceledException(); });
            try {
                _ = await MqttClientAdapter.CreateAsync(mock.Object, _mqttClientConnectionStringBuilder, "device1", "/topic/{device_id}", TimeSpan.Zero, _logger);
            }
            catch (TaskCanceledException) {
            }
            mock.VerifyAdd(x => x.ConnectedAsync += It.IsAny<Func<MqttClientConnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectingFailedAsync += It.IsAny<Func<ConnectingFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ConnectionStateChangedAsync += It.IsAny<Func<EventArgs, Task>>());
            mock.VerifyAdd(x => x.SynchronizingSubscriptionsFailedAsync += It.IsAny<Func<ManagedProcessFailedEventArgs, Task>>());
            mock.VerifyAdd(x => x.DisconnectedAsync += It.IsAny<Func<MqttClientDisconnectedEventArgs, Task>>());
            mock.VerifyAdd(x => x.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>());
            mock.Verify(x => x.StartAsync(
                It.Is<ManagedMqttClientOptions>(x =>
                    x.ClientOptions.ChannelOptions is MqttClientTcpOptions &&
                    x.ClientOptions.ProtocolVersion == MqttProtocolVersion.V500 &&
                    string.Equals(x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().Server, "hub1.azure-devices.net") &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().Port == 8883 &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().BufferSize == GetExpectedBufferSize() &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions != null &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.UseTls &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.Certificates.Count == 1 &&
                    x.ClientOptions.ChannelOptions.As<MqttClientTcpOptions>().TlsOptions.SslProtocol == SslProtocols.Tls12 &&
                 //   !string.IsNullOrWhiteSpace(x.ClientOptions.Credentials.Username) &&
                 //   x.ClientOptions.Credentials.Password.Length > 0 &&
                    x.Storage is ManagedMqttClientStorage)));
            mock.VerifyNoOtherCalls();
        }

        private static uint GetExpectedBufferSize() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return 64 * 1024;
            }
            return 8 * 1024;
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.Tests {
    using Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient;
    using MQTTnet.Formatter;
    using System;
    using System.Collections.Generic;
    using Xunit;

    [Collection("MqttClientTests")]
    public class MqttClientConnectionStringBuilderTests : MqttClientConnectionStringBuilderTestsBase {
        [Fact]
        public void ValidGenericConnectionStringTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=127.0.0.1;DeviceId=device1;ModuleId=module1;Username=username1;Password=password1";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("127.0.0.1", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            Assert.Equal("username1", mqttClientConnectionStringBuilder.Username);
            Assert.Equal("password1", mqttClientConnectionStringBuilder.Password);
            Assert.Equal(1883, mqttClientConnectionStringBuilder.Port);
            Assert.False(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.False(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(MqttProtocolVersion.V311, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void ValidIoTHubConnectionStringTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("hub1.azure-devices.net", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal("SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860", mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Equal(8883, mqttClientConnectionStringBuilder.Port);
            Assert.True(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.True(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(MqttProtocolVersion.V311, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void OverrideUsingIoTHubTest() {
            // Use a HostName which cannot automatically be detected as an Azure IoT Hub.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.invalid.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860;UsingIoTHub=true";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("hub1.invalid.net", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal("SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860", mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Equal(8883, mqttClientConnectionStringBuilder.Port);
            Assert.True(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.True(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(MqttProtocolVersion.V311, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void OverridePortTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=127.0.0.1;DeviceId=device1;ModuleId=module1;Username=username1;Password=password1;Port=1234";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("127.0.0.1", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            Assert.Equal("username1", mqttClientConnectionStringBuilder.Username);
            Assert.Equal("password1", mqttClientConnectionStringBuilder.Password);
            Assert.Equal(1234, mqttClientConnectionStringBuilder.Port);
            Assert.False(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.False(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(MqttProtocolVersion.V311, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void UsingStateFileTest() {
            // Use a HostName which cannot automatically be detected as an Azure IoT Hub.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.invalid.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860;UsingIoTHub=true;StateFile=file1";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("hub1.invalid.net", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal("SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860", mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Equal(8883, mqttClientConnectionStringBuilder.Port);
            Assert.True(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.True(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.True(mqttClientConnectionStringBuilder.UsingStateFile);
            Assert.Equal("file1", mqttClientConnectionStringBuilder.StateFile);
            Assert.Equal(MqttProtocolVersion.V311, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void UsingMqttV500Test1() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=127.0.0.1;DeviceId=device1;ModuleId=module1;Username=username1;Password=password1;Protocol=v500";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal("127.0.0.1", mqttClientConnectionStringBuilder.HostName);
            Assert.Equal("device1", mqttClientConnectionStringBuilder.DeviceId);
            Assert.Equal("module1", mqttClientConnectionStringBuilder.ModuleId);
            Assert.Equal("username1", mqttClientConnectionStringBuilder.Username);
            Assert.Equal("password1", mqttClientConnectionStringBuilder.Password);
            Assert.Equal(1883, mqttClientConnectionStringBuilder.Port);
            Assert.False(mqttClientConnectionStringBuilder.UsingIoTHub);
            Assert.False(mqttClientConnectionStringBuilder.UsingX509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(MqttProtocolVersion.V500, mqttClientConnectionStringBuilder.Protocol);
        }

        [Fact]
        public void MissingSharedAccessSignatureTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;UsingIoTHub=true;Username=username1;Password=password1";
            Assert.Throws<KeyNotFoundException>(() => MqttClientConnectionStringBuilder.Create(mqttClientConnectionString));
        }

        [Fact]
        public void InvalidSharedAccessSignatureTest() {
            // Invalid resource URI.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString1 = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860";
            Assert.Throws<ArgumentException>(() => MqttClientConnectionStringBuilder.Create(mqttClientConnectionString1));

            // Invalid signature.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString2 = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=&se=1943452860";
            Assert.Throws<ArgumentException>(() => MqttClientConnectionStringBuilder.Create(mqttClientConnectionString2));

            // Invalid expiry.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString3 = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=invalid";
            Assert.Throws<ArgumentException>(() => MqttClientConnectionStringBuilder.Create(mqttClientConnectionString3));
        }
    }
}

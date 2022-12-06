// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.Tests {
    using Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient;
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

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "127.0.0.1");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            Assert.Equal(mqttClientConnectionStringBuilder.Username, "username1");
            Assert.Equal(mqttClientConnectionStringBuilder.Password, "password1");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 1883);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, false);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, false);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.MessageExpiryInterval);
        }

        [Fact]
        public void ValidIoTHubConnectionStringTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "hub1.azure-devices.net");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal(mqttClientConnectionStringBuilder.SharedAccessSignature, "SharedAccessSignature sr=hub1.azure-devices.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 8883);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, true);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, true);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.MessageExpiryInterval);
        }

        [Fact]
        public void OverrideUsingIoTHubTest() {
            // Use a HostName which cannot automatically be detected as an Azure IoT Hub.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.invalid.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860;UsingIoTHub=true";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "hub1.invalid.net");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal(mqttClientConnectionStringBuilder.SharedAccessSignature, "SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 8883);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, true);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, true);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.MessageExpiryInterval);
        }

        [Fact]
        public void OverridePortTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=127.0.0.1;DeviceId=device1;ModuleId=module1;Username=username1;Password=password1;Port=1234";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "127.0.0.1");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            Assert.Equal(mqttClientConnectionStringBuilder.Username, "username1");
            Assert.Equal(mqttClientConnectionStringBuilder.Password, "password1");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 1234);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, false);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, false);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Null(mqttClientConnectionStringBuilder.MessageExpiryInterval);
        }

        [Fact]
        public void UsingStateFileTest() {
            // Use a HostName which cannot automatically be detected as an Azure IoT Hub.
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=hub1.invalid.net;DeviceId=device1;ModuleId=module1;SharedAccessSignature=SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860;UsingIoTHub=true;StateFile=file1";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "hub1.invalid.net");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            Assert.Equal(mqttClientConnectionStringBuilder.SharedAccessSignature, "SharedAccessSignature sr=hub1.invalid.net%2Fdevices%2Fdevice1&sig=SAHEh7J7dPzpIhotIEpRXUhC4v49vKJOHLiKlcGv1U8%3D&se=1943452860");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 8883);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, true);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, true);
            Assert.NotNull(mqttClientConnectionStringBuilder.X509Cert);
            Assert.True(mqttClientConnectionStringBuilder.UsingStateFile);
            Assert.Equal(mqttClientConnectionStringBuilder.StateFile, "file1");
            Assert.Null(mqttClientConnectionStringBuilder.MessageExpiryInterval);
        }

        [Fact]
        public void MessageExpiryIntervalTest() {
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Test Example, no real secret")]
            const string mqttClientConnectionString = "HostName=127.0.0.1;DeviceId=device1;ModuleId=module1;Username=username1;Password=password1;MessageExpiryInterval=1234";
            var mqttClientConnectionStringBuilder = MqttClientConnectionStringBuilder.Create(mqttClientConnectionString);

            Assert.Equal(mqttClientConnectionStringBuilder.HostName, "127.0.0.1");
            Assert.Equal(mqttClientConnectionStringBuilder.DeviceId, "device1");
            Assert.Equal(mqttClientConnectionStringBuilder.ModuleId, "module1");
            Assert.Equal(mqttClientConnectionStringBuilder.Username, "username1");
            Assert.Equal(mqttClientConnectionStringBuilder.Password, "password1");
            Assert.Equal(mqttClientConnectionStringBuilder.Port, 1883);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingIoTHub, false);
            Assert.Equal(mqttClientConnectionStringBuilder.UsingX509Cert, false);
            Assert.Null(mqttClientConnectionStringBuilder.SharedAccessSignature);
            Assert.Null(mqttClientConnectionStringBuilder.X509Cert);
            Assert.Equal(mqttClientConnectionStringBuilder.MessageExpiryInterval, 1234u);
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

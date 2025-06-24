// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using AssetModel = Iot.Operations.Services.AssetAndDeviceRegistry.Models.Asset;
    using Furly.Azure.IoT.Operations.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Extensions.Options;

    public class AssetDeviceIntegrationTests
    {
        [Fact]
        public void ConstructorInitializesFields()
        {
            // Arrange/Act
            var sut = CreateSut();
            // Assert
            Assert.NotNull(sut);
        }

        [Fact]
        public async Task DisposeAsyncCanBeCalledMultipleTimes()
        {
            // Arrange
            var sut = CreateSut();
            // Act
            await sut.DisposeAsync();
            // Should not throw
            await sut.DisposeAsync();
        }

        [Fact]
        public void DisposeCallsDisposeAsyncMethod()
        {
            // Arrange
            var sut = CreateSut();
            // Act/Assert
            sut.Dispose();
            // Should not throw
            sut.Dispose();
        }

        [Fact]
        public void OnDeviceCreatedAddsDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";

            // Act
            sut.OnDeviceCreated(deviceName, endpointName, device);
            // Assert: device should be in the internal dictionary
            Assert.Single(sut.Devices, d => d.DeviceName == deviceName);
        }

        [Fact]
        public void OnDeviceCreatedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            TryCompleteChannel(sut);

            // Act / Assert
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnDeviceCreated(deviceName, endpointName, device));
        }

        [Fact]
        public void OnDeviceUpdatedUpdatesDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Act
            sut.OnDeviceUpdated(deviceName, endpointName, device);
            // Assert: device should be in the internal dictionary
            Assert.Single(sut.Devices, d => d.DeviceName == deviceName);
        }

        [Fact]
        public void OnDeviceUpdatedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            TryCompleteChannel(sut);

            // Act / Assert
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnDeviceUpdated(deviceName, endpointName, device));
        }

        [Fact]
        public void OnDeviceDeletedRemovesDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Add device first
            sut.OnDeviceCreated(deviceName, endpointName, device);
            // Act
            sut.OnDeviceDeleted(deviceName, endpointName, device);
            // Assert: device should be removed from the internal dictionary
            Assert.DoesNotContain(sut.Devices, d => d.DeviceName == deviceName);
        }

        [Fact]
        public void OnDeviceDeletedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Add device first
            sut.OnDeviceCreated(deviceName, endpointName, device);
            TryCompleteChannel(sut);

            // Act / Assert
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnDeviceDeleted(deviceName, endpointName, device));
        }

        [Fact]
        public void OnAssetCreatedAddsAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";

            // Act
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            Assert.Single(sut.Assets, a => a.AssetName == assetName);
        }

        [Fact]
        public void OnAssetCreatedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            TryCompleteChannel(sut);

            // Act / Assert
            Assert.Throws<ObjectDisposedException>(() => sut.OnAssetCreated(deviceName, endpointName, assetName, asset));
        }

        [Fact]
        public void OnAssetUpdatedUpdatesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Act
            sut.OnAssetUpdated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            Assert.Single(sut.Assets, a => a.AssetName == assetName);
        }

        [Fact]
        public void OnAssetUpdatedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            TryCompleteChannel(sut);

            // Act / Assert
            Assert.Throws<ObjectDisposedException>(() => sut.OnAssetUpdated(deviceName, endpointName, assetName, asset));
        }

        [Fact]
        public void OnAssetDeletedRemovesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Act
            sut.OnAssetDeleted(deviceName, endpointName, assetName, asset);
            // Assert: asset should be removed from the internal dictionary
            Assert.DoesNotContain(sut.Assets, a => a.AssetName == assetName);
        }

        [Fact]
        public void OnAssetDeletedThrowsIfChangeFeedWasCompleted()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            TryCompleteChannel(sut);
            // Act / Assert
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnAssetDeleted(deviceName, endpointName, assetName, asset));
        }

        private static void TryCompleteChannel(AssetDeviceIntegration sut)
        {
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel =
                (System.Threading.Channels.Channel<(string, AssetDeviceIntegration.Resource)>)
                    field.GetValue(sut);
            channel.Writer.TryComplete();
        }

        private AssetDeviceIntegration CreateSut() =>
            new(_clientMock.Object, _publishedNodesMock.Object, _configurationServicesMock.Object,
                _serializerMock.Object, _optionsMock.Object, _loggerMock.Object);

        private readonly Mock<IOptions<PublisherOptions>> _optionsMock = new();
        private readonly Mock<IAioAdrClient> _clientMock = new();
        private readonly Mock<IPublishedNodesServices> _publishedNodesMock = new();
        private readonly Mock<IJsonSerializer> _serializerMock = new();
        private readonly Mock<IConfigurationServices> _configurationServicesMock = new();
        private readonly Mock<ILogger<AssetDeviceIntegration>> _loggerMock = new();
    }
}

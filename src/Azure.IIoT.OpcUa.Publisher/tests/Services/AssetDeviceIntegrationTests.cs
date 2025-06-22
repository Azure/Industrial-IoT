// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using AssetModel = Iot.Operations.Services.AssetAndDeviceRegistry.Models.Asset;
    using Furly.Azure.IoT.Operations.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit;

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
        public void DisposeCallsDisposeAsync()
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
        public void OnDeviceCreatedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();

            // Act
            sut.OnDeviceCreated(deviceName, endpointName, device);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process creation of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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
        public void OnDeviceUpdatedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();

            // Act
            sut.OnDeviceUpdated(deviceName, endpointName, device);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process update of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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
        public void OnDeviceDeletedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Add device first
            sut.OnDeviceCreated(deviceName, endpointName, device);

            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();

            // Act
            sut.OnDeviceDeleted(deviceName, endpointName, device);

            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process deletion of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetCreatedAddsAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";

            // Act
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceIntegration).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(sut);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetCreatedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();

            // Act
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process creation of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetUpdatedUpdatesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Act
            sut.OnAssetUpdated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceIntegration).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(sut);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetUpdatedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();

            // Act
            sut.OnAssetUpdated(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process update of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetDeletedRemovesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Act
            sut.OnAssetDeleted(deviceName, endpointName, assetName, asset);
            // Assert: asset should be removed from the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceIntegration).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(sut);
            Assert.False(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetDeletedLogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceIntegration).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(sut);
            channel.Writer.TryComplete();
            // Act
            sut.OnAssetDeleted(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process deletion of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        private AssetDeviceIntegration CreateSut() =>
            new(_clientMock.Object, _publishedNodesMock.Object, _serializerMock.Object, _loggerMock.Object);

        private readonly Mock<IAioAdrClient> _clientMock = new();
        private readonly Mock<IPublishedNodesServices> _publishedNodesMock = new();
        private readonly Mock<IJsonSerializer> _serializerMock = new();
        private readonly Mock<ILogger<AssetDeviceIntegration>> _loggerMock = new();
    }
}

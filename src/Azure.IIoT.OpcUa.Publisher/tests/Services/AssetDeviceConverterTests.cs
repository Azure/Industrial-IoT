// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Furly.Azure.IoT.Operations.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit;
    using AssetModel = Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models.Asset;

    public class AssetDeviceConverterTests
    {
        [Fact]
        public void ConstructorInitializesFields()
        {
            // Arrange/Act
            var converter = CreateConverter();
            // Assert
            Assert.NotNull(converter);
        }

        [Fact]
        public async Task DisposeAsyncCanBeCalledMultipleTimes()
        {
            // Arrange
            var converter = CreateConverter();
            // Act
            await converter.DisposeAsync();
            // Should not throw
            await converter.DisposeAsync();
        }

        [Fact]
        public void DisposeCallsDisposeAsync()
        {
            // Arrange
            var converter = CreateConverter();
            // Act/Assert
            converter.Dispose();
            // Should not throw
            converter.Dispose();
        }

        [Fact]
        public void OnDeviceCreated_AddsDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";

            // Act
            converter.OnDeviceCreated(deviceName, endpointName, device);
            // Assert: device should be in the internal dictionary
            var key = deviceName + "_" + endpointName;
            var field = typeof(AssetDeviceConverter).GetField("_devices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnDeviceCreated_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();

            // Act
            converter.OnDeviceCreated(deviceName, endpointName, device);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process creation of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnDeviceUpdated_UpdatesDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Act
            converter.OnDeviceUpdated(deviceName, endpointName, device);
            // Assert: device should be in the internal dictionary
            var key = deviceName + "_" + endpointName;
            var field = typeof(AssetDeviceConverter).GetField("_devices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnDeviceUpdated_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();

            // Act
            converter.OnDeviceUpdated(deviceName, endpointName, device);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process update of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnDeviceDeleted_RemovesDeviceAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Add device first
            converter.OnDeviceCreated(deviceName, endpointName, device);
            // Act
            converter.OnDeviceDeleted(deviceName, endpointName, device);
            // Assert: device should be removed from the internal dictionary
            var key = deviceName + "_" + endpointName;
            var field = typeof(AssetDeviceConverter).GetField("_devices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.False(dict.ContainsKey(key));
        }

        [Fact]
        public void OnDeviceDeleted_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var device = new Device();
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            // Add device first
            converter.OnDeviceCreated(deviceName, endpointName, device);
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = (System.Threading.Channels.Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();
            // Act
            converter.OnDeviceDeleted(deviceName, endpointName, device);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process deletion of device")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetCreated_AddsAssetAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";

            // Act
            converter.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceConverter).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetCreated_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();

            // Act
            converter.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process creation of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetUpdated_UpdatesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Act
            converter.OnAssetUpdated(deviceName, endpointName, assetName, asset);
            // Assert: asset should be in the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceConverter).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.True(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetUpdated_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();

            // Act
            converter.OnAssetUpdated(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process update of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void OnAssetDeleted_RemovesAssetAndWritesToChangeFeed()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            converter.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Act
            converter.OnAssetDeleted(deviceName, endpointName, assetName, asset);
            // Assert: asset should be removed from the internal dictionary
            var key = deviceName + "_" + endpointName + "_" + assetName;
            var field = typeof(AssetDeviceConverter).GetField("_assets", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (ConcurrentDictionary<string, object>)field.GetValue(converter);
            Assert.False(dict.ContainsKey(key));
        }

        [Fact]
        public void OnAssetDeleted_LogsErrorIfChangeFeedWriteFails()
        {
            // Arrange
            var converter = CreateConverter();
            var asset = new AssetModel { DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" } };
            const string deviceName = "dev1";
            const string endpointName = "ep1";
            const string assetName = "asset1";
            // Add asset first
            converter.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Simulate full channel by completing writer
            var field = typeof(AssetDeviceConverter).GetField("_changeFeed", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = (Channel<(string, object)>)field.GetValue(converter);
            channel.Writer.TryComplete();
            // Act
            converter.OnAssetDeleted(deviceName, endpointName, assetName, asset);
            // Assert: error log should be called
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to process deletion of asset")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        private AssetDeviceConverter CreateConverter() =>
            new(_clientMock.Object, _publishedNodesMock.Object, _serializerMock.Object, _loggerMock.Object);

        private readonly Mock<IAioAdrClient> _clientMock = new();
        private readonly Mock<IPublishedNodesServices> _publishedNodesMock = new();
        private readonly Mock<IJsonSerializer> _serializerMock = new();
        private readonly Mock<ILogger<AssetDeviceConverter>> _loggerMock = new();
    }
}

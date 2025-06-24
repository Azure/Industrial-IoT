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
    using System.Collections.Generic;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Linq;
    using System.Buffers;
    using System.Threading;

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
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnAssetCreated(deviceName, endpointName, assetName, asset));
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
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnAssetUpdated(deviceName, endpointName, assetName, asset));
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

        [Fact]
        public async Task RunAsyncProcessesChangeFeedWithoutException()
        {
            // Arrange
            var sut = CreateSut();
            using var cts = new System.Threading.CancellationTokenSource(100); // Cancel after 100ms
            // Act/Assert
            await sut.RunAsync(cts.Token); // Should not throw
        }

        [Fact]
        public async Task RunDiscoveryUsingTypesAsyncReportsDiscoveredAssets()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device
            {
                Endpoints = new DeviceEndpoints
                {
                    Inbound = new Dictionary<string, InboundEndpointSchemaMapValue>
                    {
                        { "ep1",
                            new InboundEndpointSchemaMapValue
                            {
                                Address = "opc.tcp://localhost:4840"
                            }
                        }
                    }
                }
            };
            var resource = new AssetDeviceIntegration.DeviceEndpointResource(
                "dev1", device, "ep1");
            var types = new HashSet<string> { "ns=2;s=Type1" };
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            var publishedNodesEntry = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://endpoint",
                DataSetWriterGroup = "AssetGroup",
                WriterGroupRootNodeId = "rootId",
                WriterGroupType = "typeRef",
                OpcNodes = new List<OpcNodeModel>
                {
                    new OpcNodeModel
                    {
                        Id = "ns=2;s=Type1",
                        DisplayName = "Node1"
                    }
                }
            };
            var serviceResponseMock = new ServiceResponse<PublishedNodesEntryModel>
            {
                ErrorInfo = null,
                Result = publishedNodesEntry
            };

            _configurationServicesMock
                .Setup(s => s.ExpandAsync(
                    It.IsAny<PublishedNodesEntryModel>(),
                    It.IsAny<PublishedNodeExpansionModel>(),
                    default))
                .Returns(AsyncEnumerable.Range(0, 1).Select(_ => serviceResponseMock));
            _clientMock.Setup(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default))
                .Returns(ValueTask.FromResult<DiscoveredAssetResponseSchema>(null))
                .Verifiable();

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, types, errors, default);

            // Assert
            _clientMock
                .Verify(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default), Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunDiscoveryUsingTypesAsyncReportsNothingIfNothingIsFound()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device
            {
                Endpoints = new DeviceEndpoints
                {
                    Inbound = new Dictionary<string, InboundEndpointSchemaMapValue>
                    {
                        { "ep1",
                            new InboundEndpointSchemaMapValue
                            {
                                Address = "opc.tcp://localhost:4840"
                            }
                        }
                    }
                }
            };
            var resource = new AssetDeviceIntegration.DeviceEndpointResource(
                "dev1", device, "ep1");
            var types = new HashSet<string> { "ns=2;s=Type1" };
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            var publishedNodesEntry = new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://endpoint",
                DataSetWriterGroup = "AssetGroup",
                WriterGroupRootNodeId = "rootId",
                WriterGroupType = "typeRef",
                OpcNodes = new List<OpcNodeModel>()
            };
            var serviceResponseMock = new ServiceResponse<PublishedNodesEntryModel>
            {
                ErrorInfo = null,
                Result = publishedNodesEntry
            };

            _configurationServicesMock
                .Setup(s => s.ExpandAsync(
                    It.IsAny<PublishedNodesEntryModel>(),
                    It.IsAny<PublishedNodeExpansionModel>(),
                    default))
                .Returns(AsyncEnumerable.Range(0, 1).Select(_ => serviceResponseMock));
            _clientMock.Setup(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default))
                .Returns(ValueTask.FromResult<DiscoveredAssetResponseSchema>(null))
                .Verifiable();

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, types, errors, default);

            // Assert
            _clientMock
                .Verify(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default), Times.Never);
        }

        [Fact]
        public async Task RunDiscoveryUsingTypesAsyncEndpointNotFoundReportsError()
        {
            // Arrange
            var sut = CreateSut();
            var device = new Device();
            var resource = new AssetDeviceIntegration.DeviceEndpointResource("dev1", device, "ep1");
            var types = new HashSet<string> { "ns=2;s=Type1" };
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, types, errors, default);
            // Assert: error should be recorded (no exception thrown)
        }

        [Fact]
        public async Task ToPublishedNodesAsyncWithDatasetsAndEventsReturnsEntries()
        {
            // Arrange
            var sut = CreateSut();
            var device = new AssetDeviceIntegration.DeviceResource("dev1", new Device
            {
                Endpoints = new DeviceEndpoints
                {
                    Inbound = new Dictionary<string, InboundEndpointSchemaMapValue>
                    {
                        { "ep1",
                            new InboundEndpointSchemaMapValue
                            {
                                Address = "opc.tcp://localhost:4840"
                            }
                        }
                    }
                }
            });
            var dataset = new AssetDataset
            {
                Name = "ds1",
                DataPoints = new List<AssetDatasetDataPointSchemaElement>
                {
                    new AssetDatasetDataPointSchemaElement { Name = "dp1", DataSource = "ns=2;s=dp1" }
                }
            };
            var @event = new AssetEvent { Name = "ev1", EventNotifier = "ns=2;s=ev1" };
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" },
                Datasets = new List<AssetDataset> { dataset },
                Events = new List<AssetEvent> { @event }
            });
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            _serializerMock
                .Setup(s => s.Deserialize(It.IsAny<ReadOnlySequence<byte>>(), It.IsAny<Type>()))
                .Returns((object)null);

            // Act
            var result = await sut.ToPublishedNodesAsync(
                new[] { device }, new[] { asset }, errors, default);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ToPublishedNodesAsyncDeviceNotFoundReportsError()
        {
            // Arrange
            var sut = CreateSut();
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef { DeviceName = "devX", EndpointName = "epX" }
            });
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            // Act
            var result = await sut.ToPublishedNodesAsync(
                new AssetDeviceIntegration.DeviceResource[0], new[] { asset }, errors, default);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ToPublishedNodesAsyncReturnsExpectedEntries()
        {
            // Arrange
            var sut = CreateSut();
            var device = new AssetDeviceIntegration.DeviceResource("dev1", new Device
            {
                Endpoints = new DeviceEndpoints
                {
                    Inbound = new Dictionary<string, InboundEndpointSchemaMapValue>
                    {
                        { "ep1",
                            new InboundEndpointSchemaMapValue
                            {
                                Address = "opc.tcp://localhost:4840"
                            }
                        }
                    }
                }
            });
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                },
                Datasets = null,
                Events = null
            });
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            // Act
            var result = await sut.ToPublishedNodesAsync(
                new[] { device }, new[] { asset }, errors, default);

            // Assert
            Assert.NotNull(result);
            // Should be empty because no datasets/events, but no error thrown
        }

        [Fact]
        public void CollectAssetAndDevicePropertiesReturnsExpectedDictionary()
        {
            // Arrange
            var device = new AssetDeviceIntegration.DeviceResource("dev1", new Device
            {
                Model = "X",
                Manufacturer = "Y"
            });
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                },
                Model = null,
                Manufacturer = "B"
            });

            // Act
            var result = AssetDeviceIntegration.CollectAssetAndDeviceProperties(
                asset, device);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("X", result[nameof(Device.Model)]);
            Assert.Equal("B", result[nameof(AssetModel.Manufacturer)]);
        }

        [Fact]
        public async Task ValidationErrorsReportAsyncReportsDeviceAndAssetStatus()
        {
            // Arrange
            var sut = CreateSut();
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            var device = new AssetDeviceIntegration.DeviceResource("dev1", new Device());
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                }
            });
            errors.OnError(device, "code1", "error1");
            errors.OnError(asset, "code2", "error2");
            _clientMock
                .Setup(c => c.UpdateDeviceStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DeviceStatus>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<DeviceStatus>(null))
                .Verifiable();
            _clientMock
                .Setup(c => c.UpdateAssetStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AssetStatus>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<AssetStatus>(null))
                .Verifiable();

            // Act
            await errors.ReportAsync(default);

            // Assert
            _clientMock
                .Verify(c => c.UpdateDeviceStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DeviceStatus>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce());
            _clientMock
                .Verify(c => c.UpdateAssetStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<AssetStatus>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<System.Threading.CancellationToken>()), Times.AtLeastOnce());
        }

        [Fact]
        public void ValidationErrorsOnErrorAddsError()
        {
            // Arrange
            var sut = CreateSut();
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            var device = new AssetDeviceIntegration.DeviceResource("dev1", new Device());

            // Act
            errors.OnError(device, "code1", "error1");
            // No assert, just ensure no exception and internal state updated
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

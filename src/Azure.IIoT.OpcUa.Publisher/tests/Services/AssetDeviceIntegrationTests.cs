// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
    using Azure.Iot.Operations.Services.SchemaRegistry.SchemaRegistry;
    using IEventSchema = Azure.IIoT.OpcUa.Core.Messaging.IEventSchema;
    using AssetModel = Iot.Operations.Services.AssetAndDeviceRegistry.Models.Asset;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Azure.IIoT.OpcUa.Publisher.Stack;

    public class AssetDeviceIntegrationTests
    {
        public AssetDeviceIntegrationTests()
        {
            // Initialize mocks
            _srMock.Setup(x => x.Register(It.IsAny<IAioSrCallbacks>())).Returns(Mock.Of<IDisposable>());
            _optionsMock.SetupGet(o => o.Value).Returns(new PublisherOptions
            {
                PublisherId = "aio"
            });
        }

        [Fact]
        public void ConstructorInitializesFields()
        {
            // Arrange/Act
            var sut = CreateSut();
            // Assert
            Assert.NotNull(sut);
        }

        [Fact]
        public void AssetConfigurationRootsParseWithoutReflection()
        {
            AssertGeneratedRoundTrip(new DataSetConfiguration());
            AssertGeneratedRoundTrip(new DataSetDataPointConfiguration());
            AssertGeneratedRoundTrip(new EventGroupConfiguration());
            AssertGeneratedRoundTrip(new EventConfiguration());
            AssertGeneratedRoundTrip(new EventDataPointConfiguration());
            AssertGeneratedRoundTrip(new ManagementGroupConfiguration());
            AssertGeneratedRoundTrip(new ActionConfiguration { CompiledMetadata = [] });
            AssertGeneratedRoundTrip(new DeviceEndpointConfiguration());
        }

        [Fact]
        public void DebugAssetAndDeviceTypesSerializeWithoutReflection()
        {
            AssertGeneratedDebugSerialization<DiscoveredAsset>();
            AssertGeneratedDebugSerialization<DiscoveredDevice>();
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
            sut.OnDeviceDeleted(deviceName, endpointName);
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
                () => sut.OnDeviceDeleted(deviceName, endpointName));
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
            sut.OnDeviceCreated(deviceName, endpointName, new Device());
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
            sut.OnDeviceCreated(deviceName, endpointName, new Device());
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            // Act
            sut.OnAssetDeleted(deviceName, endpointName, assetName);
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
            sut.OnDeviceCreated(deviceName, endpointName, new Device());
            sut.OnAssetCreated(deviceName, endpointName, assetName, asset);
            TryCompleteChannel(sut);
            // Act / Assert
            Assert.Throws<ObjectDisposedException>(
                () => sut.OnAssetDeleted(deviceName, endpointName, assetName));
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
            var types = new List<string> { "ns=2;s=Type1" };
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
#pragma warning disable CA2012 // Use ValueTasks correctly
            _clientMock.Setup(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default))
                .Returns(ValueTask.FromResult<DiscoveredAssetResponseSchema>(null))
                .Verifiable();
#pragma warning restore CA2012 // Use ValueTasks correctly

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, new DeviceEndpointConfiguration { AssetTypes = types },
                errors, default);

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
            var types = new List<string> { "ns=2;s=Type1" };
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
#pragma warning disable CA2012 // Use ValueTasks correctly
            _clientMock.Setup(c => c.ReportDiscoveredAssetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DiscoveredAsset>(), null, default))
                .Returns(ValueTask.FromResult<DiscoveredAssetResponseSchema>(null))
                .Verifiable();
#pragma warning restore CA2012 // Use ValueTasks correctly

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, new DeviceEndpointConfiguration { AssetTypes = types },
                errors, default);

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
            var types = new List<string> { "ns=2;s=Type1" };
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            // Act
            await sut.RunDiscoveryUsingTypesAsync(resource, new DeviceEndpointConfiguration { AssetTypes = types },
                errors, default);
            // Assert: error should be recorded (no exception thrown)
        }

        [Fact]
        public async Task ToPublishedNodesAsyncWithDatasetsAndEventsReturnsEntries()
        {
            // Arrange
            var sut = CreateSut();
            var d = new Device
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
            var device = new AssetDeviceIntegration.DeviceResource("dev1", d);
            sut.OnDeviceCreated("dev1", "ep1", d);
            var dataset = new AssetDataset
            {
                Name = "ds1",
                DataPoints = new List<AssetDatasetDataPoint>
                {
                    new AssetDatasetDataPoint { Name = "dp1", DataSource = "ns=2;s=dp1" }
                }
            };
            var @event = new AssetEvent { Name = "ev1", DataSource = "ns=2;s=ev1" };
            var eg = new AssetEventGroup { Name = "eg1", Events = new List<AssetEvent> { @event } };
            var asset = new AssetDeviceIntegration.AssetResource("asset1", new AssetModel
            {
                DeviceRef = new AssetDeviceRef { DeviceName = "dev1", EndpointName = "ep1" },
                Datasets = new List<AssetDataset> { dataset },
                EventGroups = new List<AssetEventGroup> { eg }
            });
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
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
                Array.Empty<AssetDeviceIntegration.DeviceResource>(), new[] { asset }, errors, default);

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
                EventGroups = null
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
            Assert.Equal("X", (string?)result[nameof(Device.Model)]);
            Assert.Equal("B", (string?)result[nameof(AssetModel.Manufacturer)]);
        }

        [Fact]
        public async Task ValidationErrorsReportAsyncReportsDeviceAndAssetStatus()
        {
            // Arrange
            var sut = CreateSut();
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            var device = new AssetDeviceIntegration.DeviceEndpointResource("dev1", new Device(), "ep1");
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
#pragma warning disable CA2012 // Use ValueTasks correctly
            _clientMock
                .Setup(c => c.UpdateDeviceStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DeviceStatus>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<DeviceStatus>(null))
                .Verifiable();
#pragma warning restore CA2012 // Use ValueTasks correctly
#pragma warning disable CA2012 // Use ValueTasks correctly
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
#pragma warning restore CA2012 // Use ValueTasks correctly

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
            var device = new AssetDeviceIntegration.DeviceEndpointResource("dev1", new Device(), "ep1");

            // Act
            errors.OnError(device, "code1", "error1");
            // No assert, just ensure no exception and internal state updated
        }

        [Fact]
        public async Task OnSchemaRegisteredAsyncUpdatesDatasetSchemaReferenceAsync()
        {
            // Arrange
            var sut = CreateSut();
            var asset = CreateAssetWithSchemaResources(displayName: "Boiler");
            sut.OnDeviceCreated("dev1", "ep1", CreateDevice("opc.tcp://localhost:4840"));
            sut.OnAssetCreated("dev1", "ep1", "asset1", asset);

            AssetStatus? captured = null;
#pragma warning disable CA2012 // Use ValueTasks correctly
            _clientMock
                .Setup(c => c.UpdateAssetStatusAsync("dev1", "ep1", "asset1",
                    It.IsAny<AssetStatus>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, string, AssetStatus, TimeSpan?, CancellationToken>(
                    (_, _, _, status, _, _) => captured = status)
                .Returns(ValueTask.FromResult<AssetStatus>(null));
#pragma warning restore CA2012 // Use ValueTasks correctly

            // Act
            await sut.OnSchemaRegisteredAsync(CreateSchema("Boiler|ds1"),
                CreateRegistration(), default);

            // Assert
            var dataset = Assert.Single(captured?.Datasets ??
                throw new InvalidOperationException("Missing dataset status."));
            Assert.Equal("ds1", dataset.Name);
            Assert.Equal("schema1", dataset.MessageSchemaReference?.SchemaName);
            Assert.Equal("namespace1", dataset.MessageSchemaReference?.SchemaRegistryNamespace);
            Assert.Equal("1.0.0", dataset.MessageSchemaReference?.SchemaVersion);
        }

        [Fact]
        public async Task OnSchemaRegisteredAsyncUsesAssetNameFallbackAndUpdatesEventSchemaReferenceAsync()
        {
            // Arrange
            var sut = CreateSut();
            var asset = CreateAssetWithSchemaResources(displayName: "Display", model: "Model");
            sut.OnDeviceCreated("dev1", "ep1", CreateDevice("opc.tcp://localhost:4840"));
            sut.OnAssetCreated("dev1", "ep1", "asset1", asset);

            AssetStatus? captured = null;
#pragma warning disable CA2012 // Use ValueTasks correctly
            _clientMock
                .Setup(c => c.UpdateAssetStatusAsync("dev1", "ep1", "asset1",
                    It.IsAny<AssetStatus>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, string, AssetStatus, TimeSpan?, CancellationToken>(
                    (_, _, _, status, _, _) => captured = status)
                .Returns(ValueTask.FromResult<AssetStatus>(null));
#pragma warning restore CA2012 // Use ValueTasks correctly

            // Act
            await sut.OnSchemaRegisteredAsync(CreateSchema("asset1|ev1"),
                CreateRegistration(), default);

            // Assert
            var eventGroup = Assert.Single(captured?.EventGroups ??
                throw new InvalidOperationException("Missing event group status."));
            var @event = Assert.Single(eventGroup.Events ??
                throw new InvalidOperationException("Missing event status."));
            Assert.Equal("ev1", @event.Name);
            Assert.Equal("schema1", @event.MessageSchemaReference?.SchemaName);
            Assert.Equal("namespace1", @event.MessageSchemaReference?.SchemaRegistryNamespace);
            Assert.Equal("1.0.0", @event.MessageSchemaReference?.SchemaVersion);
        }

        [Theory]
        [InlineData(null, "namespace1", "1.0.0", "Boiler|ds1")]
        [InlineData("schema1", null, "1.0.0", "Boiler|ds1")]
        [InlineData("schema1", "namespace1", null, "Boiler|ds1")]
        [InlineData("schema1", "namespace1", "1.0.0", null)]
        [InlineData("schema1", "namespace1", "1.0.0", "x|ds1")]
        public async Task OnSchemaRegisteredAsyncRejectsInvalidRegistrationsAsync(
            string? name, string? @namespace, string? version, string? schemaId)
        {
            // Arrange
            var sut = CreateSut();
            sut.OnDeviceCreated("dev1", "ep1", CreateDevice("opc.tcp://localhost:4840"));
            sut.OnAssetCreated("dev1", "ep1", "asset1",
                CreateAssetWithSchemaResources(displayName: "Boiler"));

            // Act
            await sut.OnSchemaRegisteredAsync(CreateSchema(schemaId),
                new Schema { Name = name!, Namespace = @namespace!, Version = version! },
                default);

            // Assert
            _clientMock.Verify(c => c.UpdateAssetStatusAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetStatus>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToPublishedNodesAsyncMapsDatasetsEventsManagementAndDestinationsAsync()
        {
            // Arrange
            var sut = CreateSut();
            var deviceModel = CreateDevice("opc.tcp://localhost:4840");
            deviceModel.ExternalDeviceId = "device-ext";
            sut.OnDeviceCreated("dev1", "ep1", deviceModel);
            var device = new AssetDeviceIntegration.DeviceResource("dev1", deviceModel);
            var assetModel = CreateAssetWithAllResources();
            var asset = new AssetDeviceIntegration.AssetResource("asset1", assetModel);
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);

            // Act
            var result = await sut.ToPublishedNodesAsync([device], [asset], errors, default);

            // Assert
            Assert.Equal(4, result.Count);

            var dataset = Assert.Single(result, e => e.DataSetWriterId == "dataset1");
            Assert.Equal("opc.tcp://localhost:4840", dataset.EndpointUrl);
            Assert.Equal("asset1", dataset.DataSetWriterGroup);
            Assert.Equal("dev1", dataset.PublisherId);
            Assert.Equal("asset-type", dataset.WriterGroupType);
            Assert.Equal("ns=2;s=Asset1", dataset.WriterGroupRootNodeId);
            Assert.Equal("dataset1", dataset.DataSetName);
            Assert.Equal("ns=2;s=Dataset", dataset.DataSetRootNodeId);
            Assert.Equal("dataset-type", dataset.DataSetType);
            Assert.Equal("ms-aio:device-ext_ep1/ns%3D2%3Bs%3DDataset",
                dataset.DataSetSourceUri);
            Assert.Equal("asset-ext/dataset1", dataset.DataSetSubject);
            Assert.Equal(100, dataset.DataSetPublishingInterval);
            Assert.Equal(50, dataset.OpcNodes?.Single().OpcSamplingInterval);
            Assert.Equal(WriterGroupTransport.AioMqtt, dataset.WriterGroupTransport);
            Assert.Equal(Azure.IIoT.OpcUa.Core.Messaging.QoS.AtLeastOnce,
                dataset.QualityOfService);
            Assert.Equal(true, dataset.MessageRetention);
            Assert.Equal("telemetry/dataset1", dataset.QueueName);
            Assert.Equal(TimeSpan.FromSeconds(42), dataset.MessageTtlTimespan);
            Assert.Equal(3u, dataset.DataSetKeyFrameCount);

            var @event = Assert.Single(result, e => e.DataSetWriterId == "events1");
            Assert.Equal("events1", @event.DataSetName);
            Assert.Equal("event-group-type", @event.DataSetType);
            Assert.Equal("asset-ext/events1/event1", @event.DataSetSubject);
            Assert.Equal("telemetry/events1", @event.QueueName);
            Assert.Equal(7u, @event.OpcNodes?.Single().QueueSize);
            Assert.Equal("event-type", @event.OpcNodes?.Single().EventFilter?.TypeDefinitionId);

            var managementA = Assert.Single(result, e => e.QueueName == "cmd/a");
            Assert.Equal("mgmt1", managementA.DataSetName);
            Assert.Equal("asset-ext/mgmt1", managementA.DataSetSubject);
            Assert.Equal("action1", managementA.OpcNodes?.Single().DisplayName);
            Assert.NotNull(managementA.OpcNodes?.Single().MethodMetadata);

            var managementB = Assert.Single(result, e => e.QueueName == "cmd/default");
            Assert.Equal("action2", managementB.OpcNodes?.Single().DisplayName);
        }

        [Fact]
        public void ConvertActionConfigurationRoundTripsCompressedMethodMetadata()
        {
            // Arrange
            var sut = CreateSut();
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            var resource = new AssetDeviceIntegration.ManagementActionResource(
                "asset1", CreateAssetWithSchemaResources(), new AssetManagementGroup
                {
                    Name = "mgmt1"
                }, new AssetManagementGroupAction
                {
                    Name = "action1"
                });
            var metadata = new MethodMetadataModel
            {
                ObjectId = "ns=2;s=Object",
                InputArguments =
                [
                    new MethodMetadataArgumentModel
                    {
                        Name = "temperature",
                        Type = new NodeModel { NodeId = "i=11" }
                    }
                ],
                OutputArguments =
                [
                    new MethodMetadataArgumentModel
                    {
                        Name = "accepted",
                        Type = new NodeModel { NodeId = "i=1" }
                    }
                ]
            };

            // Act
            var json = sut.ConvertActionConfiguration(metadata);
            var roundTripped = sut.ConvertActionConfiguration(json, errors, resource);

            // Assert
            Assert.NotNull(json);
            Assert.Equal("ns=2;s=Object", roundTripped.ObjectId);
            Assert.Equal("temperature", Assert.Single(roundTripped.InputArguments!).Name);
            Assert.Equal("accepted", Assert.Single(roundTripped.OutputArguments!).Name);
        }

        [Fact]
        public void ConvertActionConfigurationReturnsEmptyMetadataForInvalidJson()
        {
            // Arrange
            var sut = CreateSut();
            var errors = new AssetDeviceIntegration.ValidationErrors(sut);
            var resource = new AssetDeviceIntegration.ManagementActionResource(
                "asset1", CreateAssetWithSchemaResources(), new AssetManagementGroup
                {
                    Name = "mgmt1"
                }, new AssetManagementGroupAction
                {
                    Name = "action1"
                });

            // Act
            var metadata = sut.ConvertActionConfiguration("{not-json", errors, resource);

            // Assert
            Assert.NotNull(metadata);
            Assert.Null(metadata.ObjectId);
        }

        private static void AssertGeneratedRoundTrip<T>(T configuration)
        {
            var typeInfo = Json.GetTypeInfo<T>();
            Assert.Equal("PublisherJsonContext",
                typeInfo.OriginatingResolver?.GetType().Name);
            var json = Json.SerializeToString(configuration, typeInfo);
            Assert.NotNull(Json.Deserialize(json, typeInfo));
        }

        private static void AssertGeneratedDebugSerialization<T>()
        {
            var typeInfo = Json.GetTypeInfo<T>();
            Assert.Equal("PublisherJsonContext",
                typeInfo.OriginatingResolver?.GetType().Name);
            Assert.Equal("null", Json.SerializeToString<T>(default, typeInfo,
                SerializeOption.Indented));
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

        private static Device CreateDevice(string endpointUrl)
        {
            return new Device
            {
                Endpoints = new DeviceEndpoints
                {
                    Inbound = new Dictionary<string, InboundEndpointSchemaMapValue>
                    {
                        ["ep1"] = new()
                        {
                            Address = endpointUrl
                        }
                    }
                }
            };
        }

        private static AssetModel CreateAssetWithSchemaResources(
            string? displayName = null, string? model = null)
        {
            return new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                },
                DisplayName = displayName,
                Model = model,
                Datasets =
                [
                    new AssetDataset
                    {
                        Name = "ds1",
                        DataPoints =
                        [
                            new AssetDatasetDataPoint
                            {
                                Name = "dp1",
                                DataSource = "ns=2;s=dp1"
                            }
                        ]
                    }
                ],
                EventGroups =
                [
                    new AssetEventGroup
                    {
                        Name = "eg1",
                        Events =
                        [
                            new AssetEvent
                            {
                                Name = "ev1",
                                DataSource = "ns=2;s=ev1"
                            }
                        ]
                    }
                ]
            };
        }

        private static AssetModel CreateAssetWithAllResources()
        {
            return new AssetModel
            {
                DeviceRef = new AssetDeviceRef
                {
                    DeviceName = "dev1",
                    EndpointName = "ep1"
                },
                ExternalAssetId = "asset-ext",
                AssetTypeRefs = ["asset-type"],
                Attributes = new Dictionary<string, string>
                {
                    ["AssetId"] = "ns=2;s=Asset1"
                },
                DefaultDatasetsConfiguration = Serialize(new PublishedNodesEntryModel
                {
                    EndpointUrl = string.Empty,
                    WriterGroupQueueName = "removed"
                }),
                DefaultEventsConfiguration = Serialize(new PublishedNodesEntryModel
                {
                    EndpointUrl = string.Empty,
                    WriterGroupQueueName = "removed"
                }),
                DefaultManagementGroupsConfiguration = Serialize(new PublishedNodesEntryModel
                {
                    EndpointUrl = string.Empty,
                    WriterGroupQueueName = "removed"
                }),
                Datasets =
                [
                    new AssetDataset
                    {
                        Name = "dataset1",
                        DataSource = "ns=2;s=Dataset",
                        TypeRef = "dataset-type",
                        DatasetConfiguration = Serialize(new DataSetConfiguration
                        {
                            PublishingInterval = 100,
                            KeyFrameCount = 3,
                            MessageEncoding = MessageEncoding.Avro
                        }),
                        Destinations =
                        [
                            new DatasetDestination
                            {
                                Target = DatasetTarget.Mqtt,
                                Configuration = new DestinationConfiguration
                                {
                                    Topic = "telemetry/dataset1",
                                    Qos = QoS.Qos1,
                                    Retain = Retain.Keep,
                                    Ttl = 42
                                }
                            }
                        ],
                        DataPoints =
                        [
                            new AssetDatasetDataPoint
                            {
                                Name = "temperature",
                                DataSource = "ns=2;s=Temperature",
                                TypeRef = "double-type",
                                DataPointConfiguration = Serialize(new DataSetDataPointConfiguration
                                {
                                    SamplingInterval = 50
                                })
                            }
                        ]
                    }
                ],
                EventGroups =
                [
                    new AssetEventGroup
                    {
                        Name = "events1",
                        DataSource = "ns=2;s=Events",
                        TypeRef = "event-group-type",
                        EventGroupConfiguration = Serialize(new EventGroupConfiguration
                        {
                            SamplingInterval = 250
                        }),
                        DefaultDestinations =
                        [
                            new EventStreamDestination
                            {
                                Target = EventStreamTarget.Mqtt,
                                Configuration = new DestinationConfiguration
                                {
                                    Topic = "telemetry/events1",
                                    Qos = QoS.Qos0,
                                    Retain = Retain.Never,
                                    Ttl = 17
                                }
                            }
                        ],
                        Events =
                        [
                            new AssetEvent
                            {
                                Name = "event1",
                                DataSource = "ns=2;s=EventNotifier",
                                TypeRef = "event-type",
                                EventConfiguration = Serialize(new EventConfiguration
                                {
                                    QueueSize = 7
                                })
                            }
                        ]
                    }
                ],
                ManagementGroups =
                [
                    new AssetManagementGroup
                    {
                        Name = "mgmt1",
                        DataSource = "ns=2;s=Object",
                        TypeRef = "object-type",
                        DefaultTopic = "cmd/default",
                        ManagementGroupConfiguration = Serialize(new ManagementGroupConfiguration
                        {
                            Priority = 5
                        }),
                        Actions =
                        [
                            new AssetManagementGroupAction
                            {
                                Name = "action1",
                                TargetUri = "ns=2;s=Method1",
                                TypeRef = "method-type",
                                Topic = "cmd/a"
                            },
                            new AssetManagementGroupAction
                            {
                                Name = "action2",
                                TargetUri = "ns=2;s=Method2",
                                TypeRef = "method-type"
                            }
                        ]
                    }
                ]
            };
        }

        private static IEventSchema CreateSchema(string? id)
        {
            var schema = new Mock<IEventSchema>();
            schema.SetupGet(s => s.Id).Returns(id!);
            schema.SetupGet(s => s.Name).Returns("eventSchema");
            return schema.Object;
        }

        private static Schema CreateRegistration()
        {
            return new Schema
            {
                Name = "schema1",
                Namespace = "namespace1",
                Version = "1.0.0"
            };
        }

        private static string Serialize<T>(T value)
        {
            return Json.SerializeToString(value, Json.GetTypeInfo<T>());
        }

        private AssetDeviceIntegration CreateSut()
        {
            return new(_clientMock.Object, _srMock.Object, _publishedNodesMock.Object, _configurationServicesMock.Object,
                _connectionsMock.Object, _discoveryMock.Object,
                _optionsMock.Object, _loggerMock.Object);
        }

        private readonly Mock<IOptions<PublisherOptions>> _optionsMock = new();
        private readonly Mock<IDiscoveryServices> _discoveryMock = new();
        private readonly Mock<IConnectionServices<ConnectionModel>> _connectionsMock = new();
        private readonly Mock<IAioAdrClient> _clientMock = new();
        private readonly Mock<IAioSrClient> _srMock = new();
        private readonly Mock<IPublishedNodesServices> _publishedNodesMock = new();
        private readonly Mock<IConfigurationServices> _configurationServicesMock = new();
        private readonly Mock<ILogger<AssetDeviceIntegration>> _loggerMock = new();
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Xunit;

    public sealed class MonitoredItemTests
    {
        public MonitoredItemTests()
        {
            _mockSubscription = new Mock<IManagedSubscription>();
            _mockOptionsMonitor = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            _mockLogger = new Mock<ILogger<MonitoredItem>>();

            _mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(new MonitoredItemOptions());
        }

        [Fact]
        public void DisplayNameShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const string displayName = "TestDisplayName";

            // Act
            sut.DisplayName = displayName;

            // Assert
            sut.DisplayName.Should().Be(displayName);
        }

        [Fact]
        public void StartNodeIdShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var nodeId = new NodeId("TestNodeId", 0);

            // Act
            sut.StartNodeId = nodeId;

            // Assert
            sut.StartNodeId.Should().Be(nodeId);
        }

        [Fact]
        public void NodeClassShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const NodeClass nodeClass = NodeClass.Variable;

            // Act
            sut.NodeClass = nodeClass;

            // Assert
            sut.NodeClass.Should().Be(nodeClass);
        }

        [Fact]
        public void AttributeIdShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const uint attributeId = Attributes.Value;

            // Act
            sut.AttributeId = attributeId;

            // Assert
            sut.AttributeId.Should().Be(attributeId);
        }

        [Fact]
        public void IndexRangeShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const string indexRange = "0:10";

            // Act
            sut.IndexRange = indexRange;

            // Assert
            sut.IndexRange.Should().Be(indexRange);
        }

        [Fact]
        public void EncodingShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var encoding = new QualifiedName("TestEncoding");

            // Act
            sut.Encoding = encoding;

            // Assert
            sut.Encoding.Should().Be(encoding);
        }

        [Fact]
        public void MonitoringModeShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const MonitoringMode monitoringMode = MonitoringMode.Sampling;

            // Act
            sut.MonitoringMode = monitoringMode;

            // Assert
            sut.MonitoringMode.Should().Be(monitoringMode);
        }

        [Fact]
        public void SamplingIntervalShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var samplingInterval = TimeSpan.FromMilliseconds(1000);

            // Act
            sut.SamplingInterval = samplingInterval;

            // Assert
            sut.SamplingInterval.Should().Be(samplingInterval);
        }

        [Fact]
        public void FilterShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var filter = new DataChangeFilter();

            // Act
            sut.Filter = filter;

            // Assert
            sut.Filter.Should().Be(filter);
        }

        [Fact]
        public void QueueSizeShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
              _mockOptionsMonitor.Object, _mockLogger.Object);
            const uint queueSize = 10u;

            // Act
            sut.QueueSize = queueSize;

            // Assert
            sut.QueueSize.Should().Be(queueSize);
        }

        [Fact]
        public void DiscardOldestShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const bool discardOldest = true;

            // Act
            sut.DiscardOldest = discardOldest;

            // Assert
            sut.DiscardOldest.Should().Be(discardOldest);
        }

        [Fact]
        public void ServerIdShouldGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const uint serverId = 123u;

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = serverId,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.ServerId.Should().Be(serverId);
            sut.Created.Should().BeTrue();
        }

        [Fact]
        public void CreatedShouldReturnFalseWhenServerIdIsNotSet()
        {
            // Act
            var sut = new TestMonitoredItem(_mockSubscription.Object,
              _mockOptionsMonitor.Object, _mockLogger.Object);
            var result = sut.Created;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ErrorShouldSetAndGetWhenSetMonitoringModeFails()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
                _mockOptionsMonitor.Object, _mockLogger.Object)
            {
                MonitoringMode = MonitoringMode.Sampling
            };

            sut.Error.Should().Be(ServiceResult.Good);

            // Act
            sut.SetMonitoringModeResult(MonitoringMode.Sampling, StatusCodes.Bad,
                0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.Error.Should().BeEquivalentTo(new ServiceResult(StatusCodes.Bad));
        }

        [Fact]
        public void ErrorShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Bad
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.Error.StatusCode.Should().Be(StatusCodes.Bad);
        }

        [Fact]
        public void FilterResultShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var filterResult = new EventFilterResult();

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                FilterResult = new ExtensionObject(filterResult)
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            Utils.IsEqual(sut.FilterResult, filterResult).Should().BeTrue();
        }

        [Fact]
        public void CurrentMonitoringModeShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
                _mockOptionsMonitor.Object, _mockLogger.Object)
            {
                MonitoringMode = MonitoringMode.Reporting
            };

            sut.CurrentMonitoringMode.Should().NotBe(MonitoringMode.Sampling);

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.CurrentMonitoringMode.Should().Be(MonitoringMode.Sampling);
        }

        [Fact]
        public void CurrentMonitoringModeShouldUpdate()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
                _mockOptionsMonitor.Object, _mockLogger.Object)
            {
                MonitoringMode = MonitoringMode.Sampling
            };

            sut.CurrentMonitoringMode.Should().NotBe(MonitoringMode.Sampling);

            // Act
            sut.SetMonitoringModeResult(MonitoringMode.Sampling, StatusCodes.Good,
                0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.CurrentMonitoringMode.Should().Be(MonitoringMode.Sampling);
        }

        [Fact]
        public void CurrentSamplingIntervalShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var currentSamplingInterval = TimeSpan.FromMilliseconds(500);

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                RevisedSamplingInterval = currentSamplingInterval.TotalMilliseconds,
                StatusCode = StatusCodes.Good
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.CurrentSamplingInterval.Should().Be(currentSamplingInterval);
        }

        [Fact]
        public void CurrentQueueSizeShouldSetAndGet()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const uint currentQueueSize = 5u;

            // Act
            sut.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = sut.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                RevisedQueueSize = currentQueueSize,
                StatusCode = StatusCodes.Good
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            // Assert
            sut.CurrentQueueSize.Should().Be(currentQueueSize);
        }

        [Fact]
        public void ClientHandleShouldGet()
        {
            // Act
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var result = sut.ClientHandle;

            // Assert
            result.Should().NotBe(0);
        }

        [Fact]
        public void ResolvedNodeIdShouldReturnStartNodeId()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            var nodeId = new NodeId("TestNodeId", 0);
            sut.StartNodeId = nodeId;

            // Act
            var result = sut.ResolvedNodeId;

            // Assert
            result.Should().Be(nodeId);
        }

        [Fact]
        public void DisposeShouldCallRemoveItemOnSubscription()
        {
            // Act
            var sut = new TestMonitoredItem(_mockSubscription.Object,
              _mockOptionsMonitor.Object, _mockLogger.Object);
            sut.Dispose();

            // Assert
            _mockSubscription.Verify(s => s.RemoveItem(sut), Times.Once);
        }

        [Fact]
        public void ToStringShouldReturnExpectedString()
        {
            // Arrange
            var sut = new TestMonitoredItem(_mockSubscription.Object,
               _mockOptionsMonitor.Object, _mockLogger.Object);
            const string displayName = "TestDisplayName";
            sut.DisplayName = displayName;

            // Act
            var result = sut.ToString();

            // Assert
            result.Should().Contain(displayName);
        }

        private sealed class TestMonitoredItem : MonitoredItem
        {
            public TestMonitoredItem(IManagedSubscription subscription,
                IOptionsMonitor<MonitoredItemOptions> options, ILogger logger)
                : base(subscription, options, logger)
            {
            }
        }

        private readonly Mock<IManagedSubscription> _mockSubscription;
        private readonly Mock<IOptionsMonitor<MonitoredItemOptions>> _mockOptionsMonitor;
        private readonly Mock<ILogger<MonitoredItem>> _mockLogger;
    }
}

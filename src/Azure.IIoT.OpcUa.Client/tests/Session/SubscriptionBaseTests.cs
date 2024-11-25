// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Nito.AsyncEx;
    using Xunit;

    public sealed class SubscriptionBaseTests
    {
        public SubscriptionBaseTests()
        {
            _mockSession = new Mock<ISubscriptionContext>();
            _mockCompletion = new Mock<IMessageAckQueue>();
            _mockOptions = new Mock<IOptionsMonitor<SubscriptionOptions>>();

            _mockTimeProvider = new Mock<TimeProvider>();
            _mockTimer = new Mock<ITimer>();
            _mockTimeProvider
                .Setup(t => t.CreateTimer(
                    It.IsAny<TimerCallback>(),
                    It.IsAny<object>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>()))
                .Returns(_mockTimer.Object);
            _mockObservability = new Mock<IObservability>();
            _mockObservability
                .Setup(o => o.TimeProvider).Returns(_mockTimeProvider.Object);
            _mockLogger = new Mock<ILogger<SubscriptionBase>>();
            _mockObservability
                .Setup(o => o.LoggerFactory.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
        }

        [Fact]
        public void AddMonitoredItemShouldAddItemToMonitoredItems()
        {
            // Arrange
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();

            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object);

            // Act
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);

            // Assert
            sut.MonitoredItems.Should().Contain(monitoredItem);
        }

        [Fact]
        public async Task ApplyChangesAsyncShouldCallCreateAsyncIfNotCreatedAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromSeconds(100),
                KeepAliveCount = 7,
                LifetimeCount = 15,
                Priority = 3,
                MaxNotificationsPerPublish = 10
            };
            _mockSession
                .Setup(s => s.CreateSubscriptionAsync(It.IsAny<RequestHeader>(),
                    sut.PublishingInterval.TotalMilliseconds, 21, 7, 10, true,
                    3, ct))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    SubscriptionId = 22,
                    RevisedLifetimeCount = 10,
                    RevisedMaxKeepAliveCount = 5,
                    RevisedPublishingInterval = 10000
                })
                .Verifiable(Times.Once);

            sut.Created.Should().BeFalse();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.Zero);
            sut.CurrentKeepAliveCount.Should().Be(0);
            sut.CurrentLifetimeCount.Should().Be(0);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(0);
            sut.CurrentPriority.Should().Be(0);

            // Act
            await sut.ApplyChangesAsync(ct);

            // Assert
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.FromSeconds(10));
            sut.CurrentKeepAliveCount.Should().Be(5);
            sut.CurrentLifetimeCount.Should().Be(10);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(10);
            sut.CurrentPriority.Should().Be(3);
            sut.Id.Should().Be(22);
            _mockSession.Verify();
        }

        [Fact]
        public async Task ApplyChangesAsyncShouldCallModifyAsyncIfCreatedAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromSeconds(100),
                KeepAliveCount = 7,
                LifetimeCount = 15,
                Priority = 3,
                MaxNotificationsPerPublish = 10
            };
            _mockSession
                .Setup(s => s.ModifySubscriptionAsync(It.IsAny<RequestHeader>(),
                    22, sut.PublishingInterval.TotalMilliseconds, 21, 7, 10,
                    3, ct))
                .ReturnsAsync(new ModifySubscriptionResponse
                {
                    RevisedLifetimeCount = 10,
                    RevisedMaxKeepAliveCount = 5,
                    RevisedPublishingInterval = 10000
                })
                .Verifiable(Times.Once);
            _mockSession
                .Setup(s => s.SetPublishingModeAsync(It.IsAny<RequestHeader>(),
                    true, new UInt32Collection { 22 }, ct))
                .ReturnsAsync(new SetPublishingModeResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                })
                .Verifiable(Times.Once);

            sut.Id = 22; // Created
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingEnabled.Should().BeFalse();

            // Act
            await sut.ApplyChangesAsync(ct);

            // Assert
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.FromSeconds(10));
            sut.CurrentKeepAliveCount.Should().Be(5);
            sut.CurrentLifetimeCount.Should().Be(10);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(10);
            sut.CurrentPriority.Should().Be(3);
            sut.CurrentPublishingEnabled.Should().BeTrue();
            _mockSession.Verify();
        }

        [Fact]
        public async Task ApplyMonitoredItemChangesAsyncShouldNotDoAnythingAsync()
        {
            // Arrange
            var ct = CancellationToken.None;

            var sut = new TestSubscriptionBase(_mockSession.Object,
              _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };

            // Act
            await sut.ApplyMonitoredItemChangesAsync(ct);
        }

        [Fact]
        public async Task ApplyMonitoredItemChangesAsyncShouldAddCreatedItemsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;

            var sut = new TestSubscriptionBase(_mockSession.Object,
              _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            _mockSession
                .Setup(s => s.CreateMonitoredItemsAsync(It.IsAny<RequestHeader>(), 2,
                    TimestampsToReturn.Both,
                    It.IsAny<MonitoredItemCreateRequestCollection>(), ct))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            MonitoredItemId = 100,
                            RevisedSamplingInterval = 10000,
                            RevisedQueueSize = 10,
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.ApplyMonitoredItemChangesAsync(ct);

            monitoredItem.ServerId.Should().Be(100);
            monitoredItem.CurrentSamplingInterval.Should().Be(TimeSpan.FromSeconds(10));
            monitoredItem.CurrentQueueSize.Should().Be(10);
            monitoredItem.CurrentMonitoringMode.Should().Be(MonitoringMode.Reporting);
            _mockSession.Verify();
        }

        [Fact]
        public async Task ApplyMonitoredItemChangesAsyncShouldApplyChangesAsync()
        {
            // Arrange
            var ct = CancellationToken.None;

            var sut = new TestSubscriptionBase(_mockSession.Object,
              _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 100,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            monitoredItem.ServerId.Should().Be(100);
            monitoredItem.SamplingInterval = TimeSpan.FromSeconds(100);
            monitoredItem.CurrentMonitoringMode.Should().Be(MonitoringMode.Sampling);

            _mockSession
                .Setup(s => s.ModifyMonitoredItemsAsync(It.IsAny<RequestHeader>(), 2,
                    TimestampsToReturn.Both,
                    It.IsAny<MonitoredItemModifyRequestCollection>(), ct))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            RevisedSamplingInterval = 100000,
                            RevisedQueueSize = 1000,
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.ApplyMonitoredItemChangesAsync(ct);

            monitoredItem.CurrentSamplingInterval.Should().Be(TimeSpan.FromSeconds(100));
            monitoredItem.CurrentQueueSize.Should().Be(1000);
            monitoredItem.CurrentMonitoringMode.Should().Be(MonitoringMode.Sampling);
            _mockSession.Verify();
        }

        [Fact]
        public async Task SetMonitoringModeAsyncShouldCallSessionSetMonitoringModeAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var monitoredItems = new List<MonitoredItem>
            {
            };
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 100,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            monitoredItem.ServerId.Should().Be(100);
            monitoredItem.CurrentMonitoringMode.Should().Be(MonitoringMode.Sampling);
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;

            _mockSession
                .Setup(s => s.SetMonitoringModeAsync(It.IsAny<RequestHeader>(),
                    2, MonitoringMode.Reporting, new UInt32Collection { 100 }, ct))
                .ReturnsAsync(new SetMonitoringModeResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.SetMonitoringModeAsync(MonitoringMode.Reporting,
                new List<MonitoredItem> { monitoredItem }, ct);

            // Assert
            monitoredItem.CurrentMonitoringMode.Should().Be(MonitoringMode.Reporting);
            _mockSession.Verify();
        }

        [Fact]
        public async Task ApplyMonitoredItemChangesAsyncShouldRemoveRemovedItemsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;

            var sut = new TestSubscriptionBase(_mockSession.Object,
              _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 100,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            monitoredItem.Dispose();

            _mockSession
                .Setup(s => s.DeleteMonitoredItemsAsync(It.IsAny<RequestHeader>(), 2,
                    new UInt32Collection { 100 }, ct))
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.ApplyMonitoredItemChangesAsync(ct);
            sut.MonitoredItemCount.Should().Be(0);
            _mockSession.Verify();
        }

        [Fact]
        public async Task ConditionRefreshAsyncShouldCallSessionCallAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };

            // Assert
            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.IsAny<CallMethodRequestCollection>(), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.ConditionRefreshAsync(ct);

            // Assert
            _mockSession.Verify();
        }

        [Fact]
        public async Task DeleteAsyncShouldCallSessionDeleteSubscriptionsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 22
            };

            _mockSession
                .Setup(s => s.DeleteSubscriptionsAsync(
                    It.IsAny<RequestHeader>(), new UInt32Collection { 22 }, ct))
                .ReturnsAsync(new DeleteSubscriptionsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                }).
                Verifiable(Times.Once);

            // Act
            await sut.DeleteAsync(false, ct);

            // Assert
            _mockSession.Verify();
        }

        [Fact]
        public async Task DeleteAsyncShouldCallSessionDeleteSubscriptionsAndCleanupMonitoredItemsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 22
            };
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 199,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            _mockSession
                .Setup(s => s.DeleteSubscriptionsAsync(
                    It.IsAny<RequestHeader>(), new UInt32Collection { 22 }, ct))
                .ReturnsAsync(new DeleteSubscriptionsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                }).
                Verifiable(Times.Once);

            // Act
            await sut.DeleteAsync(false, ct);

            // Assert
            _mockSession.Verify();
            sut.MonitoredItems.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsyncShouldCatchAllExceptionsIfSilentAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 22
            };

            _mockSession
                .Setup(s => s.DeleteSubscriptionsAsync(
                    It.IsAny<RequestHeader>(), new UInt32Collection { 22 }, ct))
                .ReturnsAsync(new DeleteSubscriptionsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Bad }
                }).
                Verifiable(Times.Once);

            // Act
            await sut.DeleteAsync(true, ct);

            // Assert
            _mockSession.Verify();
        }

        [Fact]
        public async Task DeleteAsyncShouldThrowIfNotSilentAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 22
            };

            _mockSession
                .Setup(s => s.DeleteSubscriptionsAsync(
                    It.IsAny<RequestHeader>(), new UInt32Collection { 22 }, ct))
                .ReturnsAsync(new DeleteSubscriptionsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Bad }
                }).
                Verifiable(Times.Once);

            // Act
            await sut.Invoking(async s => await s.DeleteAsync(false, ct))
                .Should().ThrowAsync<ServiceResultException>();

            // Assert
            _mockSession.Verify();
        }

        [Fact]
        public async Task DisposeAsyncShouldDisposePublishTimerAsync()
        {
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object);
            // Act
            await sut.DisposeAsync();

            // Assert
            _mockTimer.Verify(t => t.Dispose(), Times.Once);
        }

        [Fact]
        public void FindItemByClientHandleShouldReturnMonitoredItem()
        {
            // Arrange
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object);
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);

            // Act
            var result = sut.FindItemByClientHandle(monitoredItem.ClientHandle);

            // Assert
            result.Should().Be(monitoredItem);
        }

        [Fact]
        public async Task OnPublishReceivedAsyncShouldResetKeepAliveTimerAsync()
        {
            // Arrange
            var message = new NotificationMessage();
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object);

            // Act
            await sut.OnPublishReceivedAsync(message, null, null);

            // Assert
            _mockTimer.Verify(t => t.Change(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task RecreateAsyncShouldReCreateSubscriptionAndSyncStateAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromSeconds(100),
                KeepAliveCount = 7,
                LifetimeCount = 15,
                Priority = 3,
                MaxNotificationsPerPublish = 10,
                Id = 10
            };
            _mockSession
                .Setup(s => s.CreateSubscriptionAsync(It.IsAny<RequestHeader>(),
                    sut.PublishingInterval.TotalMilliseconds, 21, 7, 10, true,
                    3, ct))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    SubscriptionId = 22,
                    RevisedLifetimeCount = 10,
                    RevisedMaxKeepAliveCount = 5,
                    RevisedPublishingInterval = 10000
                })
                .Verifiable(Times.Once);

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(22u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            OutputArguments = new VariantCollection
                            {
                                new Variant(Array.Empty<uint>()), // serverHandles
                                new Variant(Array.Empty<uint>())  // clientHandles
                            }
                        }
                    }
                })
                .Verifiable(Times.Once);

            sut.Created.Should().BeTrue();

            // Act
            await sut.RecreateAsync(ct);

            // Assert
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.FromSeconds(10));
            sut.CurrentKeepAliveCount.Should().Be(5);
            sut.CurrentLifetimeCount.Should().Be(10);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(10);
            sut.CurrentPriority.Should().Be(3);
            sut.Id.Should().Be(22);
            _mockSession.Verify();
        }

        [Fact]
        public async Task RecreateAsyncShouldReCreateSubscriptionAndMonitoredItemsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromSeconds(100),
                KeepAliveCount = 7,
                LifetimeCount = 15,
                Priority = 3,
                MaxNotificationsPerPublish = 10,
                Id = 10
            };

            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 199,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            _mockSession
                .Setup(s => s.CreateSubscriptionAsync(It.IsAny<RequestHeader>(),
                    sut.PublishingInterval.TotalMilliseconds, 21, 7, 10, true,
                    3, ct))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    SubscriptionId = 22,
                    RevisedLifetimeCount = 10,
                    RevisedMaxKeepAliveCount = 5,
                    RevisedPublishingInterval = 10000
                })
                .Verifiable(Times.Once);

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(22u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            OutputArguments = new VariantCollection
                            {
                                new Variant(Array.Empty<uint>()), // serverHandles
                                new Variant(Array.Empty<uint>())  // clientHandles
                            }
                        }
                    }
                })
                .Verifiable(Times.Once);
            _mockSession
                .Setup(s => s.CreateMonitoredItemsAsync(It.IsAny<RequestHeader>(), 22,
                    TimestampsToReturn.Both,
                    It.IsAny<MonitoredItemCreateRequestCollection>(), ct))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            MonitoredItemId = 200,
                            RevisedSamplingInterval = 10000,
                            RevisedQueueSize = 10
                        }
                    }
                })
                .Verifiable(Times.Once);

            sut.Created.Should().BeTrue();

            // Act
            await sut.RecreateAsync(ct);

            // Assert
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.FromSeconds(10));
            sut.CurrentKeepAliveCount.Should().Be(5);
            sut.CurrentLifetimeCount.Should().Be(10);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(10);
            sut.CurrentPriority.Should().Be(3);
            sut.Id.Should().Be(22);
            monitoredItem.ServerId.Should().Be(200);
            _mockSession.Verify();
        }

        [Fact]
        public async Task RecreateAsyncShouldReCreateSubscriptionEventIfStateSyncFailsAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromSeconds(100),
                KeepAliveCount = 7,
                LifetimeCount = 15,
                Priority = 3,
                MaxNotificationsPerPublish = 10,
                Id = 10
            };

            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 199,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            _mockSession
                .Setup(s => s.CreateSubscriptionAsync(It.IsAny<RequestHeader>(),
                    sut.PublishingInterval.TotalMilliseconds, 21, 7, 10, true,
                    3, ct))
                .ReturnsAsync(new CreateSubscriptionResponse
                {
                    SubscriptionId = 22,
                    RevisedLifetimeCount = 10,
                    RevisedMaxKeepAliveCount = 5,
                    RevisedPublishingInterval = 10000
                })
                .Verifiable(Times.Once);

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(22u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Bad
                        }
                    }
                })
                .Verifiable(Times.Once);
            _mockSession
                .Setup(s => s.CreateMonitoredItemsAsync(It.IsAny<RequestHeader>(), 22,
                    TimestampsToReturn.Both,
                    It.IsAny<MonitoredItemCreateRequestCollection>(), ct))
                .ReturnsAsync(new CreateMonitoredItemsResponse
                {
                    Results = new MonitoredItemCreateResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            MonitoredItemId = 200,
                            RevisedSamplingInterval = 10000,
                            RevisedQueueSize = 10
                        }
                    }
                })
                .Verifiable(Times.Once);
            sut.Created.Should().BeTrue();

            // Act
            await sut.RecreateAsync(ct);

            // Assert
            sut.Created.Should().BeTrue();
            sut.CurrentPublishingInterval.Should().Be(TimeSpan.FromSeconds(10));
            sut.CurrentKeepAliveCount.Should().Be(5);
            sut.CurrentLifetimeCount.Should().Be(10);
            sut.CurrentMaxNotificationsPerPublish.Should().Be(10);
            sut.CurrentPriority.Should().Be(3);
            sut.Id.Should().Be(22);
            sut.MonitoredItemCount.Should().Be(1);
            monitoredItem.ServerId.Should().Be(200);
            _mockSession.Verify();
        }

        [Fact]
        public void RemoveItemShouldRemoveItemFromMonitoredItems()
        {
            // Arrange
            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object);
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);

            // Act
            sut.RemoveItem(monitoredItem);

            // Assert
            sut.MonitoredItems.Should().NotContain(monitoredItem);
        }

        [Fact]
        public async Task TransferAsyncShouldCallGetMonitoredItemsAsyncAndCreateServerItemAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };

            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(2u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            OutputArguments = new VariantCollection
                            {
                                new Variant(new[]{199u}), // serverHandles
                                new Variant(new[]{monitoredItem.ClientHandle})  // clientHandles
                            }
                        }
                    }
                })
                .Verifiable(Times.Once);
            _mockSession
                .Setup(s => s.ModifyMonitoredItemsAsync(It.IsAny<RequestHeader>(), 2,
                    TimestampsToReturn.Both,
                    It.IsAny<MonitoredItemModifyRequestCollection>(), ct))
                .ReturnsAsync(new ModifyMonitoredItemsResponse
                {
                    Results = new MonitoredItemModifyResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            RevisedSamplingInterval = 100000,
                            RevisedQueueSize = 1000
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            var success = await sut.TryCompleteTransferAsync(Array.Empty<uint>(), ct);

            // Assert
            _mockSession.Verify();
            success.Should().BeTrue();
            monitoredItem.ServerId.Should().Be(199);
        }

        [Fact]
        public async Task TransferAsyncShouldCallGetMonitoredItemsAsyncAndDoNothingIfStateIsCorrectAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };

            var mockMonitoredItemOptions = new Mock<IOptionsMonitor<MonitoredItemOptions>>();
            var monitoredItem = sut.AddMonitoredItem(mockMonitoredItemOptions.Object);
            monitoredItem.SetCreateResult(new MonitoredItemCreateRequest
            {
                MonitoringMode = MonitoringMode.Sampling,
                RequestedParameters = new MonitoringParameters
                {
                    ClientHandle = monitoredItem.ClientHandle,
                    SamplingInterval = 1000,
                    QueueSize = 5,
                    DiscardOldest = true
                }
            }, new MonitoredItemCreateResult
            {
                StatusCode = StatusCodes.Good,
                MonitoredItemId = 199,
                RevisedSamplingInterval = 10000,
                RevisedQueueSize = 10,
            }, 0, new DiagnosticInfoCollection(), new ResponseHeader());

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(2u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            OutputArguments = new VariantCollection
                            {
                                new Variant(new[]{monitoredItem.ServerId}), // serverHandles
                                new Variant(new[]{monitoredItem.ClientHandle})  // clientHandles
                            }
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            var success = await sut.TryCompleteTransferAsync(Array.Empty<uint>(), ct);

            // Assert
            _mockSession.Verify();
            monitoredItem.ServerId.Should().Be(199);
            success.Should().BeTrue();
        }

        [Fact]
        public async Task TransferAsyncShouldCallGetMonitoredItemsAsyncAndDeleteItemIfNotInSubscriptionAsync()
        {
            // Arrange
            var ct = CancellationToken.None;
            var sut = new TestSubscriptionBase(_mockSession.Object,
                _mockCompletion.Object, _mockOptions.Object, _mockObservability.Object)
            {
                Id = 2
            };

            _mockSession
                .Setup(s => s.CallAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<CallMethodRequestCollection>(r =>
                           r.Count == 1
                        && r[0].InputArguments.Count == 1
                        && r[0].InputArguments[0].Value.Equals(2u)
                        && r[0].ObjectId == ObjectIds.Server
                        && r[0].MethodId == MethodIds.Server_GetMonitoredItems), ct))
                .ReturnsAsync(new CallResponse
                {
                    Results = new CallMethodResultCollection
                    {
                        new ()
                        {
                            StatusCode = StatusCodes.Good,
                            OutputArguments = new VariantCollection
                            {
                                new Variant(new[]{199u}), // serverHandles
                                new Variant(new[]{3u})  // clientHandles
                            }
                        }
                    }
                })
                .Verifiable(Times.Once);

            _mockSession
                .Setup(s => s.DeleteMonitoredItemsAsync(It.IsAny<RequestHeader>(), 2,
                    new UInt32Collection { 199 }, ct))
                .ReturnsAsync(new DeleteMonitoredItemsResponse
                {
                    Results = new StatusCodeCollection { StatusCodes.Good }
                })
                .Verifiable(Times.Once);

            // Act
            var success = await sut.TryCompleteTransferAsync(Array.Empty<uint>(), ct);

            // Assert
            _mockSession.Verify();
            success.Should().BeTrue();
        }

        private class TestMonitoredItem : MonitoredItem
        {
            public TestMonitoredItem(IManagedSubscription subscription,
                IOptionsMonitor<MonitoredItemOptions> options, ILogger logger)
                : base(subscription, options, logger)
            {
            }
        }

        private class TestSubscriptionBase : SubscriptionBase
        {
            public TestSubscriptionBase(ISubscriptionContext session,
                IMessageAckQueue completion, IOptionsMonitor<SubscriptionOptions> options,
                IObservability observability)
                : base(session, completion, options, observability)
            {
            }
            public SemaphoreSlim Block { get; } = new(1, 1);
            public AsyncManualResetEvent DataChangeNotificationReceived { get; } = new();
            public AsyncManualResetEvent EventNotificationReceived { get; } = new();
            public AsyncManualResetEvent KeepAliveNotificationReceived { get; } = new();
            public PublishState PublishState { get; set; }
            public List<uint> ReceivedSequenceNumbers { get; } = new List<uint>();
            public AsyncManualResetEvent StatusChangeNotificationReceived { get; } = new();
            public void SetCreated(bool created)
            {
                Id = created ? 1u : 0u;
            }

            public async ValueTask WaitAsync()
            {
                await Block.WaitAsync();
                Block.Release();
            }

            protected override MonitoredItem CreateMonitoredItem(IOptionsMonitor<MonitoredItemOptions> options)
            {
                return new TestMonitoredItem(this, options, new Mock<ILogger>().Object);
            }

            protected override ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, DataChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                DataChangeNotificationReceived.Set();
                ReceivedSequenceNumbers.Add(sequenceNumber);
                if (publishStateMask != PublishState.None)
                {
                    PublishState = publishStateMask;
                }
                return WaitAsync();
            }

            protected override ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
                DateTime publishTime, EventNotificationList notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                EventNotificationReceived.Set();
                ReceivedSequenceNumbers.Add(sequenceNumber);
                if (publishStateMask != PublishState.None)
                {
                    PublishState = publishStateMask;
                }
                return WaitAsync();
            }

            protected override ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
                                                    DateTime publishTime, PublishState publishStateMask)
            {
                KeepAliveNotificationReceived.Set();
                ReceivedSequenceNumbers.Add(sequenceNumber);
                if (publishStateMask != PublishState.None)
                {
                    PublishState = publishStateMask;
                }
                return WaitAsync();
            }
            protected override void OnPublishStateChanged(PublishState stateMask)
            {
                PublishState = stateMask;
                base.OnPublishStateChanged(stateMask);
            }
        }

        private readonly Mock<IMessageAckQueue> _mockCompletion;
        private readonly Mock<IObservability> _mockObservability;
        private readonly Mock<IOptionsMonitor<SubscriptionOptions>> _mockOptions;
        private readonly Mock<TimeProvider> _mockTimeProvider;
        private readonly Mock<ISubscriptionContext> _mockSession;
        private readonly Mock<ITimer> _mockTimer;
        private readonly Mock<ILogger<SubscriptionBase>> _mockLogger;
    }
}

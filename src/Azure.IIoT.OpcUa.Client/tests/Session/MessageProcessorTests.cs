// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Nito.AsyncEx;
    using Xunit;

    public class MessageProcessorTests
    {
        private readonly Mock<ISubscriptionContext> _mockSession;
        private readonly Mock<IMessageAckQueue> _mockCompletion;
        private readonly Mock<IObservability> _mockObservability;
        private readonly Mock<ILogger<SubscriptionBase>> _mockLogger;
        private readonly Mock<TimeProvider> _mockTimeProvider;

        public MessageProcessorTests()
        {
            _mockSession = new Mock<ISubscriptionContext>();
            _mockCompletion = new Mock<IMessageAckQueue>();
            _mockObservability = new Mock<IObservability>();
            _mockLogger = new Mock<ILogger<SubscriptionBase>>();
            _mockTimeProvider = new Mock<TimeProvider>();

            _mockObservability.Setup(o => o.LoggerFactory.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
            _mockObservability.Setup(o => o.TimeProvider).Returns(_mockTimeProvider.Object);
        }

        [Fact]
        public async Task DisposeAsyncShouldCompleteMessageWriterAndCancelTokenAsync()
        {
            // Arrange
            var sut = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object)
            {
                Id = 3
            };
            _mockCompletion
                .Setup(c => c.CompleteAsync(
                    It.Is<uint>(i => i == 3),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable(Times.Once);

            // Act
            await sut.DisposeAsync();

            // Assert
            sut.PublishState.Should().Be(PublishState.Completed);
            _mockCompletion.Verify();
        }

        [Fact]
        public async Task OnPublishReceivedKeepAliveShouldDispatchKeepAliveAsync()
        {
            // Arrange
            var message = new NotificationMessage
            {
                SequenceNumber = 3
            };
            var availableSequenceNumbers = new List<uint> { 1, 2, 3 };
            var stringTable = new List<string> { "test" };
            await using var sut = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object)
            {
                Id = 3
            };

            // Act
            await sut.OnPublishReceivedAsync(message, availableSequenceNumbers, stringTable);
            await sut.KeepAliveNotificationReceived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            sut.KeepAliveNotificationReceived.IsSet.Should().BeTrue();
            sut.AvailableInRetransmissionQueue.Should().BeEquivalentTo(availableSequenceNumbers);
            sut.LastSequenceNumberProcessed.Should().Be(3);
            sut.DataChangeNotificationReceived.IsSet.Should().BeFalse();

            // Arrange
            sut.KeepAliveNotificationReceived.Reset();
            sut.DataChangeNotificationReceived.Reset();
            message = new NotificationMessage
            {
                SequenceNumber = 4,
                NotificationData = new ExtensionObjectCollection
                {
                    new ExtensionObject(new DataChangeNotification
                    {
                        MonitoredItems = new MonitoredItemNotificationCollection
                        {
                            new MonitoredItemNotification()
                        }
                    })
                }
            };

            // Act
            await sut.OnPublishReceivedAsync(message, availableSequenceNumbers, stringTable);
            await sut.DataChangeNotificationReceived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            sut.AvailableInRetransmissionQueue.Should().BeEquivalentTo(availableSequenceNumbers);
            sut.DataChangeNotificationReceived.IsSet.Should().BeTrue();
            sut.KeepAliveNotificationReceived.IsSet.Should().BeFalse();
            sut.LastSequenceNumberProcessed.Should().Be(4);
        }

        [Fact]
        public async Task ProcessReceivedMessagesAsyncShouldProcessMessagesInOrderAsync()
        {
            var availableSequenceNumbers = new List<uint> { 1, 2, 3 };
            var stringTable = new List<string> { "test" };

            // Arrange
            var messages = Enumerable.Range(2, 99).Select(i => new NotificationMessage
            {
                SequenceNumber = (uint)i
            }).ToList();
            messages.Shuffle();

            await using var sut = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object)
            {
                Id = 3
            };

            sut.Block.Wait();
            await sut.OnPublishReceivedAsync(new NotificationMessage
            {
                SequenceNumber = 1u
            }, availableSequenceNumbers, stringTable);
            foreach (var message in messages)
            {
                await sut.OnPublishReceivedAsync(message, availableSequenceNumbers, stringTable);
            }
            sut.Block.Release();

            // Act
            await Task.Delay(10);

            sut.ReceivedSequenceNumbers.Should().BeEquivalentTo(
                Enumerable.Range(1, 100).Select(i => (uint)i));
            sut.AvailableInRetransmissionQueue.Should().BeEquivalentTo(availableSequenceNumbers);
            sut.DataChangeNotificationReceived.IsSet.Should().BeFalse();
            sut.KeepAliveNotificationReceived.IsSet.Should().BeTrue();
            sut.LastSequenceNumberProcessed.Should().Be(100);
        }

        [Fact]
        public async Task ProcessMessageAsyncShouldRepublishMissingMessagesAsync()
        {
            // Arrange
            var availableSequenceNumbers = new List<uint> { 1, 2, 3 };
            var stringTable = new List<string> { "test" };

            await using var sut = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object)
            {
                Id = 2
            };

            await sut.OnPublishReceivedAsync(new NotificationMessage
            {
                SequenceNumber = 1
            }, availableSequenceNumbers, stringTable);

            _mockSession
                .Setup(c => c.RepublishAsync(
                    It.IsAny<RequestHeader>(),
                    It.Is<uint>(id => id == sut.Id),
                    It.Is<uint>(s => s == 2),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepublishResponse
                {
                    NotificationMessage = new NotificationMessage
                    {
                        SequenceNumber = 2,
                        NotificationData = new ExtensionObjectCollection
                        {
                            new ExtensionObject(new DataChangeNotification
                            {
                                MonitoredItems = new MonitoredItemNotificationCollection
                                {
                                    new MonitoredItemNotification()
                                }
                            })
                        }
                    }
                })
                .Verifiable(Times.Once);

            // Act
            await sut.OnPublishReceivedAsync(new NotificationMessage
            {
                SequenceNumber = 3,
                NotificationData = new ExtensionObjectCollection
                {
                    new ExtensionObject(new EventNotificationList
                    {
                        Events = new EventFieldListCollection
                        {
                            new EventFieldList()
                        }
                    })
                }
            }, availableSequenceNumbers, stringTable);
            await sut.EventNotificationReceived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            sut.EventNotificationReceived.IsSet.Should().BeTrue();
            sut.KeepAliveNotificationReceived.IsSet.Should().BeTrue();
            sut.DataChangeNotificationReceived.IsSet.Should().BeTrue();

            _mockSession.Verify();
        }

        [Fact]
        public async Task ReceivingTransferStatusUpdateShouldUpdatePublishStateAsync()
        {
            // Arrange
            var availableSequenceNumbers = new List<uint> { 1, 2, 3 };
            var stringTable = new List<string> { "test" };
            await using var sut = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object)
            {
                Id = 3
            };

            // Act
            await sut.OnPublishReceivedAsync(new NotificationMessage
            {
                SequenceNumber = 3,
                NotificationData = new ExtensionObjectCollection
                {
                    new ExtensionObject(new StatusChangeNotification
                    {
                        Status = StatusCodes.GoodSubscriptionTransferred
                    })
                }
            }, availableSequenceNumbers, stringTable);
            await sut.StatusChangeNotificationReceived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            sut.StatusChangeNotificationReceived.IsSet.Should().BeTrue();
            sut.ReceivedSequenceNumbers.Should().Contain(3);
            sut.LastSequenceNumberProcessed.Should().Be(3);
            sut.PublishState.Should().Be(PublishState.Transferred);

            sut.StatusChangeNotificationReceived.Reset();

            // Act
            await sut.OnPublishReceivedAsync(new NotificationMessage
            {
                SequenceNumber = 4,
                NotificationData = new ExtensionObjectCollection
                {
                    new ExtensionObject(new StatusChangeNotification
                    {
                        Status = StatusCodes.BadTimeout
                    })
                }
            }, availableSequenceNumbers, stringTable);
            await sut.StatusChangeNotificationReceived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            sut.StatusChangeNotificationReceived.IsSet.Should().BeTrue();
            sut.ReceivedSequenceNumbers.Should().Contain(4);
            sut.LastSequenceNumberProcessed.Should().Be(4);
            sut.PublishState.Should().Be(PublishState.Timeout);
        }

        private class TestMessageProcessor : MessageProcessor
        {
            public TestMessageProcessor(ISubscriptionContext session,
                IMessageAckQueue completion, IObservability observability)
                : base(session, completion, observability)
            {
            }

            public IReadOnlyList<uint> AvailableInRetransmissionQueue
            {
                get => _availableInRetransmissionQueue;
                set => _availableInRetransmissionQueue = value;
            }
            public uint LastSequenceNumberProcessed
            {
                get => _lastSequenceNumberProcessed;
                set => _lastSequenceNumberProcessed = value;
            }

            public AsyncManualResetEvent KeepAliveNotificationReceived { get; } = new();
            public AsyncManualResetEvent DataChangeNotificationReceived { get; } = new();
            public AsyncManualResetEvent EventNotificationReceived { get; } = new();
            public AsyncManualResetEvent StatusChangeNotificationReceived { get; } = new();

            public List<uint> ReceivedSequenceNumbers { get; } = new List<uint>();
            public PublishState PublishState { get; set; }

            public async ValueTask WaitAsync()
            {
                await Block.WaitAsync();
                Block.Release();
            }
            public SemaphoreSlim Block { get; } = new (1, 1);

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

            protected override async ValueTask OnStatusChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, StatusChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                StatusChangeNotificationReceived.Set();
                ReceivedSequenceNumbers.Add(sequenceNumber);
                if (publishStateMask != PublishState.None)
                {
                    PublishState = publishStateMask;
                }
                await WaitAsync();
                await base.OnStatusChangeNotificationAsync(sequenceNumber, publishTime, notification,
                    publishStateMask, stringTable);
            }

            protected override void OnPublishStateChanged(PublishState stateMask)
            {
                PublishState = stateMask;
                base.OnPublishStateChanged(stateMask);
            }
        }
    }
}

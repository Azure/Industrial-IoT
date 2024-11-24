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
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class MessageProcessorTests : IDisposable
    {
        private readonly Mock<ISubscriptionContext> _mockSession;
        private readonly Mock<IMessageAckQueue> _mockCompletion;
        private readonly Mock<IObservability> _mockObservability;
        private readonly Mock<ILogger<SubscriptionBase>> _mockLogger;
        private readonly Mock<TimeProvider> _mockTimeProvider;
        private readonly TestMessageProcessor _processor;

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

            _processor = new TestMessageProcessor(_mockSession.Object,
                _mockCompletion.Object, _mockObservability.Object);
        }

        public void Dispose()
        {
            _processor.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task DisposeAsyncShouldCompleteMessageWriterAndCancelTokenAsync()
        {
            // Act
            await _processor.DisposeAsync();

            // Assert
            _processor.CancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            _processor.MessageChannel.Reader.Completion.IsCompletedSuccessfully.Should().BeTrue();
            _processor.MessageChannel.Writer.TryWrite(new MessageProcessor.IncomingMessage(
                new NotificationMessage
                {
                    SequenceNumber = 1
                },
                new List<string>(), DateTime.UtcNow)).Should().BeFalse();
            _processor.MessageChannel.Reader.TryRead(out _).Should().BeFalse();
        }

        [Fact]
        public async Task OnPublishReceivedAsyncShouldUpdateAvailableSequenceNumbersAndWriteMessageAsync()
        {
            // Arrange
            var message = new NotificationMessage();
            var availableSequenceNumbers = new List<uint> { 1, 2, 3 };
            var stringTable = new List<string> { "test" };

            // Act
            await _processor.OnPublishReceivedAsync(message, availableSequenceNumbers, stringTable);

            // Assert
            _processor.AvailableInRetransmissionQueue.Should().BeEquivalentTo(availableSequenceNumbers);
            _processor.MessageChannel.Reader.Count.Should().Be(1);
        }

        [Fact]
        public void OnPublishStateChangedShouldLogCorrectMessagesAsync()
        {
            // Act
            _processor.OnPublishStateChanged(PublishState.Stopped | PublishState.Recovered);

            // Assert
            _mockLogger.Verify(logger =>
            logger.LogInformation("{Subscription} STOPPED!", _processor), Times.Once);
            _mockLogger.Verify(logger =>
            logger.LogInformation("{Subscription} RECOVERED!", _processor), Times.Once);
        }

        [Fact]
        public async Task ProcessReceivedMessagesAsyncShouldProcessMessagesInOrderAsync()
        {
            // Arrange
            var messages = Enumerable.Range(1, 100).Select(i => new MessageProcessor.IncomingMessage(
                new NotificationMessage
                {
                    SequenceNumber = (uint)i
                },
                new List<string>(), DateTime.UtcNow)).ToList();
            messages.Shuffle();
            foreach (var message in messages)
            {
                await _processor.MessageChannel.Writer.WriteAsync(message);
            }

            // Act
            var processTask = _processor.ProcessReceivedMessagesAsync(
                _processor.CancellationTokenSource.Token);
            _processor.CancellationTokenSource.Cancel();
            await processTask;

            _processor.ReceivedSequenceNumbers.Should().BeEquivalentTo(Enumerable.Range(1, 100).Select(i => (uint)i));
        }

        [Fact]
        public async Task ProcessMessageAsyncShouldRepublishMissingMessagesAsync()
        {
            // Arrange
            var message = new MessageProcessor.IncomingMessage(
                new NotificationMessage
                {
                    SequenceNumber = 3
                },
                new List<string>(), DateTime.UtcNow);
            _processor.LastSequenceNumberProcessed = 1;

            _mockSession
                .Setup(c => c.RepublishAsync(
                    It.IsAny<RequestHeader>(),
                    It.IsAny<uint>(),
                    It.IsAny<uint>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepublishResponse())
                .Verifiable(Times.Once);

            // Act
            await _processor.ProcessMessageAsync(message, CancellationToken.None);

            // Assert
            _mockSession.Verify(session => session.RepublishAsync(null, _processor.Id,
                2, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TryRepublishAsyncShouldLogWarningIfMessageNotInQueueAsync()
        {
            // Arrange
            _processor.AvailableInRetransmissionQueue = new List<uint> { 1, 2, 3 };

            _mockSession
                .Setup(c => c.RepublishAsync(
                    It.IsAny<RequestHeader>(),
                    It.IsAny<uint>(),
                    It.IsAny<uint>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RepublishResponse())
                .Verifiable(Times.Once);

            // Act
            await _processor.TryRepublishAsync(4, 5, CancellationToken.None);

            // Assert
            _mockSession.Verify();
        }

        [Fact]
        public async Task OnNotificationReceivedAsyncShouldDispatchKeepAliveNotificationAsync()
        {
            // Arrange
            var message = new NotificationMessage
            {
                SequenceNumber = 1,
                NotificationData =
                new ExtensionObjectCollection()
            };

            // Act
            await _processor.OnNotificationReceivedAsync(message, PublishState.None,
                CancellationToken.None);

            // Assert
            _processor.KeepAliveNotificationReceived.Should().BeTrue();
        }

        [Fact]
        public async Task DispatchAsyncShouldCallOnDataChangeNotificationAsync()
        {
            // Arrange
            var message = new NotificationMessage
            {
                SequenceNumber = 1,
                PublishTime = DateTime.UtcNow,
                StringTable = new List<string>()
            };
            var dataChangeNotification = new DataChangeNotification();
            var notificationData = new ExtensionObject(dataChangeNotification);

            // Act
            await _processor.DispatchAsync(message, PublishState.None, notificationData);

            // Assert
            _processor.DataChangeNotificationReceived.Should().BeTrue();
        }

        private class TestMessageProcessor : MessageProcessor
        {
            public TestMessageProcessor(ISubscriptionContext session,
                IMessageAckQueue completion, IObservability observability)
                : base(session, completion, observability)
            {
            }

            public Channel<IncomingMessage> MessageChannel => _messages;
            public CancellationTokenSource CancellationTokenSource => _cts;
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

            public bool KeepAliveNotificationReceived { get; private set; }
            public bool DataChangeNotificationReceived { get; private set; }
            public bool EventNotificationReceived { get; private set; }

            public List<uint> ReceivedSequenceNumbers { get; } = new List<uint>();

            protected override ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
                DateTime publishTime, PublishState publishStateMask)
            {
                KeepAliveNotificationReceived = true;
                ReceivedSequenceNumbers.Add(sequenceNumber);
                return ValueTask.CompletedTask;
            }

            protected override ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, DataChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                DataChangeNotificationReceived = true;
                ReceivedSequenceNumbers.Add(sequenceNumber);
                return ValueTask.CompletedTask;
            }

            protected override ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
                DateTime publishTime, EventNotificationList notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                EventNotificationReceived = true;
                ReceivedSequenceNumbers.Add(sequenceNumber);
                return ValueTask.CompletedTask;
            }
        }
    }
}

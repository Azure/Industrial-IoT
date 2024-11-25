// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Processes messages inside a subscription, it is the base
    /// class for all subscription objects but basis to support
    /// better testing in isolation.
    /// </summary>
    public abstract class MessageProcessor : IMessageProcessor
    {
        /// <inheritdoc/>
        public uint Id { get; internal set; }

        /// <summary>
        /// Observability context
        /// </summary>
        protected IObservability Observability { get; }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="session"></param>
        /// <param name="completion"></param>
        /// <param name="observability"></param>
        protected MessageProcessor(ISubscriptionContext session,
            IMessageAckQueue completion, IObservability observability)
        {
            Observability = observability;
            _availableInRetransmissionQueue = Array.Empty<uint>();
            _logger = Observability.LoggerFactory.CreateLogger<SubscriptionBase>();
            _session = session;
            _completion = completion;
            _messages = Channel.CreateUnboundedPrioritized<IncomingMessage>(
                new UnboundedPrioritizedChannelOptions<IncomingMessage>
                {
                    SingleReader = true
                });
            _messageWorkerTask = ProcessReceivedMessagesAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        /// <inheritdoc/>
        public virtual async ValueTask OnPublishReceivedAsync(NotificationMessage message,
            IReadOnlyList<uint>? availableSequenceNumbers,
            IReadOnlyList<string> stringTable)
        {
            if (availableSequenceNumbers != null)
            {
                _availableInRetransmissionQueue = availableSequenceNumbers;
            }
            _lastNotificationTimestamp = Observability.TimeProvider.GetTimestamp();
            await _messages.Writer.WriteAsync(new IncomingMessage(
                message, stringTable, DateTime.UtcNow)).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose subscription
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing && !_disposed)
            {
                try
                {
                    _messages.Writer.TryComplete();
                    _cts.Cancel();

                    await _messageWorkerTask.ConfigureAwait(false);
                }
                finally
                {
                    _cts.Dispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>S
        /// Process status change notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected virtual ValueTask OnStatusChangeNotificationAsync(uint sequenceNumber,
            DateTime publishTime, StatusChangeNotification notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable)
        {
            // TODO
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Process keep alive notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="publishStateMask"></param>
        /// <returns></returns>
        protected abstract ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
            DateTime publishTime, PublishState publishStateMask);

        /// <summary>
        /// Process data change notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected abstract ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
            DateTime publishTime, DataChangeNotification notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable);

        /// <summary>
        /// Process event notification
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="publishTime"></param>
        /// <param name="notification"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        protected abstract ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
            DateTime publishTime, EventNotificationList notification,
            PublishState publishStateMask, IReadOnlyList<string> stringTable);

        /// <summary>
        /// On publish state changed
        /// </summary>
        /// <param name="stateMask"></param>
        /// <returns></returns>
        protected internal virtual void OnPublishStateChanged(PublishState stateMask)
        {
            if (stateMask.HasFlag(PublishState.Stopped))
            {
                _logger.LogInformation("{Subscription} STOPPED!", this);
            }
            if (stateMask.HasFlag(PublishState.Recovered))
            {
                _logger.LogInformation("{Subscription} RECOVERED!", this);
            }
        }

        /// <summary>
        /// Processes all incoming messages and dispatch them.
        /// </summary>
        /// <param name="ct"></param>
        internal async Task ProcessReceivedMessagesAsync(CancellationToken ct)
        {
            try
            {
                //
                // This can be optimized to peek when missing sequence number
                // and not in available sequence number als to support batching.
                // TODO This also needs to guard against overruns using some
                // form of semaphore to block the publisher.
                // Unless we get https://github.com/dotnet/runtime/issues/101292
                //
                var reader = _messages.Reader.ReadAllAsync(ct);
                await foreach (var incoming in reader.ConfigureAwait(false))
                {
                    await ProcessMessageAsync(incoming, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "{Subscription}: Error processing messages. Processor is exiting!!!",
                    this);
                OnPublishStateChanged(PublishState.Stopped);

                // Do not call complete here as we do not want the subscription
                // to be removed
                throw;
            }
            await _completion.CompleteAsync(Id, default).ConfigureAwait(false);
        }

        /// <summary>
        /// Process message. The logic will chewck whether any message was missed
        /// and try to republish. Any new messages received that already were
        /// handled are discarded. The message is then dispatched to the appropriate
        /// callbacks that must be overridden by the derived class.
        /// </summary>
        /// <param name="incoming"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task ProcessMessageAsync(IncomingMessage incoming, CancellationToken ct)
        {
            var prevSeqNum = _lastSequenceNumberProcessed;
            var curSeqNum = incoming.Message.SequenceNumber;
            if (prevSeqNum != 0)
            {
                for (var missing = prevSeqNum + 1; missing < curSeqNum; missing++)
                {
                    // Try to republish missing messages from retransmission queue
                    await TryRepublishAsync(missing, curSeqNum, ct).ConfigureAwait(false);
                }
                if (prevSeqNum >= curSeqNum)
                {
                    // Can occur if we republished a message
                    if (!_logger.IsEnabled(LogLevel.Debug))
                    {
                        return;
                    }
                    if (curSeqNum == prevSeqNum)
                    {
                        _logger.LogDebug("{Subscription}: Received duplicate message " +
                            "with sequence number #{SeqNumber}.", this, curSeqNum);
                    }
                    else
                    {
                        _logger.LogDebug("{Subscription}: Received older message with " +
                            "sequence number #{SeqNumber} but already processed message with " +
                            "sequence number #{Old}.", this, curSeqNum, prevSeqNum);
                    }
                    return;
                }
            }
            _lastSequenceNumberProcessed = curSeqNum;
            await OnNotificationReceivedAsync(incoming.Message, PublishState.None,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Try republish a missing message
        /// </summary>
        /// <param name="missing"></param>
        /// <param name="curSeqNum"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async ValueTask TryRepublishAsync(uint missing, uint curSeqNum,
            CancellationToken ct)
        {
            if (_availableInRetransmissionQueue.Contains(missing))
            {
                _logger.LogWarning("{Subscription}: Message with sequence number " +
                    "#{SeqNumber} is not in server retransmission queue and was dropped.",
                    this, missing);
                return;
            }
            _logger.LogInformation("{Subscription}: Republishing missing message " +
                "with sequence number #{Missing} to catch up to message " +
                "with sequence number #{SeqNumber}...", this, missing, curSeqNum);
            var republish = await _session.RepublishAsync(null, Id, missing,
                ct).ConfigureAwait(false);

            if (ServiceResult.IsGood(republish.ResponseHeader.ServiceResult))
            {
                await OnNotificationReceivedAsync(republish.NotificationMessage,
                    PublishState.Republish, ct).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("{Subscription}: Republishing message with " +
                    "sequence number #{SeqNumber} failed.", this, missing);
            }
        }

        /// <summary>
        /// Dispatch notification message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async ValueTask OnNotificationReceivedAsync(NotificationMessage message,
            PublishState publishStateMask, CancellationToken ct)
        {
            try
            {
                if (message.NotificationData.Count == 0)
                {
                    publishStateMask |= PublishState.KeepAlive;
                    await OnKeepAliveNotificationAsync(message.SequenceNumber,
                        message.PublishTime, publishStateMask).ConfigureAwait(false);
                }
                else
                {
#if PARALLEL_DISPATCH
                    await Task.WhenAll(message.NotificationData.Select(
                        notificationData => DispatchAsync(message, publishStateMask,
                            notificationData))).ConfigureAwait(false);
#else
                    foreach (var notificationData in message.NotificationData)
                    {
                        await DispatchAsync(message, publishStateMask,
                            notificationData).ConfigureAwait(false);
                    }
#endif
                }
                await _completion.QueueAsync(
                    new SubscriptionAcknowledgement
                    {
                        SequenceNumber = message.SequenceNumber,
                        SubscriptionId = Id
                    }, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Subscription}: Error dispatching notification data.", this);
            }
        }

        /// <summary>
        /// Dispatch notification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="publishStateMask"></param>
        /// <param name="notificationData"></param>
        /// <returns></returns>
        internal async ValueTask DispatchAsync(NotificationMessage message,
            PublishState publishStateMask, ExtensionObject? notificationData)
        {
            if (notificationData == null)
            {
                return;
            }
            switch (notificationData.Body)
            {
                case DataChangeNotification datachange:
                    await OnDataChangeNotificationAsync(message.SequenceNumber,
                        message.PublishTime, datachange, publishStateMask,
                        message.StringTable).ConfigureAwait(false);
                    break;
                case EventNotificationList events:
                    await OnEventDataNotificationAsync(message.SequenceNumber,
                        message.PublishTime, events, publishStateMask,
                        message.StringTable).ConfigureAwait(false);
                    break;
                case StatusChangeNotification statusChanged:
                    var mask = publishStateMask;
                    if (statusChanged.Status ==
                        StatusCodes.GoodSubscriptionTransferred)
                    {
                        mask |= PublishState.Transferred;
                    }
                    else if (statusChanged.Status == StatusCodes.BadTimeout)
                    {
                        mask |= PublishState.Timeout;
                    }
                    await OnStatusChangeNotificationAsync(message.SequenceNumber,
                        message.PublishTime, statusChanged, mask,
                        message.StringTable).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// A message received from the server cached until is processed or discarded.
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="StringTable"></param>
        /// <param name="Enqueued"></param>
        internal sealed record class IncomingMessage(NotificationMessage Message,
            IReadOnlyList<string> StringTable, DateTime Enqueued) :
            IComparable<IncomingMessage>
        {
            /// <inheritdoc/>
            public int CompareTo(IncomingMessage? other)
            {
                // Greater than zero – This instance follows the next message in the sort order.
                return (int)(Message.SequenceNumber - (other?.Message.SequenceNumber ?? uint.MinValue));
            }
        }

        internal long _lastNotificationTimestamp;
        internal uint _lastSequenceNumberProcessed;
        internal IReadOnlyList<uint> _availableInRetransmissionQueue;
#pragma warning disable IDE1006 // Naming Styles
        internal readonly ILogger _logger;
        internal readonly ISubscriptionContext _session;
        internal readonly IMessageAckQueue _completion;
        internal readonly Task _messageWorkerTask;
        internal readonly CancellationTokenSource _cts = new();
        internal readonly Channel<IncomingMessage> _messages;
#pragma warning restore IDE1006 // Naming Styles
        internal bool _disposed;
    }
}

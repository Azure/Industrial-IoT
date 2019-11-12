// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Triggering {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Triggers pub sub messages
    /// </summary>
    public class PubSubMessageTrigger : IMessageTrigger {

        /// <summary>
        /// Metadata major version
        /// </summary>
        public uint MetadataMajorVersion { get; set; } = 1;

        /// <summary>
        /// Metadata minor version
        /// </summary>
        public uint MetadataMinorVersion { get; set; } = 0;

        /// <inheritdoc/>
        public string Id => Guid.NewGuid().ToString();

        /// <inheritdoc/>
        public long NumberOfConnectionRetries => _subscriptions.Sum(sc => sc.NumberOfConnectionRetries);

        /// <inheritdoc/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Create trigger
        /// </summary>
        /// <param name="pubSubMessageTriggerConfig"></param>
        /// <param name="subscriptionManager"></param>
        /// <param name="logger"></param>
        public PubSubMessageTrigger(IPubSubMessageTriggerConfig pubSubMessageTriggerConfig,
            ISubscriptionManager subscriptionManager, ILogger logger) {
            _subscriptions = new List<ISubscription>();
            _subscriptionManager = subscriptionManager;
            _pubSubMessageTriggerConfig = pubSubMessageTriggerConfig;
            _logger = logger;

            if (_pubSubMessageTriggerConfig.KeyframeMessageInterval.HasValue &&
                _pubSubMessageTriggerConfig.KeyframeMessageInterval.Value > TimeSpan.Zero) {
                _keyframeTimer = new Timer(pubSubMessageTriggerConfig.KeyframeMessageInterval.Value.TotalMilliseconds);
                _keyframeTimer.Elapsed += KeyframeTimerElapsedAsync;
            }

            if (_pubSubMessageTriggerConfig.MetadataMessageInterval.HasValue &&
                _pubSubMessageTriggerConfig.MetadataMessageInterval.Value > TimeSpan.Zero) {
                _metadataTimer = new Timer(pubSubMessageTriggerConfig.MetadataMessageInterval.Value.TotalMilliseconds);
                _metadataTimer.Elapsed += MetadataTimer_Elapsed;
            }
        }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken ct) {
            foreach (var dataSet in _pubSubMessageTriggerConfig.DataSets) {
                var subscription = dataSet.ToSubscriptionInfoModel(_pubSubMessageTriggerConfig);
                var sc = await _subscriptionManager.GetOrCreateSubscriptionAsync(subscription);
                sc.OnSubscriptionMessage += OpcuaSubscriptionClient_MessageReceived;
                await sc.ApplyAsync(subscription.Subscription.MonitoredItems);
                _subscriptions.Add(sc);
            }

            if (_keyframeTimer != null) {
                ct.Register(() => _keyframeTimer.Stop());
                _keyframeTimer.Start();
            }

            if (_metadataTimer != null) {
                ct.Register(() => _metadataTimer.Stop());
                _metadataTimer.Start();
            }

            await Task.Delay(-1, ct); // TODO - add managemnt of monitored items, etc.

            _subscriptions.ForEach(sc => {
                sc.OnSubscriptionMessage -= OpcuaSubscriptionClient_MessageReceived;
                sc.Dispose(); // TODO
            });
            _subscriptions.Clear();
        }

        /// <summary>
        /// Fire when keyframe timer elapsed to send keyframe message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void KeyframeTimerElapsedAsync(object sender, ElapsedEventArgs e) {
            try {
                _keyframeTimer.Enabled = false;
                foreach (var sc in _subscriptions) {
                    var context = await sc.GetServiceMessageContextAsync();
                    if (context != null) {
                        _logger.Debug("Send keyframe message...");
                        var message = ConstructMessage(sc, sc.LastValues);
                        SendDataMessage(sc, message.YieldReturn(), context);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to send keyframe");
            }
            finally {
                _keyframeTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Fired when metadata time elapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetadataTimer_Elapsed(object sender, ElapsedEventArgs e) {
            // No op?
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OpcuaSubscriptionClient_MessageReceived(object sender, MessageReceivedEventArgs args) {
            if (_pubSubMessageTriggerConfig.SendChangeMessages ?? false) {
                if (!(args.Message.Value is SubscriptionMessage messageTyped)) {
                    throw new InvalidMessageFormatException(
                        "This trigger does only support SubscriptionMessage messages.");
                }
                if (messageTyped.Values.Any()) {
                    var subscription = sender as ISubscription;
                    var dataSetMessage = ConstructMessage(subscription, messageTyped.Values);
                    SendDataMessage(subscription, dataSetMessage.YieldReturn(),
                        messageTyped.ServiceMessageContext);
                }
                else {
                    _logger.Warning("Skipping keyframe message as payload would be empty.");
                }
            }
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private DataSetMessage ConstructMessage(ISubscription subscription,
            Dictionary<string, DataValue> payload) {
            return new DataSetMessage {
                DataSetWriterId = subscription?.Id,
                MetaDataVersion = new ConfigurationVersionDataType {
                    MajorVersion = MetadataMajorVersion,
                    MinorVersion = MetadataMinorVersion
                },
                SequenceNumber = _currentSequenceNumber++,
                Status = StatusCodes.Good, // TODO
                Timestamp = DateTime.UtcNow,
                Payload = new DataSet(payload)
            };
        }
        /// <summary>
        /// Send a data network message
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="messages"></param>
        /// <param name="context"></param>
        private void SendDataMessage(ISubscription subscription,
            IEnumerable<DataSetMessage> messages, ServiceMessageContext context) {
            if (!messages.Any()) {
                return;
            }

            var networkMessage = new NetworkMessage {
                SubscriptionId = subscription?.Id,
                MessageContext = context,
                MessageId = Guid.NewGuid().ToString(), // TODO
                MessageType = "ua-data",
                Messages = new List<DataSetMessage>(),
                PublisherId = "TODO" // TODO
            };

            networkMessage.Messages.AddRange(messages);
            var md = new MessageData<NetworkMessage>(networkMessage.MessageId, networkMessage);
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(md));
        }

        private readonly Timer _keyframeTimer;
        private readonly ILogger _logger;
        private readonly Timer _metadataTimer;
        private readonly IPubSubMessageTriggerConfig _pubSubMessageTriggerConfig;
        private readonly List<ISubscription> _subscriptions;
        private readonly ISubscriptionManager _subscriptionManager;
        private uint _currentSequenceNumber;
    }
}
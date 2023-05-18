// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttTestValidator.BusinessLogic
{
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Connecting;
    using MQTTnet.Client.Disconnecting;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Receiving;
    using MQTTnet.Client.Subscribing;
    using MQTTnet.Formatter;
    using MqttTestValidator.Interfaces;
    using MqttTestValidator.Models;
    using System.Text;
    using System.Text.RegularExpressions;

    internal sealed class VerificationTask : IVerificationTask, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler, IMqttApplicationMessageReceivedHandler
    {
        private readonly string _mqttBroker;
        private readonly int _mqttPort;
        private readonly string _mqttTopic;
        private readonly TimeSpan _startUpDelay;
        private readonly TimeSpan _observationTime;
        private readonly ILogger<IVerificationTask> _logger;
        private readonly string _clientId;
        private readonly Regex _messageIdRegEx = new Regex(".*?(\"MessageId\":).(\\d*)?", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
        private MqttVerificationDetailedResponse _result;
        private static object _lock = new object();
        private long _messageCounter;
        private long _lowestMessageId;
        private long _highestMessageId;
        private IMqttClient? _mqttClient;


        public VerificationTask(ulong id, string mqttBroker, int mqttPort, string mqttTopic, TimeSpan startUpDelay, TimeSpan observationTime, ILogger<IVerificationTask> logger)
        {
            Id = id;
            _mqttBroker = mqttBroker;
            _mqttPort = mqttPort;
            _mqttTopic = mqttTopic;
            _startUpDelay = startUpDelay;
            _observationTime = observationTime;
            _logger = logger;

            _result = new MqttVerificationDetailedResponse
            {
                IsFinished = false
            };
            _messageCounter = 0;
            _lowestMessageId = long.MaxValue;
            _highestMessageId = 0;
            _clientId = $"validator_{Id}";
        }

        /// <inheritdoc />
        public ulong Id { get; set; }

        /// <inheritdoc />
        public void Start()
        {
            var timeBuffer = TimeSpan.FromMinutes(1);
            using var cts = new CancellationTokenSource(_startUpDelay + _observationTime + timeBuffer);
            var unítOfWork = Task.Delay(_startUpDelay, cts.Token)
            .ContinueWith(t =>
            {
                var mqttFactory = new MqttFactory();

                using (var mqttClient = mqttFactory.CreateMqttClient())
                {
                    _mqttClient = mqttClient;

                    mqttClient.ConnectedHandler = this;
                    mqttClient.DisconnectedHandler = this;
                    mqttClient.ApplicationMessageReceivedHandler = this;

                    var mqttClientOptions = GetMqttClientOptions();

                    _logger.LogInformation("Connecting to {MqttBroker} on port {MqttPort} with {ClientId} as clean session", _mqttBroker, _mqttPort, _clientId);
                    var connectionResult = mqttClient.ConnectAsync(mqttClientOptions, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (connectionResult.ResultCode != MqttClientConnectResultCode.Success)
                    {
                        _logger.LogError("Can't connect to MQTT broker: {ReasonString}: {ResultCode}", connectionResult.ReasonString, connectionResult.ResultCode);
                        throw new InvalidProgramException("Can't connect to MQTT Broker");
                    }
                    _logger.LogInformation("Connected!");

                    var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f => { f.WithTopic(_mqttTopic).WithAtLeastOnceQoS(); })
                        .Build();

                    _logger.LogInformation("Subscribing to topic {MqttTopic} with QoS 1", _mqttTopic);
                    var subscriptionResult = mqttClient.SubscribeAsync(mqttSubscribeOptions, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (subscriptionResult.Items[0].ResultCode != MqttClientSubscribeResultCode.GrantedQoS1)
                    {
                        _logger.LogError("Can't subscribe to topic: {ResultCode}", subscriptionResult.Items[0].ResultCode);
                        throw new InvalidProgramException("Can't subscribe to topic");
                    }
                    _logger.LogInformation("Subscribed");

                    Task.Delay(_observationTime).ConfigureAwait(false).GetAwaiter().GetResult();
                    _mqttClient = null;
                }
            }, cts.Token)
            .ContinueWith(t =>
            {
                lock (_lock)
                {
                    _result.IsFinished = true;
                    _result.HasFailed = !t.IsCompletedSuccessfully;
                    if (t.Exception != null)
                    {
                        _result.Error = t.Exception.Flatten().Message;
                    }
                    _result.NumberOfMessages = _messageCounter;
                    _result.LowestMessageId = _lowestMessageId;
                    _result.HighestMessageId = _highestMessageId;
                }
            });
        }

        /// <inheritdoc />
        public MqttVerificationDetailedResponse GetResult()
        {
            lock (_lock)
            {
                return _result;
            }
        }

        Task IMqttClientConnectedHandler.HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogInformation("Connected to MQTT Broker ({MqttBroker} on {MqttPort}): {ReasonString}:{ResultCode}",
                 _mqttBroker, _mqttPort, eventArgs.ConnectResult.ReasonString, eventArgs.ConnectResult.ResultCode);
            return Task.CompletedTask;
        }

        async Task IMqttClientDisconnectedHandler.HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _logger.LogInformation("Disconnected from MQTT Broker ({MqttBroker} on {MqttPort}): {Reason}",
                _mqttBroker, _mqttPort, eventArgs.Reason);
            if (_mqttClient == null)
            {
                return;
            }
            ushort numberOfRetries = 3;
            ushort maxNumberOfRetries = 3;
            ushort backoffTimeInMilliseconds = 1_000;
            ushort currentRetries = 0;

            var clientOptions = GetMqttClientOptions();
            while (!_mqttClient.IsConnected && numberOfRetries > 0)
            {
                _logger.LogInformation("Reconnecting to MQTT Broker {CurrentRetries} of {MaxNumberOfRetries} with backoff: {BackoffTimeInMilliseconds}",
                    ++currentRetries, maxNumberOfRetries, backoffTimeInMilliseconds);

                await Task.Delay(backoffTimeInMilliseconds).ConfigureAwait(false);

                var result = await _mqttClient.ConnectAsync(clientOptions).ConfigureAwait(false);
                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    _logger.LogError("Failed to reconnect to MQTT Broker, {ReasonString}: {ResultCode}", result.ReasonString, result.ResultCode);
                    numberOfRetries--;
                }
                else
                {
                    _logger.LogInformation("MQTT Client reconnected!");
                    break;
                }
            }
        }

        Task IMqttApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {

            eventArgs.AutoAcknowledge = true;
            Interlocked.Increment(ref _messageCounter);

            try
            {
                var message = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
                _logger.LogTrace("{Message}", message);

                var matches = _messageIdRegEx.Matches(message);
                foreach (Match match in matches.Where(m => m.Success))
                {
                    if (long.TryParse(match.Groups[2].Value, out var messageId))
                    {
                        if (messageId > _highestMessageId)
                        {
                            _highestMessageId = messageId;
                        }

                        if (messageId < _lowestMessageId)
                        {
                            _lowestMessageId = messageId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing MQTT message.");
                throw new AggregateException("Internal error processing MQTT message", ex);
            }

            return Task.CompletedTask;
        }

        private IMqttClientOptions GetMqttClientOptions()
        {
            return new MqttClientOptionsBuilder()
                        .WithTcpServer(_mqttBroker, _mqttPort)
                        .WithCleanSession(true)
                        .WithProtocolVersion(MqttProtocolVersion.V500)
                        .WithClientId(_clientId)
                        .Build();
        }
    }
}

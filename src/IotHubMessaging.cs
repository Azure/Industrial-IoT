
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Publisher
{
    using static Opc.Ua.Workarounds.TraceWorkaround;
    using static Program;

    public class IotHubMessaging
    {
        private class OpcUaMessage
        {
            public string ApplicationUri { get; set; }
            public string DisplayName { get; set; }
            public string NodeId { get; set; }
            public OpcUaValue Value { get; set; }
        }

        private class OpcUaValue
        {
            public string Value { get; set; }
            public string SourceTimestamp { get; set; }
        }

        private ConcurrentQueue<string> _sendQueue;
        private int _currentSizeOfIotHubMessageBytes;
        private List<OpcUaMessage> _messageList;
        private uint _maxSizeOfIoTHubMessageBytes;
        private int _defaultSendIntervalSeconds;
        private CancellationTokenSource _tokenSource;
        private Task _dequeueAndSendTask;


        public IotHubMessaging(uint maxSizeOfIoTHubMessageBytes, int defaultSendIntervalSeconds)
        {
            _maxSizeOfIoTHubMessageBytes = maxSizeOfIoTHubMessageBytes;
            _defaultSendIntervalSeconds = defaultSendIntervalSeconds;
            _sendQueue = new ConcurrentQueue<string>();
            _messageList = new List<OpcUaMessage>();
            _currentSizeOfIotHubMessageBytes = 0;

            // start up task to send telemetry to IoTHub.
            _dequeueAndSendTask = null;
            _tokenSource = new CancellationTokenSource();

            Trace("Creating task to send OPC UA messages in batches to IoT Hub...");
            try
            {
                _dequeueAndSendTask = Task.Run(() => DeQueueMessagesAsync(_tokenSource.Token), _tokenSource.Token);
            }
            catch (Exception e)
            {
                Trace("Exception: " + e.ToString());
            }
        }

        public void Shutdown()
        {
            // send cancellation token and wait for last IoT Hub message to be sent.
            try
            {
                _tokenSource.Cancel();
                _dequeueAndSendTask.Wait();
            }
            catch (Exception e)
            {
                Trace(e, "Failure while shutting down IoTHub messaging.");
            }
        }

        //
        // Enqueue a message.
        //
        public void Enqueue(string json)
        {
            _sendQueue.Enqueue(json);
        }

        /// <summary>
        /// Dequeue messages
        /// </summary>
        private async Task DeQueueMessagesAsync(CancellationToken ct)
        {
            try
            {
                Timer sendTimer = null;
                if (_defaultSendIntervalSeconds > 0)
                {
                    // send every x seconds, regardless if IoT Hub message is full. 
                    sendTimer = new Timer(async state => await SendToIoTHubAsync(), null, 0, _defaultSendIntervalSeconds * 1000);
                }

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Trace($"Cancellation requested. Sending {_sendQueue.Count} remaining messages.");
                        if (sendTimer != null)
                        {
                            sendTimer.Dispose();
                        }
                        await SendToIoTHubAsync();
                        break;
                    }


                    if (_sendQueue.Count > 0)
                    {
                        bool isPeekSuccessful = false;
                        bool isDequeueSuccessful = false;
                        string messageInJson = string.Empty;
                        int nextMessageSizeBytes = 0;

                        // perform a TryPeek to determine size of next message 
                        // and whether it will fit. If so, dequeue message and add it to the list. 
                        // if it cannot fit, send current message to IoTHub, reset it, and repeat.

                        isPeekSuccessful = _sendQueue.TryPeek(out messageInJson);

                        // get size of next message in the queue
                        if (isPeekSuccessful)
                        {
                            nextMessageSizeBytes = Encoding.UTF8.GetByteCount(messageInJson);
                        }

                        // determine if it will fit into remaining space of the IoTHub message. 
                        // if so, dequeue it
                        if (_currentSizeOfIotHubMessageBytes + nextMessageSizeBytes < _maxSizeOfIoTHubMessageBytes)
                        {
                            isDequeueSuccessful = _sendQueue.TryDequeue(out messageInJson);

                            // add dequeued message to list
                            if (isDequeueSuccessful)
                            {
                                OpcUaMessage msgPayload = JsonConvert.DeserializeObject<OpcUaMessage>(messageInJson);

                                _messageList.Add(msgPayload);

                                _currentSizeOfIotHubMessageBytes = _currentSizeOfIotHubMessageBytes + nextMessageSizeBytes;

                            }
                        }
                        else
                        {
                            // message is full. send it to IoTHub
                            await SendToIoTHubAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace(e, "Error while dequeuing messages.");
            }

        }

        /// <summary>
        /// Send dequeued messages to IoT Hub
        /// </summary>
        private async Task SendToIoTHubAsync()
        {
            if (_messageList.Count > 0)
            {
                string msgListInJson = JsonConvert.SerializeObject(_messageList);

                var encodedMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(msgListInJson));

                // publish
                encodedMessage.Properties.Add("content-type", "application/opcua+uajson");
                encodedMessage.Properties.Add("deviceName", ApplicationName);

                try
                {
                    if (IotHubClient != null)
                    {
                        await IotHubClient.SendEventAsync(encodedMessage);
                    }
                    else
                    {
                        Trace("No IoTHub client available ");
                    }
                }
                catch (Exception e)
                {
                    Trace(e, "Exception while sending message to IoTHub. Dropping...");
                }

                // reset IoTHub message size
                _currentSizeOfIotHubMessageBytes = 0;
                _messageList.Clear();
            }
        }
    }
}


using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Publisher
{
    using IoTHubCredentialTools;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Client;
    using static Opc.Ua.Workarounds.TraceWorkaround;
    using static Program;

    /// <summary>
    /// Class to handle all IoTHub communication.
    /// </summary>
    public class IotHubMessaging
    {
        /// <summary>
        /// Classes for the telemetry message sent to IoTHub.
        /// </summary>
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
        private SemaphoreSlim _messageListSemaphore;
        private uint _maxSizeOfIoTHubMessageBytes;
        private int _defaultSendIntervalSeconds;
        private CancellationTokenSource _tokenSource;
        private Task _dequeueAndSendTask;
        private Timer _sendTimer;
        private AutoResetEvent _sendQueueEvent;
        private DeviceClient _iotHubClient;

        /// <summary>
        /// Ctor for the class.
        /// </summary>
        public IotHubMessaging()
        {
            _sendQueue = new ConcurrentQueue<string>();
            _sendQueueEvent = new AutoResetEvent(false);
            _messageList = new List<OpcUaMessage>();
            _messageListSemaphore = new SemaphoreSlim(1);
            _currentSizeOfIotHubMessageBytes = 0;
        }

        /// <summary>
        /// Initializes the communication with secrets and details for (batched) send process.
        /// </summary>
        /// <returns></returns>
        public bool Init(string iotHubOwnerConnectionString, uint maxSizeOfIoTHubMessageBytes, int defaultSendIntervalSeconds)
        {
            _maxSizeOfIoTHubMessageBytes = maxSizeOfIoTHubMessageBytes;
            _defaultSendIntervalSeconds = defaultSendIntervalSeconds;

            try
            {
                // check if we also received an owner connection string
                if (string.IsNullOrEmpty(iotHubOwnerConnectionString))
                {
                    Trace("IoT Hub owner connection string not passed as argument.");

                    // check if we have an environment variable to register ourselves with IoT Hub
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
                    {
                        iotHubOwnerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");
                        Trace("IoT Hub owner connection string read from environment.");
                    }
                }

                // register ourselves with IoT Hub
                string deviceConnectionString;
                Trace($"IoTHub device cert store type is: {IotDeviceCertStoreType}");
                Trace($"IoTHub device cert path is: {IotDeviceCertStorePath}");
                if (string.IsNullOrEmpty(iotHubOwnerConnectionString))
                {
                    Trace("IoT Hub owner connection string not specified. Assume device connection string already in cert store.");
                }
                else
                {
                    Trace($"Attempting to register ourselves with IoT Hub using owner connection string: {iotHubOwnerConnectionString}");
                    RegistryManager manager = RegistryManager.CreateFromConnectionString(iotHubOwnerConnectionString);

                    // remove any existing device
                    Device existingDevice = manager.GetDeviceAsync(ApplicationName).Result;
                    if (existingDevice != null)
                    {
                        Trace($"Device '{ApplicationName}' found in IoTHub registry. Remove it.");
                        manager.RemoveDeviceAsync(ApplicationName).Wait();
                    }

                    Trace($"Adding device '{ApplicationName}' to IoTHub registry.");
                    Device newDevice = manager.AddDeviceAsync(new Device(ApplicationName)).Result;
                    if (newDevice != null)
                    {
                        string hostname = iotHubOwnerConnectionString.Substring(0, iotHubOwnerConnectionString.IndexOf(";"));
                        deviceConnectionString = hostname + ";DeviceId=" + ApplicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                        Trace($"Device connection string is: {deviceConnectionString}");
                        Trace($"Adding it to device cert store.");
                        SecureIoTHubToken.Write(ApplicationName, deviceConnectionString, IotDeviceCertStoreType, IotDeviceCertStoreType);
                    }
                    else
                    {
                        Trace($"Could not register ourselves with IoT Hub using owner connection string: {iotHubOwnerConnectionString}");
                        Trace("exiting...");
                        return false;
                    }
                }

                // try to read connection string from secure store and open IoTHub client
                Trace($"Attempting to read device connection string from cert store using subject name: {ApplicationName}");
                deviceConnectionString = SecureIoTHubToken.Read(ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath);
                if (!string.IsNullOrEmpty(deviceConnectionString))
                {
                    Trace($"Create Publisher IoTHub client with device connection string: '{deviceConnectionString}' using '{IotHubProtocol}' for communication.");
                    _iotHubClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, IotHubProtocol);
                    _iotHubClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
                    _iotHubClient.OpenAsync().Wait();
                }
                else
                {
                    Trace("Device connection string not found in secure store. Could not connect to IoTHub.");
                    Trace("exiting...");
                    return false;
                }

                // start up task to send telemetry to IoTHub.
                _dequeueAndSendTask = null;
                _tokenSource = new CancellationTokenSource();

                Trace("Creating task to send OPC UA messages in batches to IoT Hub...");
                _dequeueAndSendTask = Task.Run(() => DeQueueMessagesAsync(_tokenSource.Token), _tokenSource.Token);
            }
            catch (Exception e)
            {
                Trace(e, "Error during IoTHub messaging initialization.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method to write the IoTHub owner connection string into the cert store. 
        /// </summary>
        /// <param name="iotHubOwnerConnectionString"></param>
        public void ConnectionStringWrite(string iotHubOwnerConnectionString)
        {
            DeviceClient newClient = DeviceClient.CreateFromConnectionString(iotHubOwnerConnectionString, IotHubProtocol);
            newClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
            newClient.OpenAsync().Wait();
            SecureIoTHubToken.Write(OpcConfiguration.ApplicationName, iotHubOwnerConnectionString, IotDeviceCertStoreType, IotDeviceCertStorePath);
            _iotHubClient = newClient;
        }

        /// <summary>
        /// Shuts down the IoTHub communication.
        /// </summary>
        public void Shutdown()
        {
            // send cancellation token and wait for last IoT Hub message to be sent.
            try
            {
                _tokenSource.Cancel();
                _dequeueAndSendTask.Wait();

                if (_iotHubClient != null)
                {
                    _iotHubClient.CloseAsync().Wait();
                }
                if (_sendTimer != null)
                {
                    _sendTimer.Dispose();
                }
                if (_sendQueueEvent != null)
                {
                    _sendQueueEvent.Dispose();
                }
            }
            catch (Exception e)
            {
                Trace(e, "Failure while shutting down IoTHub messaging.");
            }
        }

        //
        // Enqueue a message for batch send.
        //
        public void Enqueue(string json)
        {
            _sendQueue.Enqueue(json);
            _sendQueueEvent.Set();
        }

        /// <summary>
        /// Dequeue telemetry messages, compose them for batch send (if needed) and prepares them for sending to IoTHub.
        /// </summary>
        private async Task DeQueueMessagesAsync(CancellationToken ct)
        {
            try
            {
                if (_defaultSendIntervalSeconds > 0 && _maxSizeOfIoTHubMessageBytes > 0)
                {
                    // send every x seconds
                    Trace($"Start timer to send data to IoTHub in {_defaultSendIntervalSeconds} seconds.");
                    _sendTimer = new Timer(async state => await SendToIoTHubAsync(), null, TimeSpan.FromSeconds(_defaultSendIntervalSeconds), TimeSpan.FromSeconds(_defaultSendIntervalSeconds));
                }

                WaitHandle[] waitHandles = { _sendQueueEvent, ct.WaitHandle };
                while (true)
                {
                    // wait till some work needs to be done
                    WaitHandle.WaitAny(waitHandles);

                    // do we need to stop
                    if (ct.IsCancellationRequested)
                    {
                        Trace($"Cancellation requested. Sending {_sendQueue.Count} remaining messages.");
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

                            // sanity check that the user has set a large enough IoTHub messages size.
                            if (nextMessageSizeBytes > _maxSizeOfIoTHubMessageBytes && _maxSizeOfIoTHubMessageBytes > 0)
                            {
                                Trace(Utils.TraceMasks.Error, $"There is a telemetry message (size: {nextMessageSizeBytes}), which will not fit into an IoTHub message (max size: {_maxSizeOfIoTHubMessageBytes}].");
                                Trace(Utils.TraceMasks.Error, $"Please check your IoTHub message size settings. The telemetry message will be discarded silently. Sorry:(");
                                _sendQueue.TryDequeue(out messageInJson);
                                continue;
                            }
                        }

                        // determine if it will fit into remaining space of the IoTHub message or if we do not batch at all
                        // if so, dequeue it
                        if (_currentSizeOfIotHubMessageBytes + nextMessageSizeBytes < _maxSizeOfIoTHubMessageBytes || _maxSizeOfIoTHubMessageBytes == 0)
                        {
                            isDequeueSuccessful = _sendQueue.TryDequeue(out messageInJson);

                            // add dequeued message to list
                            if (isDequeueSuccessful)
                            {
                                OpcUaMessage msgPayload = JsonConvert.DeserializeObject<OpcUaMessage>(messageInJson);
                                _messageListSemaphore.Wait();
                                _messageList.Add(msgPayload);
                                _messageListSemaphore.Release();
                                _currentSizeOfIotHubMessageBytes = _currentSizeOfIotHubMessageBytes + nextMessageSizeBytes;
                                Trace(Utils.TraceMasks.OperationDetail, $"Added new message with size {nextMessageSizeBytes} to IoTHub message (size is now {_currentSizeOfIotHubMessageBytes}). {_sendQueue.Count} message(s) in send queue.");

                                // fall through, if we should send immediately
                                if (_maxSizeOfIoTHubMessageBytes != 0)
                                {
                                    continue;
                                }
                            }
                        }

                        // the message needs to be sent now.
                        Trace(Utils.TraceMasks.OperationDetail, $"IoTHub message complete. Trigger send of message with size {_currentSizeOfIotHubMessageBytes} to IoTHub.");
                        await SendToIoTHubAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Trace(e, "Error while dequeuing messages.");
            }
        }

        /// <summary>
        /// Send messages to IoT Hub
        /// </summary>
        private async Task SendToIoTHubAsync()
        {
            if (_messageList.Count > 0)
            {
                // process all queued messages
                _messageListSemaphore.Wait();
                string msgListInJson = JsonConvert.SerializeObject(_messageList);
                var encodedMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(msgListInJson));
                _currentSizeOfIotHubMessageBytes = 0;
                _messageList.Clear();
                _messageListSemaphore.Release();

                // publish
                encodedMessage.Properties.Add("content-type", "application/opcua+uajson");
                encodedMessage.Properties.Add("deviceName", ApplicationName);

                try
                {
                    if (_iotHubClient != null)
                    {
                        Trace(Utils.TraceMasks.OperationDetail, "Send data to IoTHub.");
                        await _iotHubClient.SendEventAsync(encodedMessage);
                    }
                    else
                    {
                        Trace("No IoTHub client available. Dropping messages...");
                    }
                }
                catch (Exception e)
                {
                    Trace(e, "Exception while sending message to IoTHub. Dropping messages...");
                }
            }

            // Restart timer
            if (_sendTimer != null)
            {
                // send in x seconds
                Trace(Utils.TraceMasks.OperationDetail, $"Retart timer to send data to IoTHub in {_defaultSendIntervalSeconds} second(s).");
                _sendTimer.Change(TimeSpan.FromSeconds(_defaultSendIntervalSeconds), TimeSpan.FromSeconds(_defaultSendIntervalSeconds));
            }
        }
    }
}

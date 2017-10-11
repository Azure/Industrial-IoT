
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using IoTHubCredentialTools;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Client;
    using Opc.Ua;
    using static Opc.Ua.CertificateStoreType;
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static OpcStackConfiguration;

    /// <summary>
    /// Class to handle all IoTHub communication.
    /// </summary>
    public class IotHubMessaging
    {
        public static string IotHubOwnerConnectionString
        {
            get => _iotHubOwnerConnectionString;
            set => _iotHubOwnerConnectionString = value;
        }
        private static string _iotHubOwnerConnectionString = string.Empty;

        public static Microsoft.Azure.Devices.Client.TransportType IotHubProtocol
        {
            get => _iotHubProtocol;
            set => _iotHubProtocol = value;
        }
        private static Microsoft.Azure.Devices.Client.TransportType _iotHubProtocol = Microsoft.Azure.Devices.Client.TransportType.Mqtt;

        public static uint MaxSizeOfIoTHubMessageBytes
        {
            get => _maxSizeOfIoTHubMessageBytes;
            set => _maxSizeOfIoTHubMessageBytes = value;
        }
        private static uint _maxSizeOfIoTHubMessageBytes = 4096;

        public static int DefaultSendIntervalSeconds
        {
            get => _defaultSendIntervalSeconds;
            set => _defaultSendIntervalSeconds = value;
        }
        private static int _defaultSendIntervalSeconds = 1;

        public static string IotDeviceCertStoreType
        {
            get => _iotDeviceCertStoreType;
            set => _iotDeviceCertStoreType = value;
        }
        private static string _iotDeviceCertStoreType = X509Store;

        public static string IotDeviceCertDirectoryStorePathDefault => "CertificateStores/IoTHub";
        public static string IotDeviceCertX509StorePathDefault => "My";
        public static string IotDeviceCertStorePath
        {
            get => _iotDeviceCertStorePath;
            set => _iotDeviceCertStorePath = value;
        }
        private static string _iotDeviceCertStorePath = IotDeviceCertX509StorePathDefault;

        /// <summary>
        /// Classes for the telemetry message sent to IoTHub.
        /// </summary>
        private class OpcUaMessage
        {
            public string ApplicationUri;
            public string DisplayName;
            public string NodeId;
            public OpcUaValue Value;
        }

        private class OpcUaValue
        {
            public string Value;
            public string SourceTimestamp;
        }

        private ConcurrentQueue<string> _sendQueue;
        private int _currentSizeOfIotHubMessageBytes;
        private List<OpcUaMessage> _messageList;
        private SemaphoreSlim _messageListSemaphore;
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
        public async Task<bool> InitAsync()
        {
            try
            {
                // check if we also received an owner connection string
                if (string.IsNullOrEmpty(_iotHubOwnerConnectionString))
                {
                    Trace("IoT Hub owner connection string not passed as argument.");

                    // check if we have an environment variable to register ourselves with IoT Hub
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
                    {
                        _iotHubOwnerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");
                        Trace("IoT Hub owner connection string read from environment.");
                    }
                }

                // register ourselves with IoT Hub
                string deviceConnectionString;
                Trace($"IoTHub device cert store type is: {IotDeviceCertStoreType}");
                Trace($"IoTHub device cert path is: {IotDeviceCertStorePath}");
                if (string.IsNullOrEmpty(_iotHubOwnerConnectionString))
                {
                    Trace("IoT Hub owner connection string not specified. Assume device connection string already in cert store.");
                }
                else
                {
                    Trace($"Attempting to register ourselves with IoT Hub using owner connection string: {_iotHubOwnerConnectionString}");
                    RegistryManager manager = RegistryManager.CreateFromConnectionString(_iotHubOwnerConnectionString);

                    // remove any existing device
                    Device existingDevice = await manager.GetDeviceAsync(ApplicationName);
                    if (existingDevice != null)
                    {
                        Trace($"Device '{ApplicationName}' found in IoTHub registry. Remove it.");
                        await manager.RemoveDeviceAsync(ApplicationName);
                    }

                    Trace($"Adding device '{ApplicationName}' to IoTHub registry.");
                    Device newDevice = await manager.AddDeviceAsync(new Device(ApplicationName));
                    if (newDevice != null)
                    {
                        string hostname = _iotHubOwnerConnectionString.Substring(0, _iotHubOwnerConnectionString.IndexOf(";"));
                        deviceConnectionString = hostname + ";DeviceId=" + ApplicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                        Trace($"Device connection string is: {deviceConnectionString}");
                        Trace($"Adding it to device cert store.");
                        await SecureIoTHubToken.WriteAsync(ApplicationName, deviceConnectionString, IotDeviceCertStoreType, IotDeviceCertStorePath);
                    }
                    else
                    {
                        Trace($"Could not register ourselves with IoT Hub using owner connection string: {_iotHubOwnerConnectionString}");
                        Trace("exiting...");
                        return false;

                    }
                }

                // try to read connection string from secure store and open IoTHub client
                Trace($"Attempting to read device connection string from cert store using subject name: {ApplicationName}");
                deviceConnectionString = await SecureIoTHubToken.ReadAsync(ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath);
                if (!string.IsNullOrEmpty(deviceConnectionString))
                {
                    Trace($"Create Publisher IoTHub client with device connection string: '{deviceConnectionString}' using '{IotHubProtocol}' for communication.");
                    _iotHubClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, IotHubProtocol);
                    ExponentialBackoff exponentialRetryPolicy = new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5000), TimeSpan.FromMilliseconds(100));
                    _iotHubClient.SetRetryPolicy(exponentialRetryPolicy);
                    await _iotHubClient.OpenAsync();
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
                Trace(e, $"Error in InitAsync. (message: {e.Message})");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Method to write the IoTHub owner connection string into the cert store. 
        /// </summary>
        public async Task ConnectionStringWriteAsync(string iotHubOwnerConnectionString)
        {
            DeviceClient newClient = DeviceClient.CreateFromConnectionString(iotHubOwnerConnectionString, IotHubProtocol);
            await newClient.OpenAsync();
            await SecureIoTHubToken.WriteAsync(PublisherOpcApplicationConfiguration.ApplicationName, iotHubOwnerConnectionString, IotDeviceCertStoreType, IotDeviceCertStorePath);
            _iotHubClient = newClient;
        }

        /// <summary>
        /// Shuts down the IoTHub communication.
        /// </summary>
        public async Task Shutdown()
        {
            // send cancellation token and wait for last IoT Hub message to be sent.
            try
            {
                _tokenSource.Cancel();
                await _dequeueAndSendTask;

                if (_iotHubClient != null)
                {
                    await _iotHubClient.CloseAsync();
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
                                await _messageListSemaphore.WaitAsync();
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
                await _messageListSemaphore.WaitAsync();
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
                Trace(Utils.TraceMasks.OperationDetail, $"Restart timer to send data to IoTHub in {_defaultSendIntervalSeconds} second(s).");
                _sendTimer.Change(TimeSpan.FromSeconds(_defaultSendIntervalSeconds), TimeSpan.FromSeconds(_defaultSendIntervalSeconds));
            }
        }
    }
}

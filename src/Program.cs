
using IoTHubCredentialTools;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Mono.Options;
using Newtonsoft.Json;
using Opc.Ua.Client;
using Publisher;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace Opc.Ua.Publisher
{
    using static Opc.Ua.CertificateStoreType;
    using static Opc.Ua.Workarounds.TraceWorkaround;
    using static System.Console;

    public class Program
    {
        public static ApplicationConfiguration m_configuration = null;
        public static List<Session> m_sessions = new List<Session>();
        public static PublishedNodesCollection m_nodesLookups = new PublishedNodesCollection();
        public static List<Uri> m_endpointUrls = new List<Uri>();
        public static string ApplicationName { get; set; }
        public static DeviceClient m_deviceClient = null;

        public static string IoTHubOwnerConnectionString { get; set; }
        public static string LogFileName { get; set; }
        public static ushort PublisherServerPort { get; set; } = 62222;
        public static string PublisherServerPath { get; set; } = "/UA/Publisher";
        public static int LdsRegistrationInterval { get; set; } = 0;
        public static int OpcOperationTimeout { get; set; } = 120000;
        public static bool TrustMyself { get; set; } = true;
        public static int OpcStackTraceMask { get; set; } = Utils.TraceMasks.Error | Utils.TraceMasks.Security | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop | Utils.TraceMasks.Information;
        public static string PublisherServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

        public static string OpcOwnCertStoreType { get; set; } = X509Store;
        private const string _opcOwnCertDirectoryStorePathDefault = "CertificateStores/own";
        private const string _opcOwnCertX509StorePathDefault = "CurrentUser\\UA_MachineDefault";
        public static string OpcOwnCertStorePath { get; set; } = _opcOwnCertX509StorePathDefault;

        public static string OpcTrustedCertStoreType { get; set; } = Directory;
        public static string OpcTrustedCertDirectoryStorePathDefault = "CertificateStores/UA Applications";
        public static string OpcTrustedCertX509StorePathDefault = "CurrentUser\\UA_MachineDefault";
        public static string OpcTrustedCertStorePath { get; set; } = null;

        public static string OpcRejectedCertStoreType { get; set; } = Directory;
        private const string _opcRejectedCertDirectoryStorePathDefault = "CertificateStores/Rejected Certificates";
        private const string _opcRejectedCertX509StorePathDefault = "CurrentUser\\UA_MachineDefault";
        public static string OpcRejectedCertStorePath { get; set; } = _opcRejectedCertDirectoryStorePathDefault;

        public static string OpcIssuerCertStoreType { get; set; } = Directory;
        private const string _opcIssuerCertDirectoryStorePathDefault = "CertificateStores/UA Certificate Authorities";
        private const string _opcIssuerCertX509StorePathDefault = "CurrentUser\\UA_MachineDefault";
        public static string OpcIssuerCertStorePath { get; set; } = _opcIssuerCertDirectoryStorePathDefault;

        public static string IotDeviceCertStoreType { get; set; } = X509Store;
        private const string _iotDeviceCertDirectoryStorePathDefault = "CertificateStores/IoTHub";
        private const string _iotDeviceCertX509StorePathDefault = "IoTHub";
        public static string IotDeviceCertStorePath { get; set; } = _iotDeviceCertX509StorePathDefault;

        public static string PublishedNodesAbsFilenameDefault = $"{System.IO.Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}publishednodes.json";
        public static string PublishedNodesAbsFilename { get; set; }


        private static PublisherServer _publishingServer = new PublisherServer();
        public static Microsoft.Azure.Devices.Client.TransportType IotHubProtocol { get; set; } = Microsoft.Azure.Devices.Client.TransportType.Mqtt;

        private static void Usage(Mono.Options.OptionSet options)
        private static ConcurrentQueue<string> m_sendQueue = new ConcurrentQueue<string>();
        private const uint m_maxSizeOfIoTHubMessageBytes = 4096;
        private const int m_defaultSendIntervalInMilliSeconds = 1000;
        private static int m_currentSizeOfIoTHubMessageBytes = 0;
        private static List<OpcUaMessage> m_messageList = new List<OpcUaMessage>();
        private static PublisherServer m_server = new PublisherServer();

        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message, params object[] args)
        {

            // show usage
            WriteLine();
            WriteLine("Usage: {0}.exe applicationname [iothubconnectionstring] [options]", Assembly.GetEntryAssembly().GetName().Name);
            WriteLine();
            WriteLine("OPC Edge Publisher to subscribe to configured OPC UA servers and send telemetry to Azure IoTHub.");
            WriteLine();
            WriteLine("applicationname: the OPC UA application name to use, required");
            WriteLine("                 The application name is also used to register the publisher under this name in the");
            WriteLine("                 IoTHub device registry.");
            WriteLine();
            WriteLine("iothubconnectionstring: the IoTHub owner connectionstring, optional");
            WriteLine();
            WriteLine("There are a couple of environemnt variables which could be used to control the application:");
            WriteLine("_HUB_CS: sets the IoTHub owner connectionstring");
            WriteLine("_GW_LOGP: sets the filename of the log file to use"); 
            WriteLine("_TPC_SP: sets the path to store certificates of trusted stations");
            WriteLine("_GW_PNFP: sets the filename of the publishing configuration file");
            WriteLine();
            WriteLine("Notes:");
            WriteLine("If an environment variable is controlling the OPC UA stack configuration, they are only taken into account");
            WriteLine("if they are not set in the OPC UA configuration file.");
            WriteLine("Command line arguments overrule OPC UA configuration file settings and environement variable settings.");
            WriteLine();
            
            // output the options
            WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        public static void Main(string[] args)
        {
            var opcTraceInitialized = false;
            try
            {
                var shouldShowHelp = false;

                // these are the available options, not that they set the variables
                Mono.Options.OptionSet options = new Mono.Options.OptionSet {
                    // Publishing configuration options
                    { "pf|publishfile=", $"the filename to configure the nodes to publish.\nDefault: '{PublishedNodesAbsFilenameDefault}'", (string p) => PublishedNodesAbsFilename = p },

                    // IoTHub specific options
                    { "ih|iothubprotocol=", $"the protocol to use for communication with Azure IoTHub (allowed values: {string.Join(", ", Enum.GetNames(IotHubProtocol.GetType()))}).\nDefault: {Enum.GetName(IotHubProtocol.GetType(), IotHubProtocol)}",
                        (Microsoft.Azure.Devices.Client.TransportType p) => IotHubProtocol = p
                    },

                    // opc server configuration options
                    { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './logs/<applicationname>.log.txt'", (string l) => LogFileName = l },
                    { "pn|portnum=", $"the server port of the publisher OPC server endpoint.\nDefault: {PublisherServerPort}", (ushort p) => PublisherServerPort = p },
                    { "pa|path=", $"the enpoint URL path part of the publisher OPC server endpoint.\nDefault: '{PublisherServerPath}'", (string a) => PublisherServerPath = a },
                    { "lr|ldsreginterval=", $"the LDS(-ME) registration interval in ms.\nDefault: {LdsRegistrationInterval}", (int i) => LdsRegistrationInterval = i },
                    { "ot|operationtimeout=", $"the operation timeout of the publisher OPC UA client in ms.\nDefault: {OpcOperationTimeout}", (int i) => OpcOperationTimeout = i },
                    { "st|opcstacktracemask=", $"the trace mask for the OPC stack. See github OPC .NET stack for definitions.\n(Information is enforced)\nDefault: 0x{OpcStackTraceMask:X}", (int i) => OpcStackTraceMask = i },

                    // trust own public cert option
                    { "tm|trustmyself=", $"the publisher certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", (bool b) => TrustMyself = b },

                    // own cert store options
                    { "at|appcertstoretype=", $"the own application cert store type. \n(allowed values: Directory, X509Store)\nDefault: '{OpcOwnCertStoreType}'", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcOwnCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "ap|appcertstorepath=", $"the path where the own application cert should be stored\nDefault (depends on store type):\n" +
                            $"X509Store: '{_opcOwnCertX509StorePathDefault}'\n" +
                            $"Directory: '{_opcOwnCertDirectoryStorePathDefault}'", (string s) => OpcOwnCertStorePath = s
                    },

                    // trusted cert store options
                    {
                    "tt|trustedcertstoretype=", $"the trusted cert store type. \n(allowed values: Directory, X509Store)\nDefault: {OpcTrustedCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcTrustedCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "tp|trustedcertstorepath=", $"the path of the trusted cert store\nDefault (depends on store type):\n" +
                            $"X509Store: '{OpcTrustedCertX509StorePathDefault}'\n" +
                            $"Directory: '{OpcTrustedCertDirectoryStorePathDefault}'", (string s) => OpcTrustedCertStorePath = s
                    },

                    // rejected cert store options
                    { "rt|rejectedcertstoretype=", $"the rejected cert store type. \n(allowed values: Directory, X509Store)\nDefault: {OpcRejectedCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcRejectedCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "rp|rejectedcertstorepath=", $"the path of the rejected cert store\nDefault (depends on store type):\n" +
                            $"X509Store: '{_opcRejectedCertX509StorePathDefault}'\n" +
                            $"Directory: '{_opcRejectedCertDirectoryStorePathDefault}'", (string s) => OpcRejectedCertStorePath = s
                    },

                    // issuer cert store options
                    {
                    "it|issuercertstoretype=", $"the trusted issuer cert store type. \n(allowed values: Directory, X509Store)\nDefault: {OpcIssuerCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcIssuerCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "p|issuercertstorepath=", $"the path of the trusted issuer cert store\nDefault (depends on store type):\n" +
                            $"X509Store: '{_opcIssuerCertX509StorePathDefault}'\n" +
                            $"Directory: '{_opcIssuerCertDirectoryStorePathDefault}'", (string s) => OpcIssuerCertStorePath = s
                    },

                    // device connection string cert store options
                    { "dt|devicecertstoretype=", $"the iothub device cert store type. \n(allowed values: Directory, X509Store)\nDefault: {IotDeviceCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                IotDeviceCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                                IotDeviceCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? _iotDeviceCertX509StorePathDefault : _iotDeviceCertDirectoryStorePathDefault;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "dp|devicecertstorepath=", $"the path of the iot device cert store\nDefault Default (depends on store type):\n" +
                            $"X509Store: '{_iotDeviceCertX509StorePathDefault}'\n" +
                            $"Directory: '{_iotDeviceCertDirectoryStorePathDefault}'", (string s) => IotDeviceCertStorePath = s
                    },

                    // misc
                    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
                };

                List<string> arguments;
                try
                {
                    // parse the command line
                    arguments = options.Parse(args);
                }
                catch (OptionException e)
                {
                    // show usage
                    Usage(options);
                    return;
                }

                // Validate and parse arguments.
                if (arguments.Count > 2 || shouldShowHelp)
                {
                    Usage(options);
                    return;
                }
                else if (arguments.Count == 2)
                {
                    ApplicationName = arguments[0];
                    IoTHubOwnerConnectionString = arguments[1];
                }
                else if (arguments.Count == 1)
                {
                    ApplicationName = arguments[0];
                }
                else {
                    ApplicationName = Utils.GetHostName();
                }

                WriteLine("Publisher is starting up...");
                ModuleConfiguration moduleConfiguration = new ModuleConfiguration(ApplicationName);
                opcTraceInitialized = true;
                m_configuration = moduleConfiguration.Configuration;
                m_configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

                // start our server interface
                try
                {
                    Trace($"Starting server on endpoint {m_configuration.ServerConfiguration.BaseAddresses[0].ToString()} ...");
                    _publishingServer.Start(m_configuration);
                    Trace("Server started.");
                }
                catch (Exception ex)
                {
                    Trace($"Starting server failed with: {ex.Message}");
                    Trace("exiting...");
                    return;
                }

                // check if we also received an owner connection string
                if (string.IsNullOrEmpty(IoTHubOwnerConnectionString))
                {
                    Trace("IoT Hub owner connection string not passed as argument.");

                    // check if we have an environment variable to register ourselves with IoT Hub
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_HUB_CS")))
                    {
                        IoTHubOwnerConnectionString = Environment.GetEnvironmentVariable("_HUB_CS");
                        Trace("IoT Hub owner connection string read from environment.");
                    }
                }

                // register ourselves with IoT Hub
                string deviceConnectionString;
                Trace($"IoTHub device cert store type is: {IotDeviceCertStoreType}");
                Trace($"IoTHub device cert path is: {IotDeviceCertStorePath}");
                if (IoTHubOwnerConnectionString != string.Empty)
                {
                    Trace($"Attemping to register ourselves with IoT Hub using owner connection string: {IoTHubOwnerConnectionString}");
                    RegistryManager manager = RegistryManager.CreateFromConnectionString(IoTHubOwnerConnectionString);

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
                        string hostname = IoTHubOwnerConnectionString.Substring(0, IoTHubOwnerConnectionString.IndexOf(";"));
                        deviceConnectionString = hostname + ";DeviceId=" + ApplicationName + ";SharedAccessKey=" + newDevice.Authentication.SymmetricKey.PrimaryKey;
                        Trace($"Device connection string is: {deviceConnectionString}");
                        Trace($"Adding it to device cert store.");
                        SecureIoTHubToken.Write(ApplicationName, deviceConnectionString, IotDeviceCertStoreType, IotDeviceCertStoreType);
                    }
                    else
                    {
                        Trace($"Could not register ourselves with IoT Hub using owner connection string: {IoTHubOwnerConnectionString}");
                        Trace("exiting...");
                        return;
                    }
                }
                else
                {
                    Trace("IoT Hub owner connection string not specified. Assume device connection string already in cert store.");
                }

                // try to read connection string from secure store and open IoTHub client
                Trace($"Attemping to read device connection string from cert store using subject name: {ApplicationName}");
                deviceConnectionString = SecureIoTHubToken.Read(ApplicationName, IotDeviceCertStoreType, IotDeviceCertStorePath);
                if (!string.IsNullOrEmpty(deviceConnectionString))
                {
                    Trace($"Create Publisher IoTHub client with device connection string: '{deviceConnectionString}' using '{IotHubProtocol}' for communication.");
                    m_deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, IotHubProtocol);
                    m_deviceClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
                    m_deviceClient.OpenAsync().Wait();
                }
                else
                {
                    Trace("Device connection string not found in secure store. Could not connect to IoTHub.");
                    Trace("exiting...");
                    return;
                }

                // get a list of persisted endpoint URLs and create a session for each.
                try
                {
                    if (string.IsNullOrEmpty(PublishedNodesAbsFilename))
                    {
                        // check if we have an env variable specifying the published nodes path, otherwise use the default
                        PublishedNodesAbsFilename = PublishedNodesAbsFilenameDefault;
                        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                        {
                            Trace("Publishing node configuration file path read from environment.");
                            PublishedNodesAbsFilename = Environment.GetEnvironmentVariable("_GW_PNFP");
                        }
                    }
                    Trace($"Attemping to load nodes file from: {PublishedNodesAbsFilename}");
                    m_nodesLookups = JsonConvert.DeserializeObject<PublishedNodesCollection>(File.ReadAllText(PublishedNodesAbsFilename));
                    Trace($"Loaded {m_nodesLookups.Count.ToString()} nodes.");
                }
                catch (Exception ex)
                {
                    Trace($"Nodes file loading failed with: {ex.Message}");
                    Trace("exiting...");
                    return;
                }

                foreach (NodeLookup nodeLookup in m_nodesLookups)
                {
                    if (!m_endpointUrls.Contains(nodeLookup.EndPointURL))
                    {
                        m_endpointUrls.Add(nodeLookup.EndPointURL);
                    }
                }

                // connect to the other servers
                Trace("Attemping to connect to servers...");
                try
                {
                    List<Task> connectionAttempts = new List<Task>();
                    foreach (Uri endpointUrl in m_endpointUrls)
                    {
                        Trace($"Connecting to server: {endpointUrl}");
                        connectionAttempts.Add(EndpointConnect(endpointUrl));
                    }

                    // Wait for all sessions to be connected
                    Task.WaitAll(connectionAttempts.ToArray());
                }
                catch (Exception ex)
                {
                    Trace($"Exception: {ex.ToString()}\r\n{ ex.InnerException?.ToString()}");
                }

                // subscribe to preconfigured nodes
                Trace("Attemping to subscribe to published nodes...");
                if (m_nodesLookups != null)
                {
                    foreach (NodeLookup nodeLookup in m_nodesLookups)
                    {
                        try
                        {
                            CreateMonitoredItem(nodeLookup);
                        }
                        catch (Exception ex)
                        {
                            Trace($"Unexpected error publishing node: {ex.Message}\r\nIgnoring node: {nodeLookup.EndPointURL.AbsoluteUri}, {nodeLookup.NodeID.ToString()}");
                        }
                    }
                }


                Task dequeueAndSendTask = null;
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                Trace("Creating task to send OPC UA messages in batches to IoT Hub...");
                try
                {
                    dequeueAndSendTask = Task.Run(() => DeQueueMessagesAsync(token),token);
                }
                catch (Exception ex)
                {
                    Trace("Exception: " + ex.ToString());
                }

                Trace("Publisher is running. Press enter to quit.");
                Console.ReadLine();

                foreach (Session session in m_sessions)
                {
                    session.Close();
                }

                //Send cancellation token and wait for last IoT Hub message to be sent.
                try
                {
                    tokenSource.Cancel();
                    dequeueAndSendTask.Wait();
                }
                catch (Exception ex)
                {
                    Trace("Exception: " + ex.ToString());
                }

                if (m_deviceClient != null)
                {
                    m_deviceClient.CloseAsync().Wait();
                }

            }
            catch (Exception e)
            {
                if (opcTraceInitialized)
                {
                    Trace(e, "Unhandled exception in Publisher. Exiting... ");
                }
                else
                {
                    WriteLine($"{DateTime.Now.ToString()}: Unhandled exception in Publisher:");
                    WriteLine($"{DateTime.Now.ToString()}: {e.Message.ToString()}");
                    WriteLine($"{DateTime.Now.ToString()}: exiting...");
                }
            }
        }

        /// <summary>
        /// Connects to a single OPC UA Server's endpoint
        /// </summary>
        public static async Task EndpointConnect(Uri endpointUrl)
        {
            EndpointDescription selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl.AbsoluteUri, true);
            ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server, EndpointConfiguration.Create(m_configuration));
            configuredEndpoint.Update(selectedEndpoint);

            Session newSession = await Session.Create(
                m_configuration,
                configuredEndpoint,
                true,
                false,
                m_configuration.ApplicationName,
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            if (newSession != null)
            {
                Trace($"Created session with updated endpoint '{selectedEndpoint.EndpointUrl}' from server!");
                newSession.KeepAlive += new KeepAliveEventHandler((sender, e) => StandardClient_KeepAlive(sender, e, newSession));
                m_sessions.Add(newSession);
            }
        }

        /// <summary>
        /// Creates a subscription to a monitored item on an OPC UA server
        /// </summary>
        public static void CreateMonitoredItem(NodeLookup nodeLookup)
        {
            // find the right session using our lookup
            Session matchingSession = null;
            foreach (Session session in m_sessions)
            {
                char[] trimChars = { '/', ' ' };
                if (session.Endpoint.EndpointUrl.TrimEnd(trimChars).Equals(nodeLookup.EndPointURL.ToString().TrimEnd(trimChars), StringComparison.OrdinalIgnoreCase))
                {
                    matchingSession = session;
                    break;
                }
            }

            if (matchingSession != null)
            {
                Subscription subscription = matchingSession.DefaultSubscription;
                if (matchingSession.AddSubscription(subscription))
                {
                    subscription.Create();
                }

                // get the DisplayName for the node.
                Node node = matchingSession.ReadNode(nodeLookup.NodeID);
                string nodeDisplayName = node.DisplayName.Text;
                if (String.IsNullOrEmpty(nodeDisplayName))
                {
                    nodeDisplayName = nodeLookup.NodeID.Identifier.ToString();
                }

                // add the new monitored item.
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    StartNodeId = nodeLookup.NodeID,
                    AttributeId = Attributes.Value,
                    DisplayName = nodeDisplayName,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = 1000,
                    QueueSize = 0,
                    DiscardOldest = true
                };
                monitoredItem.Notification += new MonitoredItemNotificationEventHandler(MonitoredItem_Notification);
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            else
            {
                Trace($"ERROR: Could not find endpoint URL '{nodeLookup.EndPointURL.ToString()}' in active server sessions, NodeID '{nodeLookup.NodeID.Identifier.ToString()}' NOT published!");
                Trace($"To fix this, please update '{PublishedNodesAbsFilename}' with the updated endpoint URL!");
            }
        }

        /// <summary>
        /// The notification that the data for a monitored item has changed on an OPC UA server
        /// </summary>
        public static void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                if (e.NotificationValue == null || monitoredItem.Subscription.Session == null)
                {
                    return;
                }

                MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
                if (notification == null)
                {
                    return;
                }

                DataValue value = notification.Value as DataValue;
                if (value == null)
                {
                    return;
                }

                JsonEncoder encoder = new JsonEncoder(monitoredItem.Subscription.Session.MessageContext, false);
                string applicationURI = monitoredItem.Subscription.Session.Endpoint.Server.ApplicationUri;
                encoder.WriteString("ApplicationUri", applicationURI);
                encoder.WriteString("DisplayName", monitoredItem.DisplayName);

                // write NodeId as ns=x;i=y
                NodeId nodeId = monitoredItem.ResolvedNodeId;
                encoder.WriteString("NodeId", new NodeId(nodeId.Identifier, nodeId.NamespaceIndex).ToString());

                // suppress output of server timestamp in json by setting it to minvalue
                value.ServerTimestamp = DateTime.MinValue;
                encoder.WriteDataValue("Value", value);

                string json = encoder.CloseAndReturnText();

                // add message to fifo send queue
                m_sendQueue.Enqueue(json);

            }
            catch (Exception exception)
            {
                Trace("Error processing monitored item notification: " + exception.ToString());
            }
        }

        /// <summary>
        /// Dequeue messages
        /// </summary>
        private static async Task DeQueueMessagesAsync(CancellationToken ct)
        {
            try
            {
                //Send every x seconds, regardless if IoT Hub message is full. 
                Timer sendTimer = new Timer(async state => await SendToIoTHubAsync(), null, 0, m_defaultSendIntervalInMilliSeconds);

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Trace("Cancellation requested. Sending remaining messages.");
                        sendTimer.Dispose();
                        await SendToIoTHubAsync();
                        break;
                    }


                    if (m_sendQueue.Count > 0)
                    {
                        bool isPeekSuccessful = false;
                        bool isDequeueSuccessful = false;
                        string messageInJson = string.Empty;
                        int nextMessageSizeBytes = 0;

                        //Perform a TryPeek to determine size of next message 
                        //and whether it will fit. If so, dequeue message and add it to the list. 
                        //If it cannot fit, send current message to IoT Hub, reset it, and repeat.

                        isPeekSuccessful = m_sendQueue.TryPeek(out messageInJson);

                        //Get size of next message in the queue
                        if (isPeekSuccessful)
                        {
                            nextMessageSizeBytes = System.Text.Encoding.UTF8.GetByteCount(messageInJson);
                        }

                        //Determine if it will fit into remaining space of the IoT Hub message. 
                        //If so, dequeue it
                        if (m_currentSizeOfIoTHubMessageBytes + nextMessageSizeBytes < m_maxSizeOfIoTHubMessageBytes)
                        {
                            isDequeueSuccessful = m_sendQueue.TryDequeue(out messageInJson);

                            //Add dequeued message to list
                            if (isDequeueSuccessful)
                            {
                                OpcUaMessage msgPayload = JsonConvert.DeserializeObject<OpcUaMessage>(messageInJson);

                                m_messageList.Add(msgPayload);

                                m_currentSizeOfIoTHubMessageBytes = m_currentSizeOfIoTHubMessageBytes + nextMessageSizeBytes;

                            }
                        }
                        else
                        {
                            //Message is full. Send it to IoT Hub
                            await SendToIoTHubAsync();

                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error dequeuing message: " + exception.ToString());
            }

        }

        /// <summary>
        /// Send dequeued messages to IoT Hub
        /// </summary>
        private static async Task SendToIoTHubAsync()
        {
            if (m_messageList.Count > 0)
            {
                string msgListInJson = JsonConvert.SerializeObject(m_messageList);

                var encodedMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(msgListInJson));

                // publish
                encodedMessage.Properties.Add("content-type", "application/opcua+uajson");
                encodedMessage.Properties.Add("deviceName", m_applicationName);

                try
                {
                    if (m_deviceClient != null)
                    {
                        await m_deviceClient.SendEventAsync(encodedMessage);
                    }
                }
                catch (Exception ex)
                {
                    Trace("Failed to publish message, dropping...");
                    Trace(ex.ToString());
                }

                //Reset IoT Hub message size
                m_currentSizeOfIoTHubMessageBytes = 0;
                m_messageList.Clear();
            }
        }

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

        /// <summary>
        /// Handler for the standard "keep alive" event sent by all OPC UA servers
        /// </summary>
        private static void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e, Session session)
        {
            if (e != null && session != null)
            {
                if (!ServiceResult.IsGood(e.Status))
                {
                    Trace($"Status: {e.Status}/t/tOutstanding requests: {session.OutstandingRequestCount}/t/tDefunct requests: {session.DefunctRequestCount}");
                }
            }
        }

        /// <summary>
        /// Standard certificate validation callback
        /// </summary>
        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Trace($"Certificate '{e.Certificate.Subject}' not trusted. If you want to trust this certificate, please copy it from the/r/n" +
                        $"'{m_configuration.SecurityConfiguration.RejectedCertificateStore.StorePath}/certs' to the " +
                        $"'{m_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}/certs' folder./r/n" +
                        "A restart of the gateway is NOT required.");
            }
        }

    }
}

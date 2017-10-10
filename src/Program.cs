
using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Text.RegularExpressions;
    using static IotHubMessaging;
    using static Opc.Ua.CertificateStoreType;
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static OpcSession;
    using static OpcStackConfiguration;
    using static System.Console;

    public class Program
    {
        public static List<OpcSession> OpcSessions = new List<OpcSession>();
        public static SemaphoreSlim OpcSessionsListSemaphore = new SemaphoreSlim(1);
        public static List<PublisherConfigFileEntry> PublisherConfigFileEntries = new List<PublisherConfigFileEntry>();
        public static List<NodeToPublishConfig> PublishConfig = new List<NodeToPublishConfig>();
        public static SemaphoreSlim PublishDataSemaphore = new SemaphoreSlim(1);
        public static IotHubMessaging IotHubCommunication;

        public static string NodesToPublishAbsFilename
        {
            get => _nodesToPublishAbsFilename;
            set => _nodesToPublishAbsFilename = value;
        }
        private static string _nodesToPublishAbsFilename;

        public static bool PublisherShutdownInProgress
        {
            get => _publisherShutdownInProgress;
            set => _publisherShutdownInProgress = value;
        }
        private static bool _publisherShutdownInProgress = false;

        public static uint PublisherShutdownWaitPeriod
        {
            get => _publisherShutdownWaitPeriod;
            set => _publisherShutdownWaitPeriod = value;
        }
        private static uint _publisherShutdownWaitPeriod = 10;

        private static PublisherServer _publisherServer;
        private static DateTime _lastServerSessionEventTime = DateTime.UtcNow;
        private static bool _opcTraceInitialized = false;
        private static int _publisherSessionConnectWaitSec = 10;
        private static string _nodesToPublishAbsFilenameDefault = $"{System.IO.Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}publishednodes.json";

        /// <summary>
        /// Usage message.
        /// </summary>
        private static void Usage(Mono.Options.OptionSet options)
        {

            // show usage
            WriteLine();
            WriteLine("Usage: {0}.exe <applicationname> [<iothubconnectionstring>] [<options>]", Assembly.GetEntryAssembly().GetName().Name);
            WriteLine();
            WriteLine("OPC Edge Publisher to subscribe to configured OPC UA servers and send telemetry to Azure IoTHub.");
            WriteLine("To exit the application, just press ENTER while it is running.");
            WriteLine();
            WriteLine("applicationname: the OPC UA application name to use, required");
            WriteLine("                 The application name is also used to register the publisher under this name in the");
            WriteLine("                 IoTHub device registry.");
            WriteLine();
            WriteLine("iothubconnectionstring: the IoTHub owner connectionstring, optional");
            WriteLine();
            WriteLine("There are a couple of environment variables which can be used to control the application:");
            WriteLine("_HUB_CS: sets the IoTHub owner connectionstring");
            WriteLine("_GW_LOGP: sets the filename of the log file to use"); 
            WriteLine("_TPC_SP: sets the path to store certificates of trusted stations");
            WriteLine("_GW_PNFP: sets the filename of the publishing configuration file");
            WriteLine();
            WriteLine("Command line arguments overrule environment variable settings.");
            WriteLine();
            
            // output the options
            WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Synchronous main method of the app.
        /// </summary>
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        /// <summary>
        /// Asynchronous part of the main method of the app.
        /// </summary>
        public async static Task MainAsync(string[] args)
        {
            try
            {
                var shouldShowHelp = false;

                // command line options configuration
                Mono.Options.OptionSet options = new Mono.Options.OptionSet {
                    // Publishing configuration options
                    { "pf|publishfile=", $"the filename to configure the nodes to publish.\nDefault: '{_nodesToPublishAbsFilenameDefault}'", (string p) => _nodesToPublishAbsFilename = p },
                    { "sd|shopfloordomain=", $"the domain of the shopfloor. if specified this domain is appended (delimited by a ':' to the 'ApplicationURI' property when telemetry is sent to IoTHub.\n" +
                            "The value must follow the syntactical rules of a DNS hostname.\nDefault: not set", (string s) => {
                            Regex domainNameRegex = new Regex("^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$");
                            if (domainNameRegex.IsMatch(s))
                            {
                                ShopfloorDomain = s;
                            }
                            else
                            {
                                throw new OptionException("The shopfloor domain is not a valid DNS hostname.", "shopfloordomain");
                            }
                        }
                     },
                    { "sw|sessionconnectwait=", $"specify the wait time in seconds publisher is trying to connect to disconnected endpoints and starts monitoring unmonitored items\nMin: 10\nDefault: {_publisherSessionConnectWaitSec}", (int i) => {
                            if (i > 10)
                            {
                                _publisherSessionConnectWaitSec = i;
                            }
                            else
                            {
                                throw new OptionException("The sessionconnectwait must be greater than 10 sec", "sessionconnectwait");
                            }
                        }
                    },
                    { "vc|verboseconsole=", $"the output of publisher is shown on the console.\nDefault: {VerboseConsole}", (bool b) => VerboseConsole = b },

                    // IoTHub specific options
                    { "ih|iothubprotocol=", $"the protocol to use for communication with Azure IoTHub (allowed values: {string.Join(", ", Enum.GetNames(IotHubProtocol.GetType()))}).\nDefault: {Enum.GetName(IotHubProtocol.GetType(), IotHubProtocol)}",
                        (Microsoft.Azure.Devices.Client.TransportType p) => IotHubProtocol = p
                    },
                    { "ms|iothubmessagesize=", $"the max size of a message which can be send to IoTHub. when telemetry of this size is available it will be sent.\n0 will enforce immediate send when telemetry is available\nMin: 0\nMax: 256 * 1024\nDefault: {MaxSizeOfIoTHubMessageBytes}", (uint u) => {
                            if (u >= 0 && u <= 256 * 1024)
                            {
                                MaxSizeOfIoTHubMessageBytes = u;
                            }
                            else
                            {
                                throw new OptionException("The iothubmessagesize must be in the range between 1 and 256*1024.", "iothubmessagesize");
                            }
                        }
                    },
                    { "si|iothubsendinterval=", $"the interval in seconds when telemetry should be send to IoTHub. If 0, then only the iothubmessagesize parameter controls when telemetry is sent.\nDefault: '{DefaultSendIntervalSeconds}'", (int i) => {
                            if (i >= 0)
                            {
                                DefaultSendIntervalSeconds = i;
                            }
                            else
                            {
                                throw new OptionException("The iothubsendinterval must be larger or equal 0.", "iothubsendinterval");
                            }
                        }
                    },

                    // opc server configuration options
                    { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './Logs/<applicationname>.log.txt'", (string l) => LogFileName = l },
                    { "pn|portnum=", $"the server port of the publisher OPC server endpoint.\nDefault: {PublisherServerPort}", (ushort p) => PublisherServerPort = p },
                    { "pa|path=", $"the enpoint URL path part of the publisher OPC server endpoint.\nDefault: '{PublisherServerPath}'", (string a) => PublisherServerPath = a },
                    { "lr|ldsreginterval=", $"the LDS(-ME) registration interval in ms. If 0, then the registration is disabled.\nDefault: {LdsRegistrationInterval}", (int i) => {
                            if (i >= 0)
                            {
                                LdsRegistrationInterval = i;
                            }
                            else
                            {
                                throw new OptionException("The ldsreginterval must be larger or equal 0.", "ldsreginterval");
                            }
                        }
                    },
                    { "ot|operationtimeout=", $"the operation timeout of the publisher OPC UA client in ms.\nDefault: {OpcOperationTimeout}", (int i) => {
                            if (i >= 0)
                            {
                                OpcOperationTimeout = i;
                            }
                            else
                            {
                                throw new OptionException("The operation timeout must be larger or equal 0.", "operationtimeout");
                            }
                        }
                    },
                    { "oi|opcsamplinginterval=", "the publisher is using this as default value in milliseconds to request the servers to sample the nodes with this interval\n" +
                        "this value might be revised by the OPC UA servers to a supported sampling interval.\n" +
                        "please check the OPC UA specification for details how this is handled by the OPC UA stack.\n" +
                        "a negative value will set the sampling interval to the publishing interval of the subscription this node is on.\n" +
                        $"0 will configure the OPC UA server to sample in the highest possible resolution and should be taken with care.\nDefault: {OpcSamplingInterval}", (int i) => OpcSamplingInterval = i
                    },
                    { "op|opcpublishinginterval=", "the publisher is using this as default value in milliseconds for the publishing interval setting of the subscriptions established to the OPC UA servers.\n" +
                        "please check the OPC UA specification for details how this is handled by the OPC UA stack.\n" +
                        $"a value less than or equal zero will let the server revise the publishing interval.\nDefault: {OpcPublishingInterval}", (int i) => {
                            if (i > 0 && i >= OpcSamplingInterval)
                            {
                                OpcPublishingInterval = i;
                            }
                            else
                            {
                                if (i <= 0)
                                {
                                    OpcPublishingInterval = 0;
                                }
                                else
                                {
                                    throw new OptionException($"The opcpublishinterval ({i}) must be larger than the opcsamplinginterval ({OpcSamplingInterval}).", "opcpublishinterval");
                                }
                            }
                        }
                    },
                    { "ct|createsessiontimeout=", $"specify the timeout in seconds used when creating a session to an endpoint. On unsuccessful connection attemps a backoff up to {OpcSessionCreationBackoffMax} times the specified timeout value is used.\nMin: 1\nDefault: {OpcSessionCreationTimeout}", (uint u) => {
                            if (u > 1)
                            {
                                OpcSessionCreationTimeout = u;
                            }
                            else
                            {
                                throw new OptionException("The createsessiontimeout must be greater than 1 sec", "createsessiontimeout");
                            }
                        }
                    },
                    { "ki|keepaliveinterval=", $"specify the interval in seconds the publisher is sending keep alive messages to the OPC servers on the endpoints it is connected to.\nMin: 2\nDefault: {OpcKeepAliveIntervalInSec}", (int i) => {
                            if (i >= 2)
                            {
                                OpcKeepAliveIntervalInSec = i;
                            }
                            else
                            {
                                throw new OptionException("The keepaliveinterval must be greater or equal 2", "keepalivethreshold");
                            }
                        }
                    },
                    { "kt|keepalivethreshold=", $"specify the number of keep alive packets a server can miss, before the session is disconneced\nMin: 1\nDefault: {OpcKeepAliveDisconnectThreshold}", (uint u) => {
                            if (u > 1)
                            {
                                OpcKeepAliveDisconnectThreshold = u;
                            }
                            else
                            {
                                throw new OptionException("The keepalivethreshold must be greater than 1", "keepalivethreshold");
                            }
                        }
                    },
                    { "st|opcstacktracemask=", $"the trace mask for the OPC stack. See github OPC .NET stack for definitions.\nTo enable IoTHub telemetry tracing set it to 711.\nDefault: {OpcStackTraceMask:X}  ({OpcStackTraceMask})", (int i) => {
                            if (i >= 0)
                            {
                                OpcStackTraceMask = i;
                            }
                            else
                            {
                                throw new OptionException("The OPC stack trace mask must be larger or equal 0.", "opcstacktracemask");
                            }
                        }
                    },
                    { "as|autotrustservercerts=", $"the publisher trusts all servers it is establishing a connection to.\nDefault: {OpcPublisherAutoTrustServerCerts}", (bool b) => OpcPublisherAutoTrustServerCerts = b },

                    // trust own public cert option
                    { "tm|trustmyself=", $"the publisher certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", (bool b) => TrustMyself = b },

                    // read the display name of the nodes to publish from the server and publish them instead of the node id
                    { "fd|fetchdisplayname=", $"enable to read the display name of a published node from the server. this will increase the runtime.\nDefault: {FetchOpcNodeDisplayName}", (bool b) => FetchOpcNodeDisplayName = b },

                    // own cert store options
                    { "at|appcertstoretype=", $"the own application cert store type. \n(allowed values: Directory, X509Store)\nDefault: '{OpcOwnCertStoreType}'", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcOwnCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                                OpcOwnCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? OpcOwnCertX509StorePathDefault : OpcOwnCertDirectoryStorePathDefault;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "ap|appcertstorepath=", $"the path where the own application cert should be stored\nDefault (depends on store type):\n" +
                            $"X509Store: '{OpcOwnCertX509StorePathDefault}'\n" +
                            $"Directory: '{OpcOwnCertDirectoryStorePathDefault}'", (string s) => OpcOwnCertStorePath = s
                    },

                    // trusted cert store options
                    {
                    "tt|trustedcertstoretype=", $"the trusted cert store type. \n(allowed values: Directory, X509Store)\nDefault: {OpcTrustedCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcTrustedCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                                OpcTrustedCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? OpcTrustedCertX509StorePathDefault : OpcTrustedCertDirectoryStorePathDefault;
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
                                OpcRejectedCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? OpcRejectedCertX509StorePathDefault : OpcRejectedCertDirectoryStorePathDefault;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "rp|rejectedcertstorepath=", $"the path of the rejected cert store\nDefault (depends on store type):\n" +
                            $"X509Store: '{OpcRejectedCertX509StorePathDefault}'\n" +
                            $"Directory: '{OpcRejectedCertDirectoryStorePathDefault}'", (string s) => OpcRejectedCertStorePath = s
                    },

                    // issuer cert store options
                    {
                    "it|issuercertstoretype=", $"the trusted issuer cert store type. \n(allowed values: Directory, X509Store)\nDefault: {OpcIssuerCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                OpcIssuerCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                                OpcIssuerCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? OpcIssuerCertX509StorePathDefault : OpcIssuerCertDirectoryStorePathDefault;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "ip|issuercertstorepath=", $"the path of the trusted issuer cert store\nDefault (depends on store type):\n" +
                            $"X509Store: '{OpcIssuerCertX509StorePathDefault}'\n" +
                            $"Directory: '{OpcIssuerCertDirectoryStorePathDefault}'", (string s) => OpcIssuerCertStorePath = s
                    },

                    // device connection string cert store options
                    { "dt|devicecertstoretype=", $"the iothub device cert store type. \n(allowed values: Directory, X509Store)\nDefault: {IotDeviceCertStoreType}", (string s) => {
                            if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(Directory, StringComparison.OrdinalIgnoreCase))
                            {
                                IotDeviceCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : Directory;
                                IotDeviceCertStorePath = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? IotDeviceCertX509StorePathDefault : IotDeviceCertDirectoryStorePathDefault;
                            }
                            else
                            {
                                throw new OptionException();
                            }
                        }
                    },
                    { "dp|devicecertstorepath=", $"the path of the iot device cert store\nDefault Default (depends on store type):\n" +
                            $"X509Store: '{IotDeviceCertX509StorePathDefault}'\n" +
                            $"Directory: '{IotDeviceCertDirectoryStorePathDefault}'", (string s) => IotDeviceCertStorePath = s
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
                    // show message
                    WriteLine($"Error: {e.Message}");
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
                    IotHubOwnerConnectionString = arguments[1];
                }
                else if (arguments.Count == 1)
                {
                    ApplicationName = arguments[0];
                }
                else
                {
                    ApplicationName = Utils.GetHostName();
                }

                WriteLine("Publisher is starting up...");

                // init OPC configuration and tracing
                OpcStackConfiguration opcStackConfiguration = new OpcStackConfiguration();
                await opcStackConfiguration.ConfigureAsync();
                _opcTraceInitialized = true;

                // log shopfloor domain setting
                if (string.IsNullOrEmpty(ShopfloorDomain))
                {
                    Trace("There is no shopfloor domain configured.");
                }
                else
                {
                    Trace($"Publisher is in shopfloor domain '{ShopfloorDomain}'.");
                }

                // Set certificate validator.
                if (OpcPublisherAutoTrustServerCerts)
                {
                    Trace("Publisher configured to auto trust server certificates of the servers it is connecting to.");
                    PublisherOpcApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_AutoTrustServerCerts);
                }
                else
                {
                    Trace("Publisher configured to not auto trust server certificates. When connecting to servers, you need to manually copy the rejected server certs to the trusted store to trust them.");
                    PublisherOpcApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_Default);
                }

                // start our server interface
                try
                {
                    Trace($"Starting server on endpoint {PublisherOpcApplicationConfiguration.ServerConfiguration.BaseAddresses[0].ToString()} ...");
                    _publisherServer = new PublisherServer();
                    _publisherServer.Start(PublisherOpcApplicationConfiguration);
                    Trace("Server started.");
                }
                catch (Exception e)
                {
                    Trace(e, $"Failed to start Publisher OPC UA server.");
                    Trace("exiting...");
                    return;
                }

                // get information on the nodes to publish and validate the json by deserializing it.
                try
                {
                    await PublishDataSemaphore.WaitAsync();
                    if (string.IsNullOrEmpty(_nodesToPublishAbsFilename))
                    {
                        // check if we have an env variable specifying the published nodes path, otherwise use the default
                        _nodesToPublishAbsFilename = _nodesToPublishAbsFilenameDefault;
                        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_PNFP")))
                        {
                            Trace("Publishing node configuration file path read from environment.");
                            _nodesToPublishAbsFilename = Environment.GetEnvironmentVariable("_GW_PNFP");
                        }
                    }

                    Trace($"Attempting to load nodes file from: {_nodesToPublishAbsFilename}");
                    PublisherConfigFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigFileEntry>>(File.ReadAllText(_nodesToPublishAbsFilename));
                    Trace($"Loaded {PublisherConfigFileEntries.Count.ToString()} config file entry/entries.");

                    foreach (var publisherConfigFileEntry in PublisherConfigFileEntries)
                    {
                        if (publisherConfigFileEntry.NodeId == null)
                        {
                            // new node configuration syntax.
                            foreach (var opcNode in publisherConfigFileEntry.OpcNodes)
                            {
                                PublishConfig.Add(new NodeToPublishConfig(ExpandedNodeId.Parse(opcNode.ExpandedNodeId), publisherConfigFileEntry.EndpointUri, opcNode.OpcSamplingInterval ?? OpcSamplingInterval, opcNode.OpcPublishingInterval ?? OpcPublishingInterval));
                            }
                        }
                        else
                        {
                            // legacy (using ns=) node configuration syntax using default sampling and publishing interval.
                            PublishConfig.Add(new NodeToPublishConfig(publisherConfigFileEntry.NodeId, publisherConfigFileEntry.EndpointUri, OpcSamplingInterval, OpcPublishingInterval));
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace(e, "Loading of the node configuration file failed. Does the file exist and has correct syntax?");
                    Trace("exiting...");
                    return;
                }
                finally
                {
                    PublishDataSemaphore.Release();
                }
                Trace($"There are {PublishConfig.Count.ToString()} nodes to publish.");

                // initialize and start IoTHub messaging
                IotHubCommunication = new IotHubMessaging();
                if (! await IotHubCommunication.InitAsync())
                {
                    return;
                }

                // create a list to manage sessions, subscriptions and monitored items.
                try
                {
                    await PublishDataSemaphore.WaitAsync();
                    await OpcSessionsListSemaphore.WaitAsync();
                    var uniqueEndpointUrls = PublishConfig.Select(n => n.EndpointUri).Distinct();
                    foreach (var endpointUrl in uniqueEndpointUrls)
                    {
                        // create new session info.
                        OpcSession opcSession = new OpcSession(endpointUrl, OpcSessionCreationTimeout);

                        // create a subscription for each distinct publishing inverval
                        var nodesDistinctPublishingInterval = PublishConfig.Where(n => n.EndpointUri.Equals(endpointUrl)).Select(c => c.OpcPublishingInterval).Distinct();
                        foreach (var nodeDistinctPublishingInterval in nodesDistinctPublishingInterval)
                        {
                            // create a subscription for the publishing interval and add it to the session.
                            OpcSubscription opcSubscription = new OpcSubscription(nodeDistinctPublishingInterval);

                            // add all nodes with this OPC publishing interval to this subscription.
                            var nodesWithSamePublishingInterval = PublishConfig.Where(n => n.EndpointUri.Equals(endpointUrl)).Where(n => n.OpcPublishingInterval == nodeDistinctPublishingInterval);
                            foreach (var nodeInfo in nodesWithSamePublishingInterval)
                            {
                                // differentiate if legacy (using ns=) or new syntax (using nsu=) is used
                                if (nodeInfo.NodeId == null)
                                {
                                    // create a monitored item for the node
                                    OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.ExpandedNodeId, opcSession.EndpointUri)
                                    {
                                        RequestedSamplingInterval = nodeInfo.OpcSamplingInterval,
                                        SamplingInterval = nodeInfo.OpcSamplingInterval
                                    };
                                    opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                                }
                                else
                                {
                                    // give user a warning that the syntax is obsolete
                                    Trace($"Please update the syntax of the configuration file and use ExpandedNodeId instead of NodeId property name for node with identifier '{nodeInfo.NodeId.ToString()}' on EndpointUrl '{nodeInfo.EndpointUri.AbsolutePath}'.");

                                    // create a monitored item for the node with the configured or default sampling interval
                                    OpcMonitoredItem opcMonitoredItem = new OpcMonitoredItem(nodeInfo.NodeId, opcSession.EndpointUri)
                                    {
                                        RequestedSamplingInterval = nodeInfo.OpcSamplingInterval,
                                        SamplingInterval = nodeInfo.OpcSamplingInterval
                                    };
                                    opcSubscription.OpcMonitoredItems.Add(opcMonitoredItem);
                                }
                            }

                            // add subscription to session.
                            opcSession.OpcSubscriptions.Add(opcSubscription);
                        }

                        // add session.
                        OpcSessions.Add(opcSession);
                    }
                }
                finally
                {
                    OpcSessionsListSemaphore.Release();
                    PublishDataSemaphore.Release();
                }

                // kick off the task to maintain all sessions
                var cts = new CancellationTokenSource();
                Task.Run(() => SessionConnectorAsync(cts.Token));

                // Show notification on session events
                _publisherServer.CurrentInstance.SessionManager.SessionActivated += ServerEventStatus;
                _publisherServer.CurrentInstance.SessionManager.SessionClosing += ServerEventStatus;
                _publisherServer.CurrentInstance.SessionManager.SessionCreated += ServerEventStatus;

                // stop on user request
                WriteLine("");
                WriteLine("");
                WriteLine("Publisher is running. Press ENTER to quit.");
                WriteLine("");
                WriteLine("");
                ReadLine();
                cts.Cancel();
                WriteLine("Publisher is shuting down...");

                // close all connected session
                PublisherShutdownInProgress = true;

                // stop the server
                _publisherServer.Stop();

                // Clean up Publisher sessions
                SessionShutdownAsync().Wait();

                // shutdown the IoTHub messaging
                IotHubCommunication.Shutdown();
            }
            catch (Exception e)
            {
                if (_opcTraceInitialized)
                {
                    Trace(e, e.StackTrace);
                    e = e.InnerException ?? null;
                    while (e != null)
                    {
                        Trace(e, e.StackTrace);
                        e = e.InnerException ?? null;
                    }
                    Trace("Publisher exiting... ");
                }
                else
                {
                    WriteLine($"{DateTime.Now.ToString()}: {e.Message.ToString()}");
                    WriteLine($"{DateTime.Now.ToString()}: {e.StackTrace}");
                    e = e.InnerException ?? null;
                    while (e != null)
                    {
                        WriteLine($"{DateTime.Now.ToString()}: {e.Message.ToString()}");
                        WriteLine($"{DateTime.Now.ToString()}: {e.StackTrace}");
                        e = e.InnerException ?? null;
                    }
                    WriteLine($"{DateTime.Now.ToString()}: Publisher exiting...");
                }
            }
        }

        /// <summary>
        /// Kicks of the work horse of the publisher regularily for all sessions.
        /// </summary>
        public static async Task SessionConnectorAsync(CancellationToken cancellationtoken)
        {
            while (true && !PublisherShutdownInProgress)
            {
                try
                {
                    // get tasks for all disconnected sessions and start them
                    await OpcSessionsListSemaphore.WaitAsync();
                    var singleSessionHandlerTaskList = OpcSessions.Select(s => s.ConnectAndMonitorAsync());
                    OpcSessionsListSemaphore.Release();
                    await Task.WhenAll(singleSessionHandlerTaskList);
                }
                catch (Exception e)
                {
                    Trace(e, $"Failed to connect and monitor a disconnected server. {(e.InnerException != null ? e.InnerException.Message : "")}");
                }
                await Task.Delay(_publisherSessionConnectWaitSec * 1000);
            }
        }

        /// <summary>
        /// Wait till all sessions are shutdown.
        /// async tasks.
        /// </summary>
        public async static Task SessionShutdownAsync()
        {
            // Shutdown all sessions.
            try
            {
                await OpcSessionsListSemaphore.WaitAsync();
                while (OpcSessions.Count > 0)
                {
                    OpcSession opcSession = OpcSessions.ElementAt(0);
                    OpcSessions.RemoveAt(0);
                    await opcSession.ShutdownAsync();
                }
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
            }

            // Wait and continue after a while.
            uint maxTries = PublisherShutdownWaitPeriod;
            while (true)
            {
                int sessionCount = OpcSessions.Count;
                if (sessionCount == 0)
                {
                    return;
                }
                if (maxTries-- == 0)
                {
                    Trace($"There are still {sessionCount} sessions alive. Ignore and continue shutdown.");
                    return;
                }
                Trace($"Publisher is shutting down. Wait {_publisherSessionConnectWaitSec} seconds, since there are stil {sessionCount} sessions alive...");
                await Task.Delay(_publisherSessionConnectWaitSec * 1000);
            }
        }

        /// <summary>
        /// Default certificate validation callback
        /// </summary>
        private static void CertificateValidator_Default(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Trace($"The Publisher does not trust the server with the certificate subject '{e.Certificate.Subject}'.");
                Trace("If you want to trust this certificate, please copy it from the directory:");
                Trace($"{PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}/certs");
                Trace("to the directory:");
                Trace($"{PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}/certs");
            }
        }

        /// <summary>
        /// Auto trust server certificate validation callback
        /// </summary>
        private static void CertificateValidator_AutoTrustServerCerts(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Trace($"Certificate '{e.Certificate.Subject}' will be trusted, since the autotrustservercerts options was specified.");
                e.Accept = true;
                return;
            }
        }

        /// <summary>
        /// Handler for server status changes.
        /// </summary>
    
        private static void ServerEventStatus(Session session, SessionEventReason reason)
        {
            _lastServerSessionEventTime = DateTime.UtcNow;
            PrintSessionStatus(session, reason.ToString());
        }

        /// <summary>
        /// Shows the session status.
        /// </summary>
        private static void PrintSessionStatus(Session session, string reason)
        {
            lock (session.DiagnosticsLock)
            {
                string item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
                if (session.Identity != null)
                {
                    item += String.Format(":{0,20}", session.Identity.DisplayName);
                }
                item += String.Format(":{0}", session.Id);
                Trace(item);
            }
        }
    }
}

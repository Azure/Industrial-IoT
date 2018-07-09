
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Serilog;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using static Diagnostics;
    using static HubCommunication;
    using static IotHubCommunication;
    using static IotEdgeHubCommunication;
    using static Opc.Ua.CertificateStoreType;
    using static OpcSession;
    using static OpcStackConfiguration;
    using static PublisherNodeConfiguration;
    using static PublisherTelemetryConfiguration;
    using static System.Console;

    public class Program
    {
        public static IotHubCommunication IotHubCommunication;
        public static IotEdgeHubCommunication IotEdgeHubCommunication;
        public static CancellationTokenSource ShutdownTokenSource;

        public static uint PublisherShutdownWaitPeriod { get; } = 10;

        public static DateTime PublisherStartTime = DateTime.UtcNow;

        public static Serilog.Core.Logger Logger = null;

        /// <summary>
        /// Synchronous main method of the app.
        /// </summary>
        public static void Main(string[] args)
        {
            // enable this to catch when running in IoTEdge
            //bool waitHere = true;
            //int i = 0;
            //while (waitHere)
            //{
            //    WriteLine($"forever loop (iteration {i++})");
            //    Thread.Sleep(5000);
            //}
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

                // Shutdown token sources.
                ShutdownTokenSource = new CancellationTokenSource();

                // detect the runtime environment. either we run standalone (native or containerized) or as IoT Edge module (containerized)
                // check if we have an environment variable containing an IoT Edge connectionstring, we run as IoT Edge module
                if (IsIotEdgeModule)
                {
                    WriteLine("IoTEdge detected.");
                }

                // command line options
                Mono.Options.OptionSet options = new Mono.Options.OptionSet {
                        // Publishing configuration options
                        { "pf|publishfile=", $"the filename to configure the nodes to publish.\nDefault: '{PublisherNodeConfigurationFilename}'", (string p) => PublisherNodeConfigurationFilename = p },
                        { "tc|telemetryconfigfile=", $"the filename to configure the ingested telemetry\nDefault: '{PublisherTelemetryConfigurationFilename}'", (string p) => PublisherTelemetryConfigurationFilename = p },
                        { "s|site=", $"the site OPC Publisher is working in. if specified this domain is appended (delimited by a ':' to the 'ApplicationURI' property when telemetry is sent to IoTHub.\n" +
                                "The value must follow the syntactical rules of a DNS hostname.\nDefault: not set", (string s) => {
                                Regex siteNameRegex = new Regex("^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$");
                                if (siteNameRegex.IsMatch(s))
                                {
                                    PublisherSite = s;
                                }
                                else
                                {
                                    throw new OptionException("The shopfloor site is not a valid DNS hostname.", "site");
                                }
                            }
                         },
                        { "ic|iotcentral", $"publisher will send OPC UA data in IoTCentral compatible format (DisplayName of a node is used as key, this key is the Field name in IoTCentral). you need to ensure that all DisplayName's are unique. (Auto enables fetch display name)\nDefault: {IotCentralMode}", b => IotCentralMode = FetchOpcNodeDisplayName = b != null },
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
                        { "mq|monitoreditemqueuecapacity=", $"specify how many notifications of monitored items can be stored in the internal queue, if the data can not be sent quick enough to IoTHub\nMin: 1024\nDefault: {MonitoredItemsQueueCapacity}", (int i) => {
                                if (i >= 1024)
                                {
                                    MonitoredItemsQueueCapacity = i;
                                }
                                else
                                {
                                    throw new OptionException("The monitoreditemqueueitems must be greater than 1024", "monitoreditemqueueitems");
                                }
                            }
                        },
                        { "di|diagnosticsinterval=", $"shows publisher diagnostic info at the specified interval in seconds (need log level info). 0 disables diagnostic output.\nDefault: {DiagnosticsInterval}", (uint u) => DiagnosticsInterval = u },

                        { "vc|verboseconsole=", $"ignored, only supported for backward comaptibility.", b => {}},

                        { "ns|noshutdown=", $"same as runforever.\nDefault: {_noShutdown}", (bool b) => _noShutdown = b },
                        { "rf|runforwver", $"publisher can not be stopped by pressing a key on the console, but will run forever.\nDefault: {_noShutdown}", b => _noShutdown = b != null },
                    
                        // IoTHub specific options
                        { "ih|iothubprotocol=", $"{(IsIotEdgeModule ? "not supported when running as IoTEdge module (Mqtt_Tcp_Only is enforced)\n" : $"the protocol to use for communication with Azure IoTHub (allowed values: {string.Join(", ", Enum.GetNames(IotHubProtocol.GetType()))}).\nDefault: {Enum.GetName(IotHubProtocol.GetType(), IotHubProtocol)}")}",
                            (Microsoft.Azure.Devices.Client.TransportType p) => {
                                if (IsIotEdgeModule)
                                {
                                    if (p != Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only)
                                    {
                                        WriteLine("When running as IoTEdge module Mqtt_Tcp_Only is enforced.");
                                        IotHubProtocol = Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only;
                                    }
                                }
                                else
                                {
                                    IotHubProtocol = p;
                                }
                            }
                        },
                        { "ms|iothubmessagesize=", $"the max size of a message which can be send to IoTHub. when telemetry of this size is available it will be sent.\n0 will enforce immediate send when telemetry is available\nMin: 0\nMax: {HubMessageSizeMax}\nDefault: {HubMessageSize}", (uint u) => {
                                if (u >= 0 && u <= HubMessageSizeMax)
                                {
                                    HubMessageSize = u;
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
                        { "dc|deviceconnectionstring=", $"{(IsIotEdgeModule ? "not supported when running as IoTEdge module\n" : $"if publisher is not able to register itself with IoTHub, you can create a device with name <applicationname> manually and pass in the connectionstring of this device.\nDefault: none")}",
                            (string dc) => DeviceConnectionString = (IsIotEdgeModule ? null : dc)
                        },
                        { "c|connectionstring=", $"the IoTHub owner connectionstring.\nDefault: none",
                            (string cs) => IotHubOwnerConnectionString = cs
                        },

                        // opc server configuration options
                        { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './{_logFileName}'", (string l) => _logFileName = l },
                        { "ll|loglevel=", $"the loglevel to use (allowed: fatal, error, warn, info, debug, verbose).\nDefault: info", (string l) => {
                                List<string> logLevels = new List<string> {"fatal", "error", "warn", "info", "debug", "verbose"};
                                if (logLevels.Contains(l.ToLowerInvariant()))
                                {
                                    _logLevel = l.ToLowerInvariant();
                                }
                                else
                                {
                                    throw new Mono.Options.OptionException("The loglevel must be one of: fatal, error, warn, info, debug, verbose", "loglevel");
                                }
                            }
                        },
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
                        { "ol|opcmaxstringlen=", $"the max length of a string opc can transmit/receive.\nDefault: {OpcMaxStringLength}", (int i) => {
                                if (i > 0)
                                {
                                    OpcMaxStringLength = i;
                                }
                                else
                                {
                                    throw new OptionException("The max opc string length must be larger than 0.", "opcmaxstringlen");
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
                        { "st|opcstacktracemask=", $"ignored, only supported for backward comaptibility.", i => {}},

                        { "as|autotrustservercerts=", $"same as autoaccept, only supported for backward cmpatibility.\nDefault: {OpcPublisherAutoTrustServerCerts}", (bool b) => OpcPublisherAutoTrustServerCerts = b },
                        { "aa|autoaccept", $"the publisher trusts all servers it is establishing a connection to.\nDefault: {OpcPublisherAutoTrustServerCerts}", b => OpcPublisherAutoTrustServerCerts = b != null },

                        // trust own public cert option
                        { "tm|trustmyself=", $"same as trustowncert.\nDefault: {TrustMyself}", (bool b) => TrustMyself = b  },
                        { "to|trustowncert", $"the publisher certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", t => TrustMyself = t != null  },
                        // read the display name of the nodes to publish from the server and publish them instead of the node id
                        { "fd|fetchdisplayname", $"enable to read the display name of a published node from the server. this will increase the runtime.\nDefault: {FetchOpcNodeDisplayName}", b => FetchOpcNodeDisplayName = IotCentralMode ? true : b != null },

                        // own cert store options
                        { "at|appcertstoretype=", $"the own application cert store type. \n(allowed values: Directory, X509Store)\nDefault: '{OpcOwnCertStoreType}'", (string s) => {
                                if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                                {
                                    OpcOwnCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : CertificateStoreType.Directory;
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
                                if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                                {
                                    OpcTrustedCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : CertificateStoreType.Directory;
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
                                if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                                {
                                    OpcRejectedCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : CertificateStoreType.Directory;
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
                                if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                                {
                                    OpcIssuerCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : CertificateStoreType.Directory;
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
                                if (s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                                {
                                    IotDeviceCertStoreType = s.Equals(X509Store, StringComparison.OrdinalIgnoreCase) ? X509Store : CertificateStoreType.Directory;
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
                        { "i|install", $"register OPC Publisher with IoTHub and then exits.\nDefault:  {_installOnly}", i => _installOnly = i != null },
                        { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
                    };


                List<string> extraArgs = new List<string>();
                try
                {
                    // parse the command line
                    extraArgs = options.Parse(args);
                }
                catch (OptionException e)
                {
                    // initialize logging
                    InitLogging();

                    // show message
                    Logger.Error(e, "Error in command line options");
                    Logger.Error($"Command line arguments: {String.Join(" ", args)}");

                    // show usage
                    Usage(options);
                    return;
                }

                // initialize logging
                InitLogging();

                // show usage if requested
                if (shouldShowHelp)
                {
                    Usage(options);
                    return;
                }

                // Validate and parse extra arguments.
                const int APP_NAME_INDEX = 0;
                const int CS_INDEX = 1;
                switch (extraArgs.Count)
                {
                    case 0:
                        {
                            ApplicationName = Utils.GetHostName();
                            break;
                        }
                    case 1:
                        {
                            ApplicationName = extraArgs[APP_NAME_INDEX];
                            break;
                        }
                    case 2:
                        {
                            ApplicationName = extraArgs[APP_NAME_INDEX];
                            if (IsIotEdgeModule)
                            {
                                WriteLine($"Warning: connection string parameter is not supported in IoTEdge context, given parameter is ignored");
                            }
                            else
                            {
                                IotHubOwnerConnectionString = extraArgs[CS_INDEX];
                            }
                            break;
                        }
                    default:
                        {
                            Logger.Error("Error in command line options");
                            Logger.Error($"Command line arguments: {String.Join(" ", args)}");
                            Usage(options);
                            return;
                        }
                }

                // install only if requested
                if (_installOnly)
                {
                    // initialize and start IoTHub communication
                    IotHubCommunication = new IotHubCommunication(ShutdownTokenSource.Token);
                    if (!await IotHubCommunication.InitAsync())
                    {
                        return;
                    }
                    Logger.Information("Installation completed. Exiting...");
                    return;
                }

                // start operation
                Logger.Information("Publisher is starting up...");

                // allow canceling the application
                var quitEvent = new ManualResetEvent(false);
                try
                {
                    Console.CancelKeyPress += (sender, eArgs) =>
                    {
                        quitEvent.Set();
                        eArgs.Cancel = true;
                        ShutdownTokenSource.Cancel();
                    };
                }
                catch
                {
                }

                // init OPC configuration and tracing
                OpcStackConfiguration opcStackConfiguration = new OpcStackConfiguration();
                await opcStackConfiguration.ConfigureAsync();

                // log shopfloor site setting
                if (string.IsNullOrEmpty(PublisherSite))
                {
                    Logger.Information("There is no site configured.");
                }
                else
                {
                    Logger.Information($"Publisher is in site '{PublisherSite}'.");
                }

                // Set certificate validator.
                if (OpcPublisherAutoTrustServerCerts)
                {
                    Logger.Information("Publisher configured to auto trust server certificates of the servers it is connecting to.");
                    PublisherOpcApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_AutoTrustServerCerts);
                }
                else
                {
                    Logger.Information("Publisher configured to not auto trust server certificates. When connecting to servers, you need to manually copy the rejected server certs to the trusted store to trust them.");
                    PublisherOpcApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_Default);
                }

                // start our server interface
                try
                {
                    Logger.Information($"Starting server on endpoint {PublisherOpcApplicationConfiguration.ServerConfiguration.BaseAddresses[0].ToString()} ...");
                    _publisherServer = new PublisherServer();
                    _publisherServer.Start(PublisherOpcApplicationConfiguration);
                    Logger.Information("Server started.");
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "Failed to start Publisher OPC UA server.");
                    Logger.Fatal("exiting...");
                    return;
                }

                // read telemetry configuration file
                PublisherTelemetryConfiguration.Init(ShutdownTokenSource.Token);
                if (!await PublisherTelemetryConfiguration.ReadConfigAsync())
                {
                    return;
                }

                // read node configuration file
                PublisherNodeConfiguration.Init();
                if (!await PublisherNodeConfiguration.ReadConfigAsync())
                {
                    return;
                }

                // initialize hub communication
                if (IsIotEdgeModule)
                {
                    // initialize and start EdgeHub communication
                    IotEdgeHubCommunication = new IotEdgeHubCommunication(ShutdownTokenSource.Token);
                    if (!await IotEdgeHubCommunication.InitAsync())
                    {
                        return;
                    }
                }
                else
                {
                    // initialize and start IoTHub communication
                    IotHubCommunication = new IotHubCommunication(ShutdownTokenSource.Token);
                    if (!await IotHubCommunication.InitAsync())
                    {
                        return;
                    }
                }

                if (!await CreateOpcPublishingDataAsync())
                {
                    return;
                }

                // kick off the task to maintain all sessions
                Task sessionConnectorAsync = Task.Run(async () => await SessionConnectorAsync(ShutdownTokenSource.Token));

                // Show notification on session events
                _publisherServer.CurrentInstance.SessionManager.SessionActivated += ServerEventStatus;
                _publisherServer.CurrentInstance.SessionManager.SessionClosing += ServerEventStatus;
                _publisherServer.CurrentInstance.SessionManager.SessionCreated += ServerEventStatus;

                // initialize publisher diagnostics
                Diagnostics.Init();

                // stop on user request
                Logger.Information("");
                Logger.Information("");
                if (_noShutdown)
                {
                    // wait forever if asked to do so
                    Logger.Information("Publisher is running infinite...");
                    await Task.Delay(Timeout.Infinite);
                }
                else
                {
                    Logger.Information("Publisher is running. Press CTRL-C to quit.");

                    // wait for Ctrl-C
                    quitEvent.WaitOne(Timeout.Infinite);
                }

                Logger.Information("");
                Logger.Information("");
                ShutdownTokenSource.Cancel();
                Logger.Information("Publisher is shutting down...");

                // Wait for session connector completion
                await sessionConnectorAsync;

                // stop the server
                _publisherServer.Stop();

                // Clean up Publisher sessions
                await SessionShutdownAsync();

                // shutdown the IoTHub messaging
                await IotHubCommunication.ShutdownAsync();
                IotHubCommunication = null;

                // shutdown diagnostics
                await ShutdownAsync();

                // free resources
                PublisherTelemetryConfiguration.Deinit();
                PublisherNodeConfiguration.Deinit();
                ShutdownTokenSource = null;
            }
            catch (Exception e)
            {
                Logger.Fatal(e, e.StackTrace);
                e = e.InnerException ?? null;
                while (e != null)
                {
                    Logger.Fatal(e, e.StackTrace);
                    e = e.InnerException ?? null;
                }
                Logger.Fatal("Publisher exiting... ");
            }
        }

        /// <summary>
        /// Kicks of the work horse of the publisher regularily for all sessions.
        /// </summary>
        public static async Task SessionConnectorAsync(CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    // get tasks for all disconnected sessions and start them
                    Task[] singleSessionHandlerTaskList;
                    try
                    {
                        await OpcSessionsListSemaphore.WaitAsync();
                        singleSessionHandlerTaskList = OpcSessions.Select(s => s.ConnectAndMonitorAsync(ct)).ToArray();
                    }
                    finally
                    {
                        OpcSessionsListSemaphore.Release();
                    }
                    Task.WaitAll(singleSessionHandlerTaskList);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to connect and monitor a disconnected server. {(e.InnerException != null ? e.InnerException.Message : "")}");
                }
                try
                {
                    await Task.Delay(_publisherSessionConnectWaitSec * 1000, ct);
                }
                catch { }
                if (ct.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Shutdown all sessions.
        /// </summary>
        public async static Task SessionShutdownAsync()
        {
            try
            {
                while (OpcSessions.Count > 0)
                {
                    OpcSession opcSession = null;
                    try
                    {
                        await OpcSessionsListSemaphore.WaitAsync();
                        opcSession = OpcSessions.ElementAt(0);
                        OpcSessions.RemoveAt(0);
                    }
                    finally
                    {
                        OpcSessionsListSemaphore.Release();
                    }
                    await opcSession?.ShutdownAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Failed to shutdown all sessions.");
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
                    Logger.Information($"There are still {sessionCount} sessions alive. Ignore and continue shutdown.");
                    return;
                }
                Logger.Information($"Publisher is shutting down. Wait {_publisherSessionConnectWaitSec} seconds, since there are stil {sessionCount} sessions alive...");
                await Task.Delay(_publisherSessionConnectWaitSec * 1000);
            }
        }

        /// <summary>
        /// Usage message.
        /// </summary>
        private static void Usage(Mono.Options.OptionSet options)
        {

            // show usage
            Logger.Information("");
            Logger.Information("Usage: {0}.exe <applicationname> [<iothubconnectionstring>] [<options>]", Assembly.GetEntryAssembly().GetName().Name);
            Logger.Information("");
            Logger.Information("OPC Edge Publisher to subscribe to configured OPC UA servers and send telemetry to Azure IoTHub.");
            Logger.Information("To exit the application, just press ENTER while it is running.");
            Logger.Information("");
            Logger.Information("applicationname: the OPC UA application name to use, required");
            Logger.Information("                 The application name is also used to register the publisher under this name in the");
            Logger.Information("                 IoTHub device registry.");
            Logger.Information("");
            Logger.Information("iothubconnectionstring: the IoTHub owner connectionstring, optional");
            Logger.Information("");
            Logger.Information("There are a couple of environment variables which can be used to control the application:");
            Logger.Information("_HUB_CS: sets the IoTHub owner connectionstring");
            Logger.Information("_GW_LOGP: sets the filename of the log file to use");
            Logger.Information("_TPC_SP: sets the path to store certificates of trusted stations");
            Logger.Information("_GW_PNFP: sets the filename of the publishing configuration file");
            Logger.Information("");
            Logger.Information("Command line arguments overrule environment variable settings.");
            Logger.Information("");

            // output the options
            Logger.Information("Options:");
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);
            options.WriteOptionDescriptions(stringWriter);
            string[] helpLines = stringBuilder.ToString().Split("\n");
            foreach (var line in helpLines)
            {
                Logger.Information(line);
            }
        }

        /// <summary>
        /// Default certificate validation callback
        /// </summary>
        private static void CertificateValidator_Default(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Logger.Information($"OPC Publisher does not trust the server with the certificate subject '{e.Certificate.Subject}'.");
                Logger.Information("If you want to trust this certificate, please copy it from the directory:");
                Logger.Information($"{PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}/certs");
                Logger.Information("to the directory:");
                Logger.Information($"{PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}/certs");
            }
        }

        /// <summary>
        /// Auto trust server certificate validation callback
        /// </summary>
        private static void CertificateValidator_AutoTrustServerCerts(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                Logger.Information($"Certificate '{e.Certificate.Subject}' will be trusted, since the autotrustservercerts options was specified.");
                e.Accept = true;
                return;
            }
        }

        /// <summary>
        /// Handler for server status changes.
        /// </summary>
        private static void ServerEventStatus(Session session, SessionEventReason reason)
        {
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
                Logger.Information(item);
            }
        }

        /// <summary>
        /// Initialize logging.
        /// </summary>
        private static void InitLogging()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration();

            // set the log level
            switch (_logLevel)
            {
                case "fatal":
                    loggerConfiguration.MinimumLevel.Fatal();
                    OpcTraceToLoggerFatal = 0;
                    break;
                case "error":
                    loggerConfiguration.MinimumLevel.Error();
                    OpcStackTraceMask = OpcTraceToLoggerError = Utils.TraceMasks.Error;
                    break;
                case "warn":
                    loggerConfiguration.MinimumLevel.Warning();
                    OpcTraceToLoggerWarning = 0;
                    break;
                case "info":
                    loggerConfiguration.MinimumLevel.Information();
                    OpcStackTraceMask = OpcTraceToLoggerInformation = 0;
                    break;
                case "debug":
                    loggerConfiguration.MinimumLevel.Debug();
                    OpcStackTraceMask = OpcTraceToLoggerDebug = Utils.TraceMasks.StackTrace | Utils.TraceMasks.Operation |
                        Utils.TraceMasks.StartStop | Utils.TraceMasks.ExternalSystem | Utils.TraceMasks.Security;
                    break;
                case "verbose":
                    loggerConfiguration.MinimumLevel.Verbose();
                    OpcStackTraceMask = OpcTraceToLoggerVerbose = Utils.TraceMasks.All;
                    break;
            }

            // set logging sinks
            loggerConfiguration.WriteTo.Console();

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
            {
                _logFileName = Environment.GetEnvironmentVariable("_GW_LOGP");
            }

            if (!string.IsNullOrEmpty(_logFileName))
            {
                // configure rolling file sink
                const int MAX_LOGFILE_SIZE = 1024 * 1024;
                const int MAX_RETAINED_LOGFILES = 2;
                loggerConfiguration.WriteTo.File(_logFileName, fileSizeLimitBytes: MAX_LOGFILE_SIZE, rollOnFileSizeLimit: true, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
            }

            Logger = loggerConfiguration.CreateLogger();
            Logger.Information($"Current directory is: {System.IO.Directory.GetCurrentDirectory()}");
            Logger.Information($"Log file is: {_logFileName}");
            Logger.Information($"Log level is: {_logLevel}");
            return;
        }

        private static PublisherServer _publisherServer;
        private static int _publisherSessionConnectWaitSec = 10;
        private static bool _noShutdown = false;
        private static bool _installOnly = false;
        private static string _logFileName = $"{Utils.GetHostName()}-publisher.log";
        private static string _logLevel = "info";
    }
}


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
    using static OpcApplicationConfiguration;
    using static PublisherNodeConfiguration;
    using static PublisherTelemetryConfiguration;
    using static System.Console;

    public class Program
    {
        /// <summary>
        /// IoTHub/EdgeHub communication objects.
        /// </summary>
        public static IotHubCommunication IotHubCommunication;
        public static IotEdgeHubCommunication IotEdgeHubCommunication;

        /// <summary>
        /// Shutdown token source.
        /// </summary>
        public static CancellationTokenSource ShutdownTokenSource;

        /// <summary>
        /// Used as delay in sec when shutting down the application.
        /// </summary>
        public static uint PublisherShutdownWaitPeriod { get; } = 10;

        /// <summary>
        /// Stores startup time.
        /// </summary>
        public static DateTime PublisherStartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Logging object.
        /// </summary>
        public static Serilog.Core.Logger Logger { get; set; } = null;

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
                        // Publisher configuration options
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
                        { "sw|sessionconnectwait=", $"specify the wait time in seconds publisher is trying to connect to disconnected endpoints and starts monitoring unmonitored items\nMin: 10\nDefault: {SessionConnectWaitSec}", (int i) => {
                                if (i > 10)
                                {
                                    SessionConnectWaitSec = i;
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

                        { "ns|noshutdown=", $"same as runforever.\nDefault: {_noShutdown}", (bool b) => _noShutdown = b },
                        { "rf|runforever", $"publisher can not be stopped by pressing a key on the console, but will run forever.\nDefault: {_noShutdown}", b => _noShutdown = b != null },

                        { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './{_logFileName}'", (string l) => _logFileName = l },
                        { "lt|logflushtimespan=", $"the timespan in seconds when the logfile should be flushed.\nDefault: {_logFileFlushTimeSpanSec} sec", (int s) => {
                                if (s > 0)
                                {
                                    _logFileFlushTimeSpanSec = TimeSpan.FromSeconds(s);
                                }
                                else
                                {
                                    throw new Mono.Options.OptionException("The logflushtimespan must be a positive number.", "logflushtimespan");
                                }
                            }
                        },
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
                        // IoTHub specific options
                        { "ih|iothubprotocol=", $"{(IsIotEdgeModule ? "not supported when running as IoT Edge module\n" : $"the protocol to use for communication with Azure IoTHub (allowed values: {string.Join(", ", Enum.GetNames(IotHubProtocol.GetType()))}).\nDefault: {Enum.GetName(IotHubProtocol.GetType(), IotHubProtocol)}")}",
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
                        { "pn|portnum=", $"the server port of the publisher OPC server endpoint.\nDefault: {ServerPort}", (ushort p) => ServerPort = p },
                        { "pa|path=", $"the enpoint URL path part of the publisher OPC server endpoint.\nDefault: '{ServerPath}'", (string a) => ServerPath = a },
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

                        { "aa|autoaccept", $"the publisher trusts all servers it is establishing a connection to.\nDefault: {AutoAcceptCerts}", b => AutoAcceptCerts = b != null },

                        // trust own public cert option
                        { "tm|trustmyself=", $"same as trustowncert.\nDefault: {TrustMyself}", (bool b) => TrustMyself = b  },
                        { "to|trustowncert", $"the publisher certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", t => TrustMyself = t != null  },
                        // read the display name of the nodes to publish from the server and publish them instead of the node id
                        { "fd|fetchdisplayname=", $"same as fetchname.\nDefault: {FetchOpcNodeDisplayName}", (bool b) => FetchOpcNodeDisplayName = IotCentralMode ? true : b },
                        { "fn|fetchname", $"enable to read the display name of a published node from the server. this will increase the runtime.\nDefault: {FetchOpcNodeDisplayName}", b => FetchOpcNodeDisplayName = IotCentralMode ? true : b != null },

                        // cert store options
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

                        { "tp|trustedcertstorepath=", $"the path of the trusted cert store\nDefault: '{OpcTrustedCertDirectoryStorePathDefault}'", (string s) => OpcTrustedCertStorePath = s },

                        { "rp|rejectedcertstorepath=", $"the path of the rejected cert store\nDefault '{OpcRejectedCertDirectoryStorePathDefault}'", (string s) => OpcRejectedCertStorePath = s },

                        { "ip|issuercertstorepath=", $"the path of the trusted issuer cert store\nDefault '{OpcIssuerCertDirectoryStorePathDefault}'", (string s) => OpcIssuerCertStorePath = s },

                        { "csr", $"show data to create a certificate signing request\nDefault '{ShowCreateSigningRequestInfo}'", c => ShowCreateSigningRequestInfo = c != null },

                        { "ab|applicationcertbase64=", $"update/set this applications certificate with the certificate passed in as bas64 string", (string s) =>
                            {
                                NewCertificateBase64String = s;
                            }
                        },
                        { "af|applicationcertfile=", $"update/set this applications certificate with the certificate file specified", (string s) =>
                            {
                                if (File.Exists(s))
                                {
                                    NewCertificateFileName = s;
                                }
                                else
                                {
                                    throw new OptionException("The file '{s}' does not exist.", "applicationcertfile");
                                }
                            }
                        },

                        { "pb|privatekeybase64=", $"initial provisioning of the application certificate (with a PEM or PFX fomat) requires a private key passed in as base64 string", (string s) =>
                            {
                                PrivateKeyBase64String = s;
                            }
                        },
                        { "pk|privatekeyfile=", $"initial provisioning of the application certificate (with a PEM or PFX fomat) requires a private key passed in as file", (string s) =>
                            {
                                if (File.Exists(s))
                                {
                                    PrivateKeyFileName = s;
                                }
                                else
                                {
                                    throw new OptionException("The file '{s}' does not exist.", "privatekeyfile");
                                }
                            }
                        },

                        { "cp|certpassword=", $"the optional password for the PEM or PFX or the installed application certificate", (string s) =>
                            {
                                CertificatePassword = s;
                            }
                        },

                        { "tb|addtrustedcertbase64=", $"adds the certificate to the applications trusted cert store passed in as base64 string (multiple strings supported)", (string s) =>
                            {
                                TrustedCertificateBase64Strings = ParseListOfStrings(s);
                            }
                        },
                        { "tf|addtrustedcertfile=", $"adds the certificate file(s) to the applications trusted cert store passed in as base64 string (multiple filenames supported)", (string s) =>
                            {
                                TrustedCertificateFileNames = ParseListOfFileNames(s, "addtrustedcertfile");
                            }
                        },

                        { "ib|addissuercertbase64=", $"adds the specified issuer certificate to the applications trusted issuer cert store passed in as base64 string (multiple strings supported)", (string s) =>
                            {
                                IssuerCertificateBase64Strings = ParseListOfStrings(s);
                            }
                        },
                        { "if|addissuercertfile=", $"adds the specified issuer certificate file(s) to the applications trusted issuer cert store (multiple filenames supported)", (string s) =>
                            {
                                IssuerCertificateFileNames = ParseListOfFileNames(s, "addissuercertfile");
                            }
                        },

                        { "rb|updatecrlbase64=", $"update the CRL passed in as base64 string to the corresponding cert store (trusted or trusted issuer)", (string s) =>
                            {
                                CrlBase64String = s;
                            }
                        },
                        { "uc|updatecrlfile=", $"update the CRL passed in as file to the corresponding cert store (trusted or trusted issuer)", (string s) =>
                            {
                                if (File.Exists(s))
                                {
                                    CrlFileName = s;
                                }
                                else
                                {
                                    throw new OptionException("The file '{s}' does not exist.", "updatecrlfile");
                                }
                            }
                        },

                        { "rc|removecert=", $"remove cert(s) with the given thumbprint(s) (multiple thumbprints supported)", (string s) =>
                            {
                                ThumbprintsToRemove = ParseListOfStrings(s);
                            }
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

                        // all the following are only supported to not break existing command lines, but some of them are just ignored
                        { "st|opcstacktracemask=", $"ignored, only supported for backward comaptibility.", i => {}},
                        { "sd|shopfloordomain=", $"same as site option, only there for backward compatibility\n" +
                                "The value must follow the syntactical rules of a DNS hostname.\nDefault: not set", (string s) => {
                                Regex siteNameRegex = new Regex("^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$");
                                if (siteNameRegex.IsMatch(s))
                                {
                                    PublisherSite = s;
                                }
                                else
                                {
                                    throw new OptionException("The shopfloor domain is not a valid DNS hostname.", "shopfloordomain");
                                }
                            }
                         },
                        { "vc|verboseconsole=", $"ignored, only supported for backward comaptibility.", b => {}},
                        { "as|autotrustservercerts=", $"same as autoaccept, only supported for backward cmpatibility.\nDefault: {AutoAcceptCerts}", (bool b) => AutoAcceptCerts = b },
                        { "tt|trustedcertstoretype=", $"ignored, only supported for backward compatibility. the trusted cert store will always reside in a directory.", s => { }},
                        { "rt|rejectedcertstoretype=", $"ignored, only supported for backward compatibility. the rejected cert store will always reside in a directory.", s => { }},
                        { "it|issuercertstoretype=", $"ignored, only supported for backward compatibility. the trusted issuer cert store will always reside in a directory.", s => { }},

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
                OpcApplicationConfiguration opcApplicationConfiguration = new OpcApplicationConfiguration();
                await opcApplicationConfiguration.ConfigureAsync();

                // log shopfloor site setting
                if (string.IsNullOrEmpty(PublisherSite))
                {
                    Logger.Information("There is no site configured.");
                }
                else
                {
                    Logger.Information($"Publisher is in site '{PublisherSite}'.");
                }

                // start our server interface
                try
                {
                    Logger.Information($"Starting server on endpoint {OpcApplicationConfiguration.ApplicationConfiguration.ServerConfiguration.BaseAddresses[0].ToString()} ...");
                    _publisherServer = new PublisherServer();
                    _publisherServer.Start(OpcApplicationConfiguration.ApplicationConfiguration);
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

                // kick off OPC session creation and node monitoring
                await SessionStartAsync();

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

                // stop the server
                _publisherServer.Stop();

                // shutdown all OPC sessions
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
        /// Start all sessions.
        /// </summary>
        public async static Task SessionStartAsync()
        {
            try
            {
                await OpcSessionsListSemaphore.WaitAsync();
                OpcSessions.ForEach(s => s.ConnectAndMonitorSession.Set());
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Failed to start all sessions.");
            }
            finally
            {
                OpcSessionsListSemaphore.Release();
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
                Logger.Information($"Publisher is shutting down. Wait {SessionConnectWaitSec} seconds, since there are stil {sessionCount} sessions alive...");
                await Task.Delay(SessionConnectWaitSec * 1000);
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
            Logger.Information("To exit the application, just press CTRL-C while it is running.");
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
                loggerConfiguration.WriteTo.File(_logFileName, fileSizeLimitBytes: MAX_LOGFILE_SIZE, flushToDiskInterval: _logFileFlushTimeSpanSec, rollOnFileSizeLimit: true, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
            }

            Logger = loggerConfiguration.CreateLogger();
            Logger.Information($"Current directory is: {System.IO.Directory.GetCurrentDirectory()}");
            Logger.Information($"Log file is: {_logFileName}");
            Logger.Information($"Log level is: {_logLevel}");
            return;
        }

        /// <summary>
        /// Helper to build a list of byte arrays out of a comma separated list of base64 strings (optional in double quotes).
        /// </summary>
        private static List<string> ParseListOfStrings(string s)
        {
            List<string> strings = new List<string>();
            if (s[0] == '"' && (s.Count(c => c.Equals('"')) % 2 == 0))
            {
                while (s.Contains('"'))
                {
                    int first = 0;
                    int next = 0;
                    first = s.IndexOf('"', next);
                    next = s.IndexOf('"', ++first);
                    strings.Add(s.Substring(first, next - first));
                    s = s.Substring(++next);
                }
            }
            else if (s.Contains(','))
            {
                strings = s.Split(',').ToList();
                strings.ForEach(st => st.Trim());
                strings = strings.Select(st => st.Trim()).ToList();
            }
            else
            {
                strings.Add(s);
            }
            return strings;
        }

        /// <summary>
        /// Helper to build a list of filenames out of a comma separated list of filenames (optional in double quotes).
        /// </summary>
        private static List<string> ParseListOfFileNames(string s, string option)
        {
            List<string> fileNames = new List<string>();
            if (s[0] == '"' && (s.Count(c => c.Equals('"')) % 2 == 0))
            {
                while (s.Contains('"'))
                {
                    int first = 0;
                    int next = 0;
                    first = s.IndexOf('"', next);
                    next = s.IndexOf('"', ++first);
                    var fileName = s.Substring(first, next - first);
                    if (File.Exists(fileName))
                    {
                        fileNames.Add(fileName);
                    }
                    else
                    {
                        throw new OptionException($"The file '{fileName}' does not exist.", option);
                    }
                    s = s.Substring(++next);
                }
            }
            else if (s.Contains(','))
            {
                List<string> parsedFileNames = s.Split(',').ToList();
                parsedFileNames = parsedFileNames.Select(st => st.Trim()).ToList();
                foreach (var fileName in parsedFileNames)
                {
                    if (File.Exists(fileName))
                    {
                        fileNames.Add(fileName);
                    }
                    else
                    {
                        throw new OptionException($"The file '{fileName}' does not exist.", option);
                    }

                }
            }
            else
            {
                if (File.Exists(s))
                {
                    fileNames.Add(s);
                }
                else
                {
                    throw new OptionException($"The file '{s}' does not exist.", option);
                }
            }
            return fileNames;
        }

        private static PublisherServer _publisherServer;
        private static bool _noShutdown = false;
        private static bool _installOnly = false;
        private static string _logFileName = $"{Utils.GetHostName()}-publisher.log";
        private static string _logLevel = "info";
        private static TimeSpan _logFileFlushTimeSpanSec = TimeSpan.FromSeconds(30);
    }
}

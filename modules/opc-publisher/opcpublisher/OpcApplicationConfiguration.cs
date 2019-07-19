using Opc.Ua;
using System;

namespace OpcPublisher
{
    using System.Globalization;
    using System.Threading.Tasks;
    using static Program;

    /// <summary>
    /// Class for OPC Application configuration.
    /// </summary>
    public partial class OpcApplicationConfiguration
    {
        /// <summary>
        /// Configuration info for the OPC application.
        /// </summary>
        public static ApplicationConfiguration ApplicationConfiguration { get; private set; }
        public static string Hostname
        {
            get => _hostname;
#pragma warning disable CA1308 // Normalize strings to uppercase
            set => _hostname = value.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        public static string ApplicationName { get; set; } = "publisher";
        public static string ApplicationUri => $"urn:{Hostname}:{ApplicationName}:microsoft:";
        public static string ProductUri => $"https://github.com/azure-samples/iot-edge-opc-publisher";
        public static ushort ServerPort { get; set; } = 62222;
        public static string ServerPath { get; set; } = "/UA/Publisher";

        /// <summary>
        /// Default endpoint security of the application.
        /// </summary>
        public static string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

        /// <summary>
        /// Enables unsecure endpoint access to the application.
        /// </summary>
        public static bool EnableUnsecureTransport { get; set; } = false;

        /// <summary>
        /// Sets the LDS registration interval.
        /// </summary>
        public static int LdsRegistrationInterval { get; set; } = 0;

        /// <summary>
        /// Set the max string length the OPC stack supports.
        /// </summary>
        public static int OpcMaxStringLength { get; set; } = HubCommunicationBase.MaxResponsePayloadLength;

        /// <summary>
        /// Mapping of the application logging levels to OPC stack logging levels.
        /// </summary>
        public static int OpcTraceToLoggerVerbose { get; set; } = 0;
        public static int OpcTraceToLoggerDebug { get; set; } = 0;
        public static int OpcTraceToLoggerInformation { get; set; } = 0;
        public static int OpcTraceToLoggerWarning { get; set; } = 0;
        public static int OpcTraceToLoggerError { get; set; } = 0;
        public static int OpcTraceToLoggerFatal { get; set; } = 0;

        /// <summary>
        /// Set the OPC stack log level.
        /// </summary>
        public static int OpcStackTraceMask { get; set; } = Utils.TraceMasks.Error | Utils.TraceMasks.Security | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop;

        /// <summary>
        /// Timeout for OPC operations.
        /// </summary>
        public static int OpcOperationTimeout { get; set; } = 120000;


        public static uint OpcSessionCreationTimeout { get; set; } = 10;

        public static uint OpcSessionCreationBackoffMax { get; set; } = 5;

        public static uint OpcKeepAliveDisconnectThreshold { get; set; } = 5;

        public static int OpcKeepAliveIntervalInSec { get; set; } = 2;


        public const int OpcSamplingIntervalDefault = 1000;

        public static int OpcSamplingInterval { get; set; } = OpcSamplingIntervalDefault;

        public const int OpcPublishingIntervalDefault = 0;

        public static int OpcPublishingInterval { get; set; } = OpcPublishingIntervalDefault;

        public static string PublisherServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;



        /// <summary>
        /// Ctor of the OPC application configuration.
        /// </summary>
        public OpcApplicationConfiguration()
        {
        }

        /// <summary>
        /// Configures all OPC stack settings.
        /// </summary>
        public async Task<ApplicationConfiguration> ConfigureAsync()
        {
            // instead of using a configuration XML file, we configure everything programmatically

            // passed in as command line argument
            ApplicationConfiguration = new ApplicationConfiguration();
            ApplicationConfiguration.ApplicationName = ApplicationName;
            ApplicationConfiguration.ApplicationUri = ApplicationUri;
            ApplicationConfiguration.ProductUri = ProductUri;
            ApplicationConfiguration.ApplicationType = ApplicationType.ClientAndServer;

            // configure OPC stack tracing
            ApplicationConfiguration.TraceConfiguration = new TraceConfiguration();
            ApplicationConfiguration.TraceConfiguration.TraceMasks = OpcStackTraceMask;
            ApplicationConfiguration.TraceConfiguration.ApplySettings();
            Utils.Tracing.TraceEventHandler += new EventHandler<TraceEventArgs>(LoggerOpcUaTraceHandler);
            Logger.Information($"opcstacktracemask set to: 0x{OpcStackTraceMask:X}");

            // configure transport settings
            ApplicationConfiguration.TransportQuotas = new TransportQuotas();
            ApplicationConfiguration.TransportQuotas.MaxStringLength = OpcMaxStringLength;
            ApplicationConfiguration.TransportQuotas.MaxMessageSize = 4 * 1024 * 1024;

            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            ApplicationConfiguration.TransportQuotas.OperationTimeout = OpcOperationTimeout;
            Logger.Information($"OperationTimeout set to {ApplicationConfiguration.TransportQuotas.OperationTimeout}");

            // configure OPC UA server
            ApplicationConfiguration.ServerConfiguration = new ServerConfiguration();

            // configure server base addresses
            if (ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // we do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                ApplicationConfiguration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Hostname}:{ServerPort}{ServerPath}");
            }
            foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
            {
                Logger.Information($"OPC UA server base address: {endpoint}");
            }

            // by default use high secure transport
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Logger.Information($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // add none secure transport on request
            if (EnableUnsecureTransport)
            {
                newPolicy = new ServerSecurityPolicy
                {
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None
                };
                ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
                Logger.Information($"Unsecure security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");
                Logger.Warning($"Note: This is a security risk and needs to be disabled for production use");
            }

            // add default client configuration
            ApplicationConfiguration.ClientConfiguration = new ClientConfiguration();

            // security configuration
            await InitApplicationSecurityAsync().ConfigureAwait(false);

            // set LDS registration interval
            ApplicationConfiguration.ServerConfiguration.MaxRegistrationInterval = LdsRegistrationInterval;
            Logger.Information($"LDS(-ME) registration intervall set to {LdsRegistrationInterval} ms (0 means no registration)");

            // show certificate store information
            await ShowCertificateStoreInformationAsync().ConfigureAwait(false);

            return ApplicationConfiguration;
        }

        /// <summary>
        /// Event handler to log OPC UA stack trace messages into own logger.
        /// </summary>
        private static void LoggerOpcUaTraceHandler(object sender, TraceEventArgs e)
        {
            // return fast if no trace needed
            if ((e.TraceMask & OpcStackTraceMask) == 0)
            {
                return;
            }

            // e.Exception and e.Message are always null

            // format the trace message
            string message = string.Empty;
            message = string.Format(CultureInfo.InvariantCulture, e.Format, e.Arguments).Trim();
            message = "OPC: " + message;

            // map logging level
            if ((e.TraceMask & OpcTraceToLoggerVerbose) != 0)
            {
                Logger.Verbose(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerDebug) != 0)
            {
                Logger.Debug(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerInformation) != 0)
            {
                Logger.Information(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerWarning) != 0)
            {
                Logger.Warning(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerError) != 0)
            {
                Logger.Error(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerFatal) != 0)
            {
                Logger.Fatal(message);
                return;
            }
            return;
        }

#pragma warning disable CA1308 // Normalize strings to uppercase
        private static string _hostname = $"{Utils.GetHostName().ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase
    }
}

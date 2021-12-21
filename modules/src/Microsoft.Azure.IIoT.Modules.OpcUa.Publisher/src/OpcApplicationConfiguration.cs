using Opc.Ua;
using System;

namespace OpcPublisher
{
    using Opc.Ua.Configuration;
    using System.Globalization;
    using System.Text;
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
        public static string ProductUri => $"https://github.com/Azure/Industrial-IoT";
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
            // instead of using a configuration XML file, configure everything programmatically
            var application = new ApplicationInstance() {
                ApplicationName = ApplicationName,
                ApplicationType = ApplicationType.ClientAndServer
            };

            // configure transport settings
            Logger.Information($"OperationTimeout set to {OpcOperationTimeout}");
            var transportQuotas = new TransportQuotas {
                MaxStringLength = OpcMaxStringLength,
                MaxMessageSize = 4 * 1024 * 1024,
                // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
                OperationTimeout = OpcOperationTimeout
            };

            // configure OPC UA server
            var serverBuilder = application.Build(ApplicationUri, ProductUri)
                .SetTransportQuotas(transportQuotas)
                .AsServer(new string[] { $"opc.tcp://{Hostname}:{ServerPort}{ServerPath}" })
                .AddSignAndEncryptPolicies()
                .AddSignPolicies();

            // use backdoor to access app config used by builder
            ApplicationConfiguration = application.ApplicationConfiguration;

            if (EnableUnsecureTransport)
            {
                serverBuilder.AddUnsecurePolicyNone();
            }

            // LDS registration interval
            serverBuilder.SetMaxRegistrationInterval(LdsRegistrationInterval);

            foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
            {
                Logger.Information($"OPC UA server base address: {endpoint}");
            }

            foreach (var policy in ApplicationConfiguration.ServerConfiguration.SecurityPolicies)
            {
                Logger.Information($"Security policy {policy.SecurityPolicyUri} with mode {policy.SecurityMode} added");
                if (policy.SecurityMode == MessageSecurityMode.None)
                {
                    Logger.Warning($"Note: This is a security risk and needs to be disabled for production use");
                }
            }

            Logger.Information($"LDS(-ME) registration intervall set to {LdsRegistrationInterval} ms (0 means no registration)");

            // add default client configuration
            var securityBuilder = serverBuilder.AsClient();

            // security configuration
            ApplicationConfiguration = await InitApplicationSecurityAsync(securityBuilder).ConfigureAwait(false);

            // configure OPC stack tracing
            Utils.SetTraceMask(OpcStackTraceMask);
            Utils.Tracing.TraceEventHandler += LoggerOpcUaTraceHandler;
            Logger.Information($"opcstacktracemask set to: 0x{OpcStackTraceMask:X}");

            var certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            if (certificate == null)
            {
                Logger.Information($"No existing Application certificate found. Create a self-signed Application certificate valid from yesterday for {CertificateFactory.DefaultLifeTime} months,");
                Logger.Information($"with a {CertificateFactory.DefaultKeySize} bit key and {CertificateFactory.DefaultHashSize} bit hash.");
            }
            else
            {
                Logger.Information($"Application certificate with thumbprint '{certificate.Thumbprint}' found in the application certificate store.");
            }

            bool certOk = await application.CheckApplicationInstanceCertificate(true, CertificateFactory.DefaultKeySize, CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
            if (!certOk)
            {
                throw new Exception("Application certificate invalid.");
            }

            if (certificate == null)
            {
                certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
                Logger.Information($"Application certificate with thumbprint '{certificate.Thumbprint}' created.");
            }

            Logger.Information($"Application certificate is for ApplicationUri '{ApplicationConfiguration.ApplicationUri}', ApplicationName '{ApplicationConfiguration.ApplicationName}' and Subject is '{ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Subject}'");

            // show CreateSigningRequest data
            if (ShowCreateSigningRequestInfo)
            {
                await ShowCreateSigningRequestInformationAsync(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate).ConfigureAwait(false);
            }

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

            // e.Exception and e.Message are special
            if (e.Exception != null)
            {
                Logger.Error(e.Exception, e.Format, e.Arguments);
                return;
            }

            // format the trace message
            var builder = new StringBuilder("OPC: ");
            builder.AppendFormat(CultureInfo.InvariantCulture, e.Format, e.Arguments);
            var message = builder.ToString().Trim();

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

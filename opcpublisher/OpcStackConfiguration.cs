
using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPublisher
{
    using System.Threading.Tasks;
    using static Opc.Ua.CertificateStoreType;
    using static Program;

    public class OpcStackConfiguration
    {
        public static ApplicationConfiguration PublisherOpcApplicationConfiguration { get; private set; }
        public static string ApplicationName { get; set; } = "publisher";

        public static ushort PublisherServerPort { get; set; } = 62222;

        public static string PublisherServerPath { get; set; } = "/UA/Publisher";

        public static int OpcMaxStringLength { get; set; } = 1024 * 1024;

        public static int OpcOperationTimeout { get; set; } = 120000;

        public static bool TrustMyself { get; set; } = true;

        public static int OpcStackTraceMask { get; set; } = Utils.TraceMasks.Error | Utils.TraceMasks.Security | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop;

        public static bool OpcPublisherAutoTrustServerCerts { get; set; } = false;

        public static uint OpcSessionCreationTimeout { get; set; } = 10;

        public static uint OpcSessionCreationBackoffMax { get; set; } = 5;

        public static uint OpcKeepAliveDisconnectThreshold { get; set; } = 5;

        public static int OpcKeepAliveIntervalInSec { get; set; } = 2;


        public const int OpcSamplingIntervalDefault = 1000;

        public static int OpcSamplingInterval { get; set; } = OpcSamplingIntervalDefault;


        public const int OpcPublishingIntervalDefault = 0;

        public static int OpcPublishingInterval { get; set; } = OpcPublishingIntervalDefault;

        public static string PublisherServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

        public static string OpcOwnCertStoreType { get; set; } = X509Store;


        public static string OpcOwnCertDirectoryStorePathDefault => "CertificateStores/own";

        public static string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";

        public static string OpcOwnCertStorePath { get; set; } = OpcOwnCertX509StorePathDefault;

        public static string OpcTrustedCertStoreType { get; set; } = Directory;

        public static string OpcTrustedCertDirectoryStorePathDefault => "CertificateStores/trusted";

        public static string OpcTrustedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";

        public static string OpcTrustedCertStorePath { get; set; } = null;

        public static string OpcRejectedCertStoreType { get; set; } = Directory;

        public static string OpcRejectedCertDirectoryStorePathDefault => "CertificateStores/rejected";

        public static string OpcRejectedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";

        public static string OpcRejectedCertStorePath { get; set; } = OpcRejectedCertDirectoryStorePathDefault;

        public static string OpcIssuerCertStoreType { get; set; } = Directory;


        public static string OpcIssuerCertDirectoryStorePathDefault => "CertificateStores/issuers";

        public static string OpcIssuerCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";

        public static string OpcIssuerCertStorePath { get; set; } = OpcIssuerCertDirectoryStorePathDefault;

        public static int LdsRegistrationInterval { get; set; } = 0;

        public static int OpcTraceToLoggerVerbose { get; set; } = 0;
        public static int OpcTraceToLoggerDebug { get; set; } = 0;
        public static int OpcTraceToLoggerInformation { get; set; } = 0;
        public static int OpcTraceToLoggerWarning { get; set; } = 0;
        public static int OpcTraceToLoggerError { get; set; } = 0;
        public static int OpcTraceToLoggerFatal { get; set; } = 0;

        /// <summary>
        /// Configures all OPC stack settings
        /// </summary>
        public async Task ConfigureAsync()
        {
            // Instead of using a Config.xml we configure everything programmatically.

            //
            // OPC UA Application configuration
            //
            PublisherOpcApplicationConfiguration = new ApplicationConfiguration();

            // Passed in as command line argument
            PublisherOpcApplicationConfiguration.ApplicationName = ApplicationName;
            PublisherOpcApplicationConfiguration.ApplicationUri = $"urn:{Utils.GetHostName()}:{PublisherOpcApplicationConfiguration.ApplicationName}:microsoft:";
            PublisherOpcApplicationConfiguration.ProductUri = "https://github.com/Azure/iot-edge-opc-publisher";
            PublisherOpcApplicationConfiguration.ApplicationType = ApplicationType.ClientAndServer;


            //
            // Security configuration
            //
            PublisherOpcApplicationConfiguration.SecurityConfiguration = new SecurityConfiguration();

            // Application certificate
            PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType = OpcOwnCertStoreType;
            PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath = OpcOwnCertStorePath;
            PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = PublisherOpcApplicationConfiguration.ApplicationName;
            Logger.Information($"Application Certificate store type is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType}");
            Logger.Information($"Application Certificate store path is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath}");
            Logger.Information($"Application Certificate subject name is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName}");

            // Use existing certificate, if it is there.
            X509Certificate2 certificate = await PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Find(true);
            if (certificate == null)
            {
                Logger.Information($"No existing Application certificate found. Create a self-signed Application certificate valid from yesterday for {CertificateFactory.defaultLifeTime} months,");
                Logger.Information($"with a {CertificateFactory.defaultKeySize} bit key and {CertificateFactory.defaultHashSize} bit hash.");
                certificate = CertificateFactory.CreateCertificate(
                    PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    PublisherOpcApplicationConfiguration.ApplicationUri,
                    PublisherOpcApplicationConfiguration.ApplicationName,
                    PublisherOpcApplicationConfiguration.ApplicationName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null
                    );
                PublisherOpcApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate ?? throw new Exception("OPC UA application certificate can not be created! Cannot continue without it!");
            }
            else
            {
                Logger.Information("Application certificate found in Application Certificate Store");
            }
            PublisherOpcApplicationConfiguration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            Logger.Information($"Application certificate is for Application URI '{PublisherOpcApplicationConfiguration.ApplicationUri}', Application '{PublisherOpcApplicationConfiguration.ApplicationName} and has Subject '{PublisherOpcApplicationConfiguration.ApplicationName}'");

            // TrustedIssuerCertificates
            PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = OpcIssuerCertStoreType;
            PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = OpcIssuerCertStorePath;
            Logger.Information($"Trusted Issuer store type is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Logger.Information($"Trusted Issuer Certificate store path is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // TrustedPeerCertificates
            PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType = OpcTrustedCertStoreType;
            if (string.IsNullOrEmpty(OpcTrustedCertStorePath))
            {
                // Set default.
                PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertStoreType == X509Store ? OpcTrustedCertX509StorePathDefault : OpcTrustedCertDirectoryStorePathDefault;
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_TPC_SP")))
                {
                    // Use environment variable.
                    PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Environment.GetEnvironmentVariable("_TPC_SP");
                }
            }
            else
            {
                PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertStorePath;
            }
            Logger.Information($"Trusted Peer Certificate store type is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType}");
            Logger.Information($"Trusted Peer Certificate store path is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");

            // RejectedCertificateStore
            PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType = OpcRejectedCertStoreType;
            PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath = OpcRejectedCertStorePath;
            Logger.Information($"Rejected certificate store type is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Logger.Information($"Rejected Certificate store path is: {PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            // AutoAcceptUntrustedCertificates
            // This is a security risk and should be set to true only for debugging purposes.
            PublisherOpcApplicationConfiguration.SecurityConfiguration.AutoAcceptUntrustedCertificates = false;

            // RejectSHA1SignedCertificates
            // We allow SHA1 certificates for now as many OPC Servers still use them
            PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates = false;
            Logger.Information($"Rejection of SHA1 signed certificates is {(PublisherOpcApplicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");

            // MinimunCertificatesKeySize
            // We allow a minimum key size of 1024 bit, as many OPC UA servers still use them
            PublisherOpcApplicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize = 1024;
            Logger.Information($"Minimum certificate key size set to {PublisherOpcApplicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize}");

            // We make the default reference stack behavior configurable to put our own certificate into the trusted peer store.
            if (TrustMyself)
            {
                // Ensure it is trusted
                try
                {
                    ICertificateStore store = PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                    if (store == null)
                    {
                        Logger.Information($"Can not open trusted peer store. StorePath={PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                    }
                    else
                    {
                        try
                        {
                            Logger.Information($"Adding publisher certificate to trusted peer store. StorePath={PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                            X509Certificate2 publicKey = new X509Certificate2(certificate.RawData);
                            await store.Add(publicKey);
                        }
                        finally
                        {
                            store.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Can not add publisher certificate to trusted peer store. StorePath={PublisherOpcApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                }
            }
            else
            {
                Logger.Information("Publisher certificate is not added to trusted peer store.");
            }


            //
            // TransportConfigurations
            //

            PublisherOpcApplicationConfiguration.TransportQuotas = new TransportQuotas();
            PublisherOpcApplicationConfiguration.TransportQuotas.MaxByteStringLength = 4 * 1024 * 1024;
            PublisherOpcApplicationConfiguration.TransportQuotas.MaxMessageSize = 4 * 1024 * 1024;

            // the maximum string length could be set to ajust for large number of nodes when reading the list of published nodes
            PublisherOpcApplicationConfiguration.TransportQuotas.MaxStringLength = OpcMaxStringLength;
            
            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            PublisherOpcApplicationConfiguration.TransportQuotas.OperationTimeout = OpcOperationTimeout;
            Logger.Information($"OperationTimeout set to {PublisherOpcApplicationConfiguration.TransportQuotas.OperationTimeout}");


            //
            // ServerConfiguration
            //
            PublisherOpcApplicationConfiguration.ServerConfiguration = new ServerConfiguration();

            // BaseAddresses
            if (PublisherOpcApplicationConfiguration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // We do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                PublisherOpcApplicationConfiguration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Utils.GetHostName()}:{PublisherServerPort}{PublisherServerPath}");
            }
            foreach (var endpoint in PublisherOpcApplicationConfiguration.ServerConfiguration.BaseAddresses)
            {
                Logger.Information($"Publisher server base address: {endpoint}");
            }

            // SecurityPolicies
            // We do not allow security policy SecurityPolicies.None, but always high security
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            PublisherOpcApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Logger.Information($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // MaxRegistrationInterval
            PublisherOpcApplicationConfiguration.ServerConfiguration.MaxRegistrationInterval = LdsRegistrationInterval;
            Logger.Information($"LDS(-ME) registration intervall set to {LdsRegistrationInterval} ms (0 means no registration)");

            //
            // TraceConfiguration
            //
            //
            // TraceConfiguration
            //
            PublisherOpcApplicationConfiguration.TraceConfiguration = new TraceConfiguration();
            PublisherOpcApplicationConfiguration.TraceConfiguration.TraceMasks = OpcStackTraceMask;
            PublisherOpcApplicationConfiguration.TraceConfiguration.ApplySettings();
            Utils.Tracing.TraceEventHandler += new EventHandler<TraceEventArgs>(LoggerOpcUaTraceHandler);
            Logger.Information($"opcstacktracemask set to: 0x{OpcStackTraceMask:X}");

            // add default client configuration
            PublisherOpcApplicationConfiguration.ClientConfiguration = new ClientConfiguration();

            // validate the configuration now
            await PublisherOpcApplicationConfiguration.Validate(PublisherOpcApplicationConfiguration.ApplicationType);
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
            message = string.Format(e.Format, e.Arguments)?.Trim();
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
    }
}

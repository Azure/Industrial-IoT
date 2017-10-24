
using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPublisher
{
    using System.Threading.Tasks;
    using static Opc.Ua.CertificateStoreType;
    using static OpcPublisher.Workarounds.TraceWorkaround;

    public class OpcStackConfiguration
    {

        public static string ApplicationName
        {
            get => _applicationName;
            set => _applicationName = value;
        }
        private static string _applicationName = "publisher";

        public static string LogFileName
        {
            get => _logFileName;
            set => _logFileName = value;
        }
        private static string _logFileName;

        public static ushort PublisherServerPort
        {
            get => _publisherServerPort;
            set => _publisherServerPort = value;
        }
        private static ushort _publisherServerPort = 62222;

        public static string PublisherServerPath
        {
            get => _publisherServerPath;
            set => _publisherServerPath = value;
        }
        private static string _publisherServerPath = "/UA/Publisher";

        public static int OpcOperationTimeout
        {
            get => _opcOperationTimeout;
            set => _opcOperationTimeout = value;
        }
        private static int _opcOperationTimeout = 120000;

        public static bool TrustMyself
        {
            get => _trustMyself;
            set => _trustMyself = value;
        }
        private static bool _trustMyself = true;

        // Enable Utils.TraceMasks.OperationDetail to get output for IoTHub telemetry operations. Current: 0x287 (647), with OperationDetail: 0x2C7 (711)
        public static int OpcStackTraceMask
        {
            get => _opcStackTraceMask;
            set => _opcStackTraceMask = value;
        }
        private static int _opcStackTraceMask = Utils.TraceMasks.Error | Utils.TraceMasks.Security | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop;

        public static bool OpcPublisherAutoTrustServerCerts
        {
            get => _opcPublisherAutoTrustServerCerts;
            set => _opcPublisherAutoTrustServerCerts = value;
        }
        private static bool _opcPublisherAutoTrustServerCerts = false;

        public static uint OpcSessionCreationTimeout
        {
            get => _opcSessionCreationTimeout;
            set => _opcSessionCreationTimeout = value;
        }
        private static uint _opcSessionCreationTimeout = 10;

        public static uint OpcSessionCreationBackoffMax
        {
            get => _opcSessionCreationBackoffMax;
            set => _opcSessionCreationBackoffMax = value;
        }
        private static uint _opcSessionCreationBackoffMax = 5;

        public static uint OpcKeepAliveDisconnectThreshold
        {
            get => _opcKeepAliveDisconnectThreshold;
            set => _opcKeepAliveDisconnectThreshold = value;
        }
        private static uint _opcKeepAliveDisconnectThreshold = 5;

        public static int OpcKeepAliveIntervalInSec
        {
            get => _opcKeepAliveIntervalInSec;
            set => _opcKeepAliveIntervalInSec = value;
        }
        private static int _opcKeepAliveIntervalInSec = 2;

        public static int OpcSamplingInterval
        {
            get => _opcSamplingInterval;
            set => _opcSamplingInterval = value;
        }
        private static int _opcSamplingInterval = 1000;

        public static int OpcPublishingInterval
        {
            get => _opcPublishingInterval;
            set => _opcPublishingInterval = value;
        }
        private static int _opcPublishingInterval = 0;

        public static string PublisherServerSecurityPolicy
        {
            get => _publisherServerSecurityPolicy;
            set => _publisherServerSecurityPolicy = value;
        }
        private static string _publisherServerSecurityPolicy = SecurityPolicies.Basic128Rsa15;

        public static string OpcOwnCertStoreType
        {
            get => _opcOwnCertStoreType;
            set => _opcOwnCertStoreType = value;
        }
        private static string _opcOwnCertStoreType = X509Store;

        public static string OpcOwnCertDirectoryStorePathDefault => "CertificateStores/own";
        public static string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcOwnCertStorePath
        {
            get => _opcOwnCertStorePath;
            set => _opcOwnCertStorePath = value;
        }
        private static string _opcOwnCertStorePath = OpcOwnCertX509StorePathDefault;

        public static string OpcTrustedCertStoreType
        {
            get => _opcTrustedCertStoreType;
            set => _opcTrustedCertStoreType = value;
        }
        private static string _opcTrustedCertStoreType = CertificateStoreType.Directory;

        public static string OpcTrustedCertDirectoryStorePathDefault => "CertificateStores/trusted";
        public static string OpcTrustedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcTrustedCertStorePath
        {
            get => _opcTrustedCertStorePath;
            set => _opcTrustedCertStorePath = value;
        }
        private static string _opcTrustedCertStorePath = null;

        public static string OpcRejectedCertStoreType
        {
            get => _opcRejectedCertStoreType;
            set => _opcRejectedCertStoreType = value;
        }
        private static string _opcRejectedCertStoreType = CertificateStoreType.Directory;

        public static string OpcRejectedCertDirectoryStorePathDefault => "CertificateStores/rejected";
        public static string OpcRejectedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcRejectedCertStorePath
        {
            get => _opcRejectedCertStorePath;
            set => _opcRejectedCertStorePath = value;
        }
        private static string _opcRejectedCertStorePath = OpcRejectedCertDirectoryStorePathDefault;

        public static string OpcIssuerCertStoreType
        {
            get => _opcIssuerCertStoreType;
            set => _opcIssuerCertStoreType = value;
        }
        private static string _opcIssuerCertStoreType = CertificateStoreType.Directory;

        public static string OpcIssuerCertDirectoryStorePathDefault => "CertificateStores/issuers";
        public static string OpcIssuerCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcIssuerCertStorePath
        {
            get => _opcIssuerCertStorePath;
            set => _opcIssuerCertStorePath = value;
        }
        private static string _opcIssuerCertStorePath = OpcIssuerCertDirectoryStorePathDefault;

        public static int LdsRegistrationInterval
        {
            get => _ldsRegistrationInterval;
            set => _ldsRegistrationInterval = value;
        }
        private static int _ldsRegistrationInterval = 0;

        public static ApplicationConfiguration PublisherOpcApplicationConfiguration
        {
            get => _configuration;
        }
        private static ApplicationConfiguration _configuration;

        /// <summary>
        /// Configures all OPC stack settings
        /// </summary>
        public async Task ConfigureAsync()
        {
            // Instead of using a Config.xml we configure everything programmatically.

            //
            // OPC UA Application configuration
            //
            _configuration = new ApplicationConfiguration();

            // Passed in as command line argument
            _configuration.ApplicationName = _applicationName;
            _configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:{_configuration.ApplicationName}:microsoft:";
            _configuration.ProductUri = "https://github.com/Azure/iot-edge-opc-publisher";
            _configuration.ApplicationType = ApplicationType.ClientAndServer;


            //
            // Security configuration
            //
            _configuration.SecurityConfiguration = new SecurityConfiguration();

            // Application certificate
            _configuration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            _configuration.SecurityConfiguration.ApplicationCertificate.StoreType = _opcOwnCertStoreType;
            _configuration.SecurityConfiguration.ApplicationCertificate.StorePath = _opcOwnCertStorePath;
            _configuration.SecurityConfiguration.ApplicationCertificate.SubjectName = _configuration.ApplicationName;
            Trace($"Application Certificate store type is: {_configuration.SecurityConfiguration.ApplicationCertificate.StoreType}");
            Trace($"Application Certificate store path is: {_configuration.SecurityConfiguration.ApplicationCertificate.StorePath}");
            Trace($"Application Certificate subject name is: {_configuration.SecurityConfiguration.ApplicationCertificate.SubjectName}");

            // Use existing certificate, if it is there.
            X509Certificate2 certificate = await _configuration.SecurityConfiguration.ApplicationCertificate.Find(true);
            if (certificate == null)
            {
                Trace($"No existing Application certificate found. Create a self-signed Application certificate valid from yesterday for {CertificateFactory.defaultLifeTime} months,");
                Trace($"with a {CertificateFactory.defaultKeySize} bit key and {CertificateFactory.defaultHashSize} bit hash.");
                certificate = CertificateFactory.CreateCertificate(
                    _configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    _configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    _configuration.ApplicationUri,
                    _configuration.ApplicationName,
                    _configuration.ApplicationName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null
                    );
                _configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate ?? throw new Exception("OPC UA application certificate could not be created! Cannot continue without it!");
            }
            else
            {
                Trace("Application certificate found in Application Certificate Store");
            }
            _configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            Trace($"Application certificate is for Application URI '{_configuration.ApplicationUri}', Application '{_configuration.ApplicationName} and has Subject '{_configuration.ApplicationName}'");

            // TrustedIssuerCertificates
            _configuration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            _configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = _opcIssuerCertStoreType;
            _configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = _opcIssuerCertStorePath;
            Trace($"Trusted Issuer store type is: {_configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Trace($"Trusted Issuer Certificate store path is: {_configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // TrustedPeerCertificates
            _configuration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            _configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType = _opcTrustedCertStoreType;
            if (string.IsNullOrEmpty(_opcTrustedCertStorePath))
            {
                // Set default.
                _configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = _opcTrustedCertStoreType == X509Store ? OpcTrustedCertX509StorePathDefault : OpcTrustedCertDirectoryStorePathDefault;
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_TPC_SP")))
                {
                    // Use environment variable.
                    _configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Environment.GetEnvironmentVariable("_TPC_SP");
                }
            }
            else
            {
                _configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = _opcTrustedCertStorePath;
            }
            Trace($"Trusted Peer Certificate store type is: {_configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType}");
            Trace($"Trusted Peer Certificate store path is: {_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");

            // RejectedCertificateStore
            _configuration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            _configuration.SecurityConfiguration.RejectedCertificateStore.StoreType = _opcRejectedCertStoreType;
            _configuration.SecurityConfiguration.RejectedCertificateStore.StorePath = _opcRejectedCertStorePath;
            Trace($"Rejected certificate store type is: {_configuration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Trace($"Rejected Certificate store path is: {_configuration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            // AutoAcceptUntrustedCertificates
            // This is a security risk and should be set to true only for debugging purposes.
            _configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates = false;

            // RejectSHA1SignedCertificates
            // We allow SHA1 certificates for now as many OPC Servers still use them
            _configuration.SecurityConfiguration.RejectSHA1SignedCertificates = false;
            Trace($"Rejection of SHA1 signed certificates is {(_configuration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");

            // MinimunCertificatesKeySize
            // We allow a minimum key size of 1024 bit, as many OPC UA servers still use them
            _configuration.SecurityConfiguration.MinimumCertificateKeySize = 1024;
            Trace($"Minimum certificate key size set to {_configuration.SecurityConfiguration.MinimumCertificateKeySize}");

            // We make the default reference stack behavior configurable to put our own certificate into the trusted peer store.
            if (_trustMyself)
            {
                // Ensure it is trusted
                try
                {
                    ICertificateStore store = _configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                    if (store == null)
                    {
                        Trace($"Could not open trusted peer store. StorePath={_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                    }
                    else
                    {
                        try
                        {
                            Trace($"Adding publisher certificate to trusted peer store. StorePath={_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
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
                    Trace(e, $"Could not add publisher certificate to trusted peer store. StorePath={_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                }
            }
            else
            {
                Trace("Publisher certificate is not added to trusted peer store.");
            }


            //
            // TransportConfigurations
            //

            _configuration.TransportQuotas = new TransportQuotas();
            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            _configuration.TransportQuotas.OperationTimeout = _opcOperationTimeout;
            Trace($"OperationTimeout set to {_configuration.TransportQuotas.OperationTimeout}");


            //
            // ServerConfiguration
            //
            _configuration.ServerConfiguration = new ServerConfiguration();

            // BaseAddresses
            if (_configuration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // We do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                _configuration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Utils.GetHostName()}:{_publisherServerPort}{_publisherServerPath}");
            }
            foreach (var endpoint in _configuration.ServerConfiguration.BaseAddresses)
            {
                Trace($"Publisher server base address: {endpoint}");
            }

            // SecurityPolicies
            // We do not allow security policy SecurityPolicies.None, but always high security
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            _configuration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Trace($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // MaxRegistrationInterval
            _configuration.ServerConfiguration.MaxRegistrationInterval = _ldsRegistrationInterval;
            Trace($"LDS(-ME) registration intervall set to {_ldsRegistrationInterval} ms (0 means no registration)");

            //
            // TraceConfiguration
            //
            _configuration.TraceConfiguration = new TraceConfiguration();
            // Due to a bug in a stack we need to do console output ourselve.
            Utils.SetTraceOutput(Utils.TraceOutput.FileOnly);

            // OutputFilePath
            if (string.IsNullOrEmpty(_logFileName))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
                {
                    _configuration.TraceConfiguration.OutputFilePath = Environment.GetEnvironmentVariable("_GW_LOGP");
                }
                else
                {
                    _configuration.TraceConfiguration.OutputFilePath = "./Logs/" + _configuration.ApplicationName + ".log.txt";
                }
            }
            else
            {
                _configuration.TraceConfiguration.OutputFilePath = _logFileName;
            }

            // DeleteOnLoad
            _configuration.TraceConfiguration.DeleteOnLoad = false;

            // TraceMasks
            _configuration.TraceConfiguration.TraceMasks = _opcStackTraceMask;

            // Apply the settings
            _configuration.TraceConfiguration.ApplySettings();
            Trace($"Current directory is: {System.IO.Directory.GetCurrentDirectory()}");
            Trace($"Log file is: {Utils.GetAbsoluteFilePath(_configuration.TraceConfiguration.OutputFilePath, true, false, false, true)}");
            Trace($"opcstacktracemask set to: 0x{_opcStackTraceMask:X} ({_opcStackTraceMask})");

            _configuration.ClientConfiguration = new ClientConfiguration();

            // validate the configuration now
            await _configuration.Validate(_configuration.ApplicationType);
        }
    }
}

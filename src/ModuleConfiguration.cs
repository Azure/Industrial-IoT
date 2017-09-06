
using System;
using System.Security.Cryptography.X509Certificates;

namespace Opc.Ua.Publisher
{
    using System.IO;
    using static Opc.Ua.Workarounds.TraceWorkaround;

    public class ModuleConfiguration
    {
        /// <summary>
        /// Opc client configuration
        /// </summary>
        public ApplicationConfiguration Configuration { get; set; }

        public ModuleConfiguration(string applicationName)
        {
            // set reasonable defaults
            Configuration = new ApplicationConfiguration()
            {
                ApplicationName = applicationName
            };
            Configuration.ApplicationUri = "urn:" + Utils.GetHostName() + ":microsoft:" + Configuration.ApplicationName;
            Configuration.ApplicationType = ApplicationType.ClientAndServer;
            Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            Configuration.ClientConfiguration = new ClientConfiguration();
            Configuration.ServerConfiguration = new ServerConfiguration();

            // initialize stack tracing
            Configuration.TraceConfiguration = new TraceConfiguration()
            {
                TraceMasks = Program.OpcStackTraceMask
            };
            Utils.SetTraceOutput(Utils.TraceOutput.FileOnly);
            if (string.IsNullOrEmpty(Program.LogFileName))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
                {
                    Configuration.TraceConfiguration.OutputFilePath = Environment.GetEnvironmentVariable("_GW_LOGP");
                }
                else
                {
                    Configuration.TraceConfiguration.OutputFilePath = "./Logs/" + Configuration.ApplicationName + ".log.txt";
                }
            }
            else
            {
                Configuration.TraceConfiguration.OutputFilePath = Program.LogFileName;
            }
            Configuration.TraceConfiguration.ApplySettings();
            Trace($"Current directory is: {Directory.GetCurrentDirectory()}");
            Trace($"Log file is: {Utils.GetAbsoluteFilePath(Configuration.TraceConfiguration.OutputFilePath, true, false, false, true)}");
            Trace($"opcstacktracemask set to: 0x{Program.OpcStackTraceMask:X} ({Program.OpcStackTraceMask})");

            Configuration.SecurityConfiguration = new SecurityConfiguration()
            {
                // Trusted cert store configuration.
                TrustedPeerCertificates = new CertificateTrustList()
                {
                    StoreType = Program.OpcTrustedCertStoreType
                }
            };
            if (string.IsNullOrEmpty(Program.OpcTrustedCertStorePath))
            {
                // Set default.
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Program.OpcTrustedCertStoreType == CertificateStoreType.X509Store ? Program.OpcTrustedCertX509StorePathDefault : Program.OpcTrustedCertDirectoryStorePathDefault;
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_TPC_SP")))
                {
                    // Use environment variable.
                    Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Environment.GetEnvironmentVariable("_TPC_SP");
                }
            }
            else
            {
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Program.OpcTrustedCertStorePath;
            }
            Trace($"Trusted Peer Certificate store type is: {Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType}");
            Trace($"Trusted Peer Certificate store path is: {Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");

            // Trusted issuer cert store configuration.
            Configuration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList()
            {
                StoreType = Program.OpcIssuerCertStoreType,
                StorePath = Program.OpcIssuerCertStorePath
            };
            Trace($"Trusted Issuer store type is: {Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Trace($"Trusted Issuer Certificate store path is: {Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // Rejected cert store configuration.
            Configuration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList()
            {
                StoreType = Program.OpcRejectedCertStoreType,
                StorePath = Program.OpcRejectedCertStorePath
            };
            Trace($"Rejected certificate store type is: {Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Trace($"Rejected Certificate store path is: {Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            Configuration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier()
            {
                StoreType = Program.OpcOwnCertStoreType,
                StorePath = Program.OpcOwnCertStorePath,
                SubjectName = Configuration.ApplicationName
            };
            Trace($"Application Certificate store type is: {Configuration.SecurityConfiguration.ApplicationCertificate.StoreType}");
            Trace($"Application Certificate store path is: {Configuration.SecurityConfiguration.ApplicationCertificate.StorePath}");

            // Use existing certificate, if it is there.
            X509Certificate2 certificate = Configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
            if (certificate == null)
            {
                Trace($"Create a self-signed Application certificate valid from yesterday for {CertificateFactory.defaultLifeTime} months,");
                Trace($"with a {CertificateFactory.defaultKeySize} bit key and {CertificateFactory.defaultHashSize} bit hash.");
                certificate = CertificateFactory.CreateCertificate(
                    Configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    Configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    Configuration.ApplicationUri,
                    Configuration.ApplicationName,
                    Configuration.ApplicationName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null
                    );
                Configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate ?? throw new Exception("OPC UA application certificate could not be created! Cannot continue without it!");

                // Trust myself if requested.
                if (Program.TrustMyself)
                {
                    // Ensure it is trusted
                    try
                    {
                        ICertificateStore store = Configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                        if (store == null)
                        {
                            Trace($"Could not open trusted peer store. StorePath={Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                        }
                        else
                        {
                            try
                            {
                                Trace($"Adding publisher certificate to trusted peer store. StorePath={Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                                X509Certificate2 publicKey = new X509Certificate2(certificate.RawData);
                                store.Add(publicKey).Wait();
                            }
                            finally
                            {
                                store.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace(e, $"Could not add publisher certificate to trusted peer store. StorePath={Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                    }
                }
                else
                {
                    Trace("Publisher certificate is not added to trusted peer store.");
                }
            }
            else
            {
                Trace("Application certificate found in Application Certificate Store");
            }
            Configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            Trace($"Application certificate is for Application URI: {Configuration.ApplicationUri}");

            // patch our base address
            if (Configuration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                Configuration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Configuration.ApplicationName.ToLowerInvariant()}:{Program.PublisherServerPort}{Program.PublisherServerPath}");
            }
            foreach (var endpoint in Configuration.ServerConfiguration.BaseAddresses)
            {
                Trace($"Publisher server Endpoint URL: {endpoint}");
            }

            // Set LDS registration interval
            Configuration.ServerConfiguration.MaxRegistrationInterval = Program.LdsRegistrationInterval;
            Trace($"LDS(-ME) registration intervall set to {Program.LdsRegistrationInterval} ms (0 means no registration)");

            // add sign & encrypt policy
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            Configuration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Trace($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            Configuration.TransportQuotas.OperationTimeout = Program.OpcOperationTimeout;
            Trace($"OperationTimeout set to {Configuration.TransportQuotas.OperationTimeout}");

            // allow SHA1 certificates for now as many OPC Servers still use them
            Configuration.SecurityConfiguration.RejectSHA1SignedCertificates = false;
            Trace($"Rejection of SHA1 signed certificates is {(Configuration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");

            // allow 1024 minimum key size as many OPC Servers still use them
            Configuration.SecurityConfiguration.MinimumCertificateKeySize = 1024;
            Trace($"Minimum certificate key size set to {Configuration.SecurityConfiguration.MinimumCertificateKeySize}");

            // validate the configuration now
            Configuration.Validate(Configuration.ApplicationType).Wait();
        }
    }
}

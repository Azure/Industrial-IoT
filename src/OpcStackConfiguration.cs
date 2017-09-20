
using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPublisher
{
    using System.IO;
    using static OpcPublisher.Workarounds.TraceWorkaround;

    public class OpcStackConfiguration
    {
        /// <summary>
        /// Opc client configuration
        /// </summary>
        public ApplicationConfiguration Configuration;

        public OpcStackConfiguration(string applicationName)
        {
            // Instead of using a Config.xml we configure everything programmatically.

            //
            // OPC UA Application configuration
            //
            Configuration = new ApplicationConfiguration();
            // Passed in as command line argument
            Configuration.ApplicationName = applicationName;
            Configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:{Configuration.ApplicationName}:microsoft:";
            Configuration.ProductUri = "https://github.com/Azure/iot-edge-opc-publisher";
            Configuration.ApplicationType = ApplicationType.ClientAndServer;


            //
            // Security configuration
            //
            Configuration.SecurityConfiguration = new SecurityConfiguration();

            // Application certificate
            Configuration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            Configuration.SecurityConfiguration.ApplicationCertificate.StoreType = Program.OpcOwnCertStoreType;
            Configuration.SecurityConfiguration.ApplicationCertificate.StorePath = Program.OpcOwnCertStorePath;
            Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName = Configuration.ApplicationName;
            Trace($"Application Certificate store type is: {Configuration.SecurityConfiguration.ApplicationCertificate.StoreType}");
            Trace($"Application Certificate store path is: {Configuration.SecurityConfiguration.ApplicationCertificate.StorePath}");
            Trace($"Application Certificate subject name is: {Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName}");

            // Use existing certificate, if it is there.
            X509Certificate2 certificate = Configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
            if (certificate == null)
            {
                Trace($"No existing Application certificate found. Create a self-signed Application certificate valid from yesterday for {CertificateFactory.defaultLifeTime} months,");
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
            }
            else
            {
                Trace("Application certificate found in Application Certificate Store");
            }
            Configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            Trace($"Application certificate is for Application URI: {Configuration.ApplicationUri}");

            // TrustedIssuerCertificates
            Configuration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = Program.OpcIssuerCertStoreType;
            Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = Program.OpcIssuerCertStorePath;
            Trace($"Trusted Issuer store type is: {Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Trace($"Trusted Issuer Certificate store path is: {Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // TrustedPeerCertificates
            Configuration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType = Program.OpcTrustedCertStoreType;
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

            // RejectedCertificateStore
            Configuration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType = Program.OpcRejectedCertStoreType;
            Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath = Program.OpcRejectedCertStorePath;
            Trace($"Rejected certificate store type is: {Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Trace($"Rejected Certificate store path is: {Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            // AutoAcceptUntrustedCertificates
            // This is a security risk and should be set to true only for debugging purposes.
            Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates = false;

            // RejectSHA1SignedCertificates
            // We allow SHA1 certificates for now as many OPC Servers still use them
            Configuration.SecurityConfiguration.RejectSHA1SignedCertificates = false;
            Trace($"Rejection of SHA1 signed certificates is {(Configuration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");

            // MinimunCertificatesKeySize
            // We allow a minimum key size of 1024 bit, as many OPC UA servers still use them
            Configuration.SecurityConfiguration.MinimumCertificateKeySize = 1024;
            Trace($"Minimum certificate key size set to {Configuration.SecurityConfiguration.MinimumCertificateKeySize}");

            // We make the default reference stack behavior configurable to put our own certificate into the trusted peer store.
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


            //
            // TransportConfigurations
            //

            Configuration.TransportQuotas = new TransportQuotas();
            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            Configuration.TransportQuotas.OperationTimeout = Program.OpcOperationTimeout;
            Configuration.TransportQuotas.MaxStringLength = 1048576;
            Configuration.TransportQuotas.MaxByteStringLength = 1048576;
            Configuration.TransportQuotas.MaxArrayLength = 65535;
            Configuration.TransportQuotas.MaxMessageSize = 4194304;
            Configuration.TransportQuotas.MaxBufferSize = 65535;
            Configuration.TransportQuotas.ChannelLifetime = 300000;
            Configuration.TransportQuotas.SecurityTokenLifetime = 3600000;
            Trace($"OperationTimeout set to {Configuration.TransportQuotas.OperationTimeout}");


            //
            // ServerConfiguration
            //
            Configuration.ServerConfiguration = new ServerConfiguration();

            // BaseAddresses
            if (Configuration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // We do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                Configuration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Utils.GetHostName()}:{Program.PublisherServerPort}{Program.PublisherServerPath}");
            }
            foreach (var endpoint in Configuration.ServerConfiguration.BaseAddresses)
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
            Configuration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Trace($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // MinRequestThreadCount
            Configuration.ServerConfiguration.MinRequestThreadCount = 5;

            // MaxRequestThreadCount
            Configuration.ServerConfiguration.MaxRequestThreadCount = 100;

            // MaxQueuedRequestCount
            Configuration.ServerConfiguration.MaxQueuedRequestCount = 2000;

            // UserTokenPolicies
            UserTokenPolicy userTokenPolicy = new UserTokenPolicy(UserTokenType.Anonymous)
            {
                SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None"
            };
            Configuration.ServerConfiguration.UserTokenPolicies.Add(userTokenPolicy);

            // DiagnosticsEnabled
            Configuration.ServerConfiguration.DiagnosticsEnabled = false;

            // MaxSessionCount
            Configuration.ServerConfiguration.MaxSessionCount = 100;

            // MinSessionTimeout
            Configuration.ServerConfiguration.MinSessionTimeout = 1000;

            // MaxSessionTimeout
            Configuration.ServerConfiguration.MaxSessionTimeout = 3600000;

            // MaxBrowseContinuationPoints
            Configuration.ServerConfiguration.MaxBrowseContinuationPoints = 10;

            // MaxQueryContinuationPoints
            Configuration.ServerConfiguration.MaxQueryContinuationPoints = 10;

            // MaxHistoryContinuationPoints
            Configuration.ServerConfiguration.MaxHistoryContinuationPoints = 100;

            // MaxRequestAge
            Configuration.ServerConfiguration.MaxRequestAge = 600000;

            // MinPublishingInterval
            Configuration.ServerConfiguration.MinPublishingInterval = 100;

            // MaxPublishingInterval
            Configuration.ServerConfiguration.MaxPublishingInterval = 3600000;

            // PublishingResolution
            Configuration.ServerConfiguration.PublishingResolution = 50;

            // MaxSubscriptionLifetime
            Configuration.ServerConfiguration.MaxSubscriptionLifetime = 3600000;

            // MaxMessageQueueSize
            Configuration.ServerConfiguration.MaxMessageQueueSize = 100;

            // MaxNotificationQueueSize
            Configuration.ServerConfiguration.MaxNotificationQueueSize = 100;

            // MaxNotificationsPerPublish
            Configuration.ServerConfiguration.MaxNotificationsPerPublish = 1000;

            // MinMetadataSamplingInterval
            Configuration.ServerConfiguration.MinMetadataSamplingInterval = 1000;

            // AvailableSamplingRates
            // not used

            // RegistrationEndpoint
            Configuration.ServerConfiguration.RegistrationEndpoint = new EndpointDescription();
            Configuration.ServerConfiguration.RegistrationEndpoint.EndpointUrl = "opc.tcp://localhost:4840";
            Configuration.ServerConfiguration.RegistrationEndpoint.Server.ApplicationUri = "opc.tcp://localhost:4840";
            Configuration.ServerConfiguration.RegistrationEndpoint.Server.ApplicationType = ApplicationType.Server;
            Configuration.ServerConfiguration.RegistrationEndpoint.Server.DiscoveryUrls.Add("opc.tcp://localhost:4840");
            Configuration.ServerConfiguration.RegistrationEndpoint.SecurityMode = MessageSecurityMode.SignAndEncrypt;

            // MaxRegistrationInterval
            Configuration.ServerConfiguration.MaxRegistrationInterval = Program.LdsRegistrationInterval;
            Trace($"LDS(-ME) registration intervall set to {Program.LdsRegistrationInterval} ms (0 means no registration)");

            // NodeManagerSaveFile
            // not used

            // MinSubscriptionLifetime
            Configuration.ServerConfiguration.MinSubscriptionLifetime = 10000;

            // MaxPublishRequestCount
            Configuration.ServerConfiguration.MaxPublishRequestCount = 20;

            // MaxSubscriptionCount
            Configuration.ServerConfiguration.MaxSubscriptionCount = 100;

            // MaxEventQueueSize
            Configuration.ServerConfiguration.MaxEventQueueSize = 10000;

            // ServerProfileArray
            // see https://opcfoundation-onlineapplications.org/profilereporting/ for list of available profiles
            Configuration.ServerConfiguration.ServerProfileArray.Add("Standard UA Server Profile");
            Configuration.ServerConfiguration.ServerProfileArray.Add("Data Access Server Facet");
            Configuration.ServerConfiguration.ServerProfileArray.Add("Method Server Facet");


            //
            // TraceConfiguration
            //
            Configuration.TraceConfiguration = new TraceConfiguration();
            // Due to a bug in a stack we need to do console output ourselve.
            Utils.SetTraceOutput(Utils.TraceOutput.FileOnly);

            // OutputFilePath
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

            // DeleteOnLoad
            Configuration.TraceConfiguration.DeleteOnLoad = false;

            // TraceMasks
            Configuration.TraceConfiguration.TraceMasks = Program.OpcStackTraceMask;

            // Apply the settings
            Configuration.TraceConfiguration.ApplySettings();
            Trace($"Current directory is: {Directory.GetCurrentDirectory()}");
            Trace($"Log file is: {Utils.GetAbsoluteFilePath(Configuration.TraceConfiguration.OutputFilePath, true, false, false, true)}");
            Trace($"opcstacktracemask set to: 0x{Program.OpcStackTraceMask:X} ({Program.OpcStackTraceMask})");

            // DisableHiResClock
            Configuration.DisableHiResClock = true;

            // 
            // ClientConfiguration
            //

            Configuration.ClientConfiguration = new ClientConfiguration();

            // DefaultSessionTimeout
            Configuration.ClientConfiguration.DefaultSessionTimeout = 600000;

            // WellKnownDiscoveryUrls
            Configuration.ClientConfiguration.WellKnownDiscoveryUrls.Add("opc.tcp://{0}:4840/UADiscovery");
            Configuration.ClientConfiguration.WellKnownDiscoveryUrls.Add("http://{0}:52601/UADiscovery");
            Configuration.ClientConfiguration.WellKnownDiscoveryUrls.Add("http://{0}/UADiscovery/Default.svc");

            // DiscoveryServers
            // Not used

            // EndpointCacheFilePath
            // Not used

            // MinSubscriptionLifetime
            Configuration.ClientConfiguration.MinSubscriptionLifetime = 10000;

            // validate the configuration now
            Configuration.Validate(Configuration.ApplicationType).Wait();
        }
    }
}

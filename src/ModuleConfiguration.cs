using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Opc.Ua.Publisher
{
    public class ModuleConfiguration
    {
        /// <summary>
        /// Opc client configuration
        /// </summary>
        public ApplicationConfiguration Configuration { get; set; }

        public ModuleConfiguration(string applicationName)
        {
            // set reasonable defaults
            Configuration = new ApplicationConfiguration();
            Configuration.ApplicationName = applicationName;
            Configuration.ApplicationUri = "urn:" + Utils.GetHostName() + ":microsoft:" + Configuration.ApplicationName;
            Configuration.ApplicationType = ApplicationType.ClientAndServer;
            Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            Configuration.ClientConfiguration = new ClientConfiguration();
            Configuration.ServerConfiguration = new ServerConfiguration();

            // enable logging
            Configuration.TraceConfiguration = new TraceConfiguration();
            Configuration.TraceConfiguration.TraceMasks = Utils.TraceMasks.Error | Utils.TraceMasks.Security | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
            {
                Configuration.TraceConfiguration.OutputFilePath = Environment.GetEnvironmentVariable("_GW_LOGP");
            }
            else
            {
                Configuration.TraceConfiguration.OutputFilePath = "./Logs/" + Configuration.ApplicationName + ".log.txt";
            }
            Configuration.TraceConfiguration.ApplySettings();

            if (Configuration.SecurityConfiguration == null)
            {
                Configuration.SecurityConfiguration = new SecurityConfiguration();
            }

            if (Configuration.SecurityConfiguration.TrustedPeerCertificates == null)
            {
                Configuration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            }

            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates == null)
            {
                Configuration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            }

            if (Configuration.SecurityConfiguration.RejectedCertificateStore == null)
            {
                Configuration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            }

            if (Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType == null)
            {
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StoreType = "Directory";
            }

            if (Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath == null)
            {
                Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = "CertificateStores/UA Applications";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_TPC_SP")))
                {
                    Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Environment.GetEnvironmentVariable("_TPC_SP");
                }
            }

            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType == null)
            {
                Configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = "Directory";
            }

            if (Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath == null)
            {
                Configuration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = "CertificateStores/UA Certificate Authorities";
            }

            if (Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType == null)
            {
                Configuration.SecurityConfiguration.RejectedCertificateStore.StoreType = "Directory";
            }

            if (Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath == null)
            {
                Configuration.SecurityConfiguration.RejectedCertificateStore.StorePath = "CertificateStores/Rejected Certificates";
            }

            Configuration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            Configuration.SecurityConfiguration.ApplicationCertificate.StoreType = "Directory";
            Configuration.SecurityConfiguration.ApplicationCertificate.StorePath = "CertificateStores/UA Applications";
            Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName = Configuration.ApplicationName;

            X509Certificate2 certificate = Configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
            if (certificate == null)
            {
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
            }
            if (certificate == null)
            {
                throw new Exception("OPC UA application certificate could not be created, cannot continue without it!");
            }

            Configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            Configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);

            // Ensure it is trusted
            try
            {
                ICertificateStore store = Configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                if (store == null)
                {
                    Program.Trace("Could not open trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
                }
                else
                {
                    try
                    {
                        Program.Trace(Utils.TraceMasks.Information, "Adding certificate to trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
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
                Program.Trace(e, "Could not add certificate to trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
            }
        
            // patch our base address
            if (Configuration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                Configuration.ServerConfiguration.BaseAddresses.Add("opc.tcp://" + Configuration.ApplicationName.ToLowerInvariant() + ":62222/UA/Publisher");
            }

            // tighten security policy by removing security policy "none" 
            foreach (ServerSecurityPolicy policy in Configuration.ServerConfiguration.SecurityPolicies)
            {
                if (policy.SecurityMode == MessageSecurityMode.None)
                {
                    Configuration.ServerConfiguration.SecurityPolicies.Remove(policy);
                    break;
                }
            }

            // turn off LDS registration
            Configuration.ServerConfiguration.MaxRegistrationInterval = 0;

            // add sign & encrypt policy
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy();
            newPolicy.SecurityMode = MessageSecurityMode.SignAndEncrypt;
            newPolicy.SecurityPolicyUri = SecurityPolicies.Basic128Rsa15;
            Configuration.ServerConfiguration.SecurityPolicies.Add(newPolicy);

            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            Configuration.TransportQuotas.OperationTimeout = 120000;

            // allow SHA1 certificates for now as many OPC Servers still use them
            Configuration.SecurityConfiguration.RejectSHA1SignedCertificates = false;

            // allow 1024 minimum key size as many OPC Servers still use them
            Configuration.SecurityConfiguration.MinimumCertificateKeySize = 1024;

            // validate the configuration now
            Configuration.Validate(Configuration.ApplicationType).Wait();
        }
    }
}

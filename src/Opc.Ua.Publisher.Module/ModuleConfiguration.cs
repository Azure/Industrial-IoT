
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace Opc.Ua.Publisher
{
    /// <summary>
    /// Module configuration object to deserialize / serialize
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ModuleConfiguration
    {
        /// <summary>
        /// Opc client configuration
        /// </summary>
        [JsonProperty]
        public ApplicationConfiguration Configuration { get; set; }

        /// <summary>
        /// Called when the object is deserialized
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            // Validate configuration and set reasonable defaults

            Configuration.ApplicationUri = Configuration.ApplicationUri.Replace("localhost", Utils.GetHostName());

            Configuration.ApplicationType = ApplicationType.ClientAndServer;
            Configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            Configuration.ClientConfiguration = new ClientConfiguration();
            Configuration.ServerConfiguration = new ServerConfiguration();

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
            Configuration.SecurityConfiguration.ApplicationCertificate.StoreType = "X509Store";
            Configuration.SecurityConfiguration.ApplicationCertificate.StorePath = "CurrentUser\\UA_MachineDefault";
            Configuration.SecurityConfiguration.ApplicationCertificate.SubjectName = Configuration.ApplicationName;

            X509Certificate2 certificate = Configuration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
            if (certificate == null)
            {
                certificate = CertificateFactory.CreateCertificate(
                    Configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    Configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    Configuration.ApplicationUri,
                    Configuration.ApplicationName,
                    Configuration.ApplicationName,
                    new List<string>(){ Configuration.ApplicationName }
                    );
            }
            if (certificate == null)
            {
                throw new Exception("Opc.Ua.Publisher.Module: OPC UA application certificate could not be created, cannot continue without it!");
            }

            Configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            Configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);

            // Ensure it is trusted
            try
            {
                ICertificateStore store = Configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                if (store == null)
                {
                    Module.Trace("Could not open trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
                }
                else
                {
                    try
                    {
                        Module.Trace(Utils.TraceMasks.Information, "Adding certificate to trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
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
                Module.Trace(e, "Could not add certificate to trusted peer store. StorePath={0}", Configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
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

            // enable logging
            Configuration.TraceConfiguration = new TraceConfiguration();
            Configuration.TraceConfiguration.DeleteOnLoad = true;
            Configuration.TraceConfiguration.TraceMasks = 519;
            Configuration.TraceConfiguration.OutputFilePath = "./Logs/" + Configuration.ApplicationName + ".Publisher.Module.log.txt";
            Configuration.TraceConfiguration.ApplySettings();

            // the OperationTimeout should be twice the minimum value for PublishingInterval * KeepAliveCount, so set to 120s
            Configuration.TransportQuotas.OperationTimeout = 120000;

            // validate the configuration now
            Configuration.Validate(Configuration.ApplicationType).Wait();
        }
    }
}

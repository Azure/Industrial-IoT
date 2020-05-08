// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OpcConfigEx {

        /// <summary>
        /// Create application configuration
        /// </summary>
        /// <param name="opcConfig"></param>
        /// <param name="handler"></param>
        /// <param name="createSelfSignedCertIfNone"></param>
        /// <returns></returns>
        public static async Task<ApplicationConfiguration> ToApplicationConfigurationAsync(
            this IClientServicesConfig opcConfig, bool createSelfSignedCertIfNone,
            CertificateValidationEventHandler handler) {
            if (string.IsNullOrWhiteSpace(opcConfig.ApplicationName)) {
                throw new ArgumentNullException(nameof(opcConfig.ApplicationName));
            }

            var applicationConfiguration = new ApplicationConfiguration {
                ApplicationName = opcConfig.ApplicationName,
                ApplicationUri = opcConfig.ApplicationUri,
                ProductUri = opcConfig.ProductUri,
                ApplicationType = ApplicationType.Client,
                TransportQuotas = opcConfig.ToTransportQuotas(),
                SecurityConfiguration = opcConfig.ToSecurityConfiguration(),
                ClientConfiguration = new ClientConfiguration(),
                CertificateValidator = new CertificateValidator()
            };

            applicationConfiguration.CertificateValidator.CertificateValidation += handler;

            var configuredSubject = applicationConfiguration.SecurityConfiguration
                .ApplicationCertificate.SubjectName;
            applicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = 
                applicationConfiguration.ApplicationName;
            await applicationConfiguration.CertificateValidator
                .Update(applicationConfiguration.SecurityConfiguration);

            // use existing certificate, if present
            var certificate = applicationConfiguration.SecurityConfiguration
                .ApplicationCertificate.Certificate;

            // create a self signed certificate if there is none
            if (certificate == null && createSelfSignedCertIfNone) {
                certificate = CertificateFactory.CreateCertificate(
                    applicationConfiguration.SecurityConfiguration
                        .ApplicationCertificate.StoreType,
                    applicationConfiguration.SecurityConfiguration
                        .ApplicationCertificate.StorePath,
                    null,
                    applicationConfiguration.ApplicationUri,
                    applicationConfiguration.ApplicationName,
                    configuredSubject,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize
                );

                if (certificate == null) {
                    throw new Exception(
                        "OPC UA application certificate can not be created! Cannot continue without it!");
                }

                applicationConfiguration.SecurityConfiguration
                    .ApplicationCertificate.Certificate = certificate;
                try {
                    // copy the certificate *public key only* into the trusted certificates list
                    using (ICertificateStore trustedStore = applicationConfiguration
                        .SecurityConfiguration.TrustedPeerCertificates.OpenStore()) {
                        using (var publicKey = new X509Certificate2(certificate.RawData)) {
                            trustedStore.Add(publicKey.YieldReturn());
                        }
                    }
                }
                catch { }
                // update security information
                await applicationConfiguration.CertificateValidator.UpdateCertificate(
                    applicationConfiguration.SecurityConfiguration);
            }

            applicationConfiguration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            return applicationConfiguration;
        }
    }
}
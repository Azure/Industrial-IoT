// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Opc.Ua;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OpcConfigEx {

        /// <summary>
        /// Create application configuration
        /// </summary>
        /// <param name="opcConfig"></param>
        /// <param name="createSelfSignedCertIfNone"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ToApplicationConfiguration(this IClientServicesConfig2 opcConfig, bool createSelfSignedCertIfNone) {
            if (string.IsNullOrWhiteSpace(opcConfig.ApplicationName)) {
                throw new ArgumentNullException($"{nameof(opcConfig)}.{nameof(ClientServicesConfig2.ApplicationName)}");
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

            applicationConfiguration.CertificateValidator.CertificateValidation += (validator, e) => {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                    e.Accept = opcConfig.AutoAcceptUntrustedCertificates;

                    if (e.Accept) {
                        //Logger.Information($"Certificate '{e.Certificate.Subject}' will be trusted, because of corresponding command line option.");
                    }
                }
            };

            X509Certificate2 certificate = null;

            // use existing certificate, if it is there
            certificate = applicationConfiguration.SecurityConfiguration.ApplicationCertificate.Find(true).Result;

            // create a self signed certificate if there is none
            if (certificate == null && createSelfSignedCertIfNone) {
                certificate = CertificateFactory.CreateCertificate(
                    applicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    applicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    applicationConfiguration.ApplicationUri,
                    applicationConfiguration.ApplicationName,
                    applicationConfiguration.ApplicationName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize
                );

                // update security information
                applicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate =
                    certificate ??
                    throw new Exception(
                        "OPC UA application certificate can not be created! Cannot continue without it!");
                //await applicationConfiguration.CertificateValidator.UpdateCertificate(applicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
            }

            applicationConfiguration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);

            return applicationConfiguration;
        }
    }
}
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OpcConfigEx {

        /// <summary>
        /// Create application configuration
        /// </summary>
        /// <param name="opcConfig"></param>
        /// <param name="identity"></param>
        /// <param name="createSelfSignedCertIfNone"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static async Task<ApplicationConfiguration> ToApplicationConfigurationAsync(
            this IClientServicesConfig opcConfig, IIdentity identity, bool createSelfSignedCertIfNone,
            CertificateValidationEventHandler handler) {
            if (string.IsNullOrWhiteSpace(opcConfig.ApplicationName)) {
                throw new ArgumentNullException(nameof(opcConfig.ApplicationName));
            }
            var hostname = !string.IsNullOrWhiteSpace(identity?.Gateway) ?
                identity.Gateway : !string.IsNullOrWhiteSpace(identity?.DeviceId) ?
                    identity.DeviceId : Dns.GetHostName();
            var applicationConfiguration = new ApplicationConfiguration {
                ApplicationName = opcConfig.ApplicationName,
                ApplicationUri = opcConfig.ApplicationUri.Replace("urn:localhost", $"urn:{hostname}"),
                ProductUri = opcConfig.ProductUri,
                ApplicationType = ApplicationType.Client,
                TransportQuotas = opcConfig.ToTransportQuotas(),
                SecurityConfiguration = opcConfig.ToSecurityConfiguration(identity),
                CertificateValidator = new CertificateValidator(),
                ClientConfiguration = new ClientConfiguration(),
                ServerConfiguration = new ServerConfiguration() {
                    BaseAddresses = new[] { $"uri:{hostname}", $"uri:{Dns.GetHostName()}" }
                }
            };
            await applicationConfiguration.Validate(applicationConfiguration.ApplicationType);
            var application = new ApplicationInstance(applicationConfiguration);
            var hasAppCertificate = await application.CheckApplicationInstanceCertificate(true,
                CertificateFactory.defaultKeySize);
            if (!hasAppCertificate) {
                throw new InvalidConfigurationException("OPC UA application certificate can not be validated");
            }

            applicationConfiguration.CertificateValidator.CertificateValidation += handler;
            await applicationConfiguration.CertificateValidator
                .Update(applicationConfiguration.SecurityConfiguration);

            return applicationConfiguration;
        }
    }
}
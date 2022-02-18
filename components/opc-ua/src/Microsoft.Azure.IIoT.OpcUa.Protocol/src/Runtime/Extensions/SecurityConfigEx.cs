// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class SecurityConfigEx {


        /// <summary>
        /// Build the security configuration
        /// </summary>
        public static async Task<ApplicationConfiguration> BuildSecurityConfiguration(
            this ISecurityConfig securityConfig,
            IApplicationConfigurationBuilderClientSelected applicationConfigurationBuilder,
            ApplicationConfiguration applicationConfiguration,
            string hostname) {
            if (securityConfig == null) {
                throw new ArgumentNullException(nameof(securityConfig));
            }

            if (securityConfig.TrustedIssuerCertificates == null) {
                throw new ArgumentNullException(
                    $"{nameof(securityConfig)}.{nameof(SecurityConfig.TrustedIssuerCertificates)}");
            }

            if (securityConfig.TrustedPeerCertificates == null) {
                throw new ArgumentNullException(
                    $"{nameof(securityConfig)}.{nameof(SecurityConfig.TrustedPeerCertificates)}");
            }

            if (securityConfig.RejectedCertificateStore == null) {
                throw new ArgumentNullException(
                    $"{nameof(securityConfig)}.{nameof(SecurityConfig.RejectedCertificateStore)}");
            }

            if (securityConfig.ApplicationCertificate == null) {
                throw new ArgumentNullException(
                    $"{nameof(securityConfig)}.{nameof(SecurityConfig.ApplicationCertificate)}");
            }

            var options = applicationConfigurationBuilder
                .AddSecurityConfiguration(
                    securityConfig.ApplicationCertificate.SubjectName.Replace("localhost", hostname),
                    securityConfig.PkiRootPath)
                .SetAutoAcceptUntrustedCertificates(securityConfig.AutoAcceptUntrustedCertificates)
                .SetRejectSHA1SignedCertificates(securityConfig.RejectSha1SignedCertificates)
                .SetMinimumCertificateKeySize(securityConfig.MinimumCertificateKeySize)
                .SetAddAppCertToTrustedStore(securityConfig.AddAppCertToTrustedStore);

            securityConfig.ApplicationCertificate.BuildCertificateIdentifier(
                applicationConfiguration.SecurityConfiguration.ApplicationCertificate);

            securityConfig.TrustedPeerCertificates.BuildCertificateTrustList(
                applicationConfiguration.SecurityConfiguration.TrustedPeerCertificates);

            securityConfig.TrustedIssuerCertificates.BuildCertificateTrustList(
                applicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates);

            securityConfig.RejectedCertificateStore.BuildCertificateIdentifierStore(
                applicationConfiguration.SecurityConfiguration.RejectedCertificateStore);

            return await options.Create().ConfigureAwait(false);
        }
    }
}

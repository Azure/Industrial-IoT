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
        /// Builds and applies the the security configuration according to the local settings. Returns a the
        /// configuration application ready to use for initialization of the OPC UA SDK client object.
        /// </summary>
        ///<remarks>
        /// Please note the input argument <cref>applicationConfiguration</cref> will be altered during execution
        /// with the locally provided security configuration and shall not be used after calling this method.
        /// </remarks>
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
                .SetAddAppCertToTrustedStore(securityConfig.AddAppCertToTrustedStore)
                .SetRejectUnknownRevocationStatus(securityConfig.RejectUnknownRevocationStatus);

            applicationConfiguration.SecurityConfiguration.ApplicationCertificate
                .ApplyLocalConfig(securityConfig.ApplicationCertificate);
            applicationConfiguration.SecurityConfiguration.TrustedPeerCertificates
                .ApplyLocalConfig(securityConfig.TrustedPeerCertificates);
            applicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates
                .ApplyLocalConfig(securityConfig.TrustedIssuerCertificates);
            applicationConfiguration.SecurityConfiguration.RejectedCertificateStore
                .ApplyLocalConfig(securityConfig.RejectedCertificateStore);

            return await options.Create().ConfigureAwait(false);
        }
    }
}

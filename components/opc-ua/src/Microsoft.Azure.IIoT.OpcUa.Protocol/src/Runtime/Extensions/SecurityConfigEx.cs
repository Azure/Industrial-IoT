﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using System;
    using Opc.Ua;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class SecurityConfigEx {

        /// <summary>
        /// Convert to security configuration
        /// </summary>
        /// <param name="securityConfig"></param>
        /// <returns></returns>
        public static SecurityConfiguration ToSecurityConfiguration(this ISecurityConfig securityConfig) {
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

            var securityConfiguration = new SecurityConfiguration {
                TrustedIssuerCertificates = securityConfig.TrustedIssuerCertificates.ToCertificateTrustList(),
                TrustedPeerCertificates = securityConfig.TrustedPeerCertificates.ToCertificateTrustList(),
                RejectedCertificateStore = securityConfig.RejectedCertificateStore.ToCertificateTrustList(),
                AutoAcceptUntrustedCertificates = securityConfig.AutoAcceptUntrustedCertificates,
                RejectSHA1SignedCertificates = securityConfig.RejectSha1SignedCertificates,
                MinimumCertificateKeySize = securityConfig.MinimumCertificateKeySize,
                ApplicationCertificate = securityConfig.ApplicationCertificate.ToCertificateIdentifier(),
                AddAppCertToTrustedStore = securityConfig.AddAppCertToTrustedStore
            };

            return securityConfiguration;
        }
    }
}
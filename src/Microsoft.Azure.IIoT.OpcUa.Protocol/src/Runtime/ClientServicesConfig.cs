// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public class ClientServicesConfig : ConfigBase, IClientServicesConfig {

        private const string kPkiRootFolder = "PkiRootFolder";
        private const string kApplicationCertificateFolder = "ApplicationCertificateFolder";
        private const string kTrustedPeerCertificatesFolder = "TrustedPeerCertificatesFolder";
        private const string kTrustedIssuerCertificatesFolder = "TrustedIssuerCertificatesFolder";
        private const string kRejectedCertificatesFolder = "RejectedCertificatesFolder";
        private const string kAutoAcceptUntrustedCertificates = "AutoAcceptUntrustedCertificates";

        /// <inheritdoc/>
        public string PkiRootFolder =>
            GetStringOrDefault(kPkiRootFolder, null);

        /// <inheritdoc/>
        public string ApplicationCertificateFolder =>
            GetStringOrDefault(kApplicationCertificateFolder, null);

        /// <inheritdoc/>
        public string TrustedPeerCertificatesFolder =>
            GetStringOrDefault(kTrustedPeerCertificatesFolder, null);

        /// <inheritdoc/>
        public string TrustedIssuerCertificatesFolder =>
            GetStringOrDefault(kTrustedIssuerCertificatesFolder, null);
        
        /// <inheritdoc/>
        public string RejectedCertificatesFolder =>
            GetStringOrDefault(kRejectedCertificatesFolder, null);
        
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates =>
            GetBoolOrDefault(kAutoAcceptUntrustedCertificates, false);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ClientServicesConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}

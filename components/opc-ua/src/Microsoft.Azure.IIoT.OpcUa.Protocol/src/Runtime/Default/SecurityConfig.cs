// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.IO;

    /// <summary>
    /// Security configuration
    /// </summary>
    public class SecurityConfig : ConfigBase, ISecurityConfig {

        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string PkiRootPathKey = "PkiRootPath";
        public const string ApplicationCertificateStorePathKey = "ApplicationCertificateStorePath";
        public const string ApplicationCertificateStoreTypeKey = "ApplicationCertificateStoreType";
        public const string ApplicationCertificateSubjectNameKey = "ApplicationCertificateSubjectName";
        public const string ApplicationNameKey = "ApplicationName";
        public const string TrustedIssuerCertificatesPathKey = "TrustedIssuerCertificatesPath";
        public const string TrustedIssuerCertificatesTypeKey = "TrustedIssuerCertificatesType";
        public const string TrustedPeerCertificatesPathKey = "TrustedPeerCertificatesPath";
        public const string TrustedPeerCertificatesTypeKey = "TrustedPeerCertificatesType";
        public const string RejectedCertificateStorePathKey = "RejectedCertificateStorePath";
        public const string RejectedCertificateStoreTypeKey = "RejectedCertificateStoreType";
        public const string AutoAcceptUntrustedCertificatesKey = "AutoAcceptUntrustedCertificates";
        public const string RejectSha1SignedCertificatesKey = "RejectSha1SignedCertificates";
        public const string MinimumCertificateKeySizeKey = "MinimumCertificateKeySize";
        public const string AddAppCertToTrustedStoreKey = "AddAppCertToTrustedStore";
        public const string RejectUnknownRevocationStatusKey = "RejectUnknownRevocationStatus";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public string PkiRootPath =>
            GetStringOrDefault(PkiRootPathKey, () => "pki");

        /// <inheritdoc/>
        public CertificateInfo ApplicationCertificate => new CertificateInfo {
            StorePath = GetStringOrDefault(ApplicationCertificateStorePathKey,
                () => $"{PkiRootPath}/own"),
            StoreType = GetStringOrDefault(ApplicationCertificateStoreTypeKey,
                () => CertificateStoreType.Directory),
            SubjectName = GetStringOrDefault(ApplicationCertificateSubjectNameKey,
                () => $"CN={GetStringOrDefault(ApplicationNameKey, () => "Microsoft.Azure.IIoT")}," +
                " C=DE, S=Bav, O=Microsoft, DC=localhost")
        };

        /// <inheritdoc/>
        public CertificateStore TrustedIssuerCertificates => new CertificateStore {
            StorePath = GetStringOrDefault(TrustedIssuerCertificatesPathKey,
                () => _issuersFolderName.Value),
            StoreType = GetStringOrDefault(TrustedIssuerCertificatesTypeKey,
                () => CertificateStoreType.Directory),
        };

        /// <inheritdoc/>
        public CertificateStore TrustedPeerCertificates => new CertificateStore {
            StorePath = GetStringOrDefault(TrustedPeerCertificatesPathKey,
                () => $"{PkiRootPath}/trusted"),
            StoreType = GetStringOrDefault(TrustedPeerCertificatesTypeKey,
                () => CertificateStoreType.Directory),
        };

        /// <inheritdoc/>
        public CertificateStore RejectedCertificateStore => new CertificateStore {
            StorePath = GetStringOrDefault(RejectedCertificateStorePathKey,
                () => $"{PkiRootPath}/rejected"),
            StoreType = GetStringOrDefault(RejectedCertificateStoreTypeKey,
                () => CertificateStoreType.Directory),
        };

        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates =>
            GetBoolOrDefault(AutoAcceptUntrustedCertificatesKey, () => false);
        /// <inheritdoc/>
        public bool RejectSha1SignedCertificates =>
            GetBoolOrDefault(RejectSha1SignedCertificatesKey, () => false);
        /// <inheritdoc/>
        public ushort MinimumCertificateKeySize =>
            (ushort)GetIntOrDefault(MinimumCertificateKeySizeKey, () => 1024);
        /// <inheritdoc/>
        public bool AddAppCertToTrustedStore =>
            GetBoolOrDefault(AddAppCertToTrustedStoreKey, () => true);
        /// <inheritdoc/>
        public bool RejectUnknownRevocationStatus =>
            GetBoolOrDefault(RejectUnknownRevocationStatusKey, () => true);

        /// <summary>
        /// Create configuration
        /// </summary>
        /// <param name="application"></param>
        /// <param name="configuration"></param>
        public SecurityConfig(IClientServicesConfig application, IConfiguration configuration) :
            base(configuration) {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _issuersFolderName = new Lazy<string>(GetIssuersFolder);
        }

        /// <summary>
        /// Returns the legacy 'issuers' if folder already exists or per specification. For more information
        /// <see href="https://github.com/OPCFoundation/UA-.NETStandard/blob/master/Docs/Certificates.md"/>.
        /// </summary>
        /// <returns>The issuer folder name</returns>
        private string GetIssuersFolder() {
            var legacyPath = $"{PkiRootPath}/issuers";
            return Directory.Exists(legacyPath) ? legacyPath : $"{PkiRootPath}/issuer";
        }

        private readonly IClientServicesConfig _application;
        private readonly Lazy<string> _issuersFolderName;
    }
}

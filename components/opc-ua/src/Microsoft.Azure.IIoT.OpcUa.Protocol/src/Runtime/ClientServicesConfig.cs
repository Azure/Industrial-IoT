// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public class ClientServicesConfig : ConfigBase, IClientServicesConfig {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string AppCertStoreTypeKey = "AppCertStoreType";
        public const string PkiRootPathKey = "PkiRootPath";
        public const string OwnCertPathKey = "OwnCertPath";
        public const string TrustedCertPathKey = "TrustedCertPath";
        public const string IssuerCertPathKey = "IssuerCertPath";
        public const string RejectedCertPathKey = "RejectedCertPath";
        public const string AutoAcceptKey = "AutoAccept";
        public const string OwnCertX509StorePathDefaultKey = "OwnCertX509StorePathDefault";
        public const string SessionTimeoutKey = "SessionTimeout";
        public const string OperationTimeoutKey = "OperationTimeout";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public string AppCertStoreType => GetStringOrDefault(AppCertStoreTypeKey,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "X509Store" : "Directory");
        /// <inheritdoc/>
        public string PkiRootPath =>
            GetStringOrDefault(PkiRootPathKey, "pki");
        /// <inheritdoc/>
        public string OwnCertPath =>
            GetStringOrDefault(OwnCertPathKey, PkiRootPath + "/own");
        /// <inheritdoc/>
        public string TrustedCertPath =>
            GetStringOrDefault(TrustedCertPathKey, PkiRootPath + "/trusted");
        /// <inheritdoc/>
        public string IssuerCertPath =>
            GetStringOrDefault(IssuerCertPathKey, PkiRootPath + "/issuer");
        /// <inheritdoc/>
        public string RejectedCertPath =>
            GetStringOrDefault(RejectedCertPathKey, PkiRootPath + "/rejected");
        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault =>
            GetStringOrDefault(OwnCertX509StorePathDefaultKey, "CurrentUser\\UA_MachineDefault");
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates =>
            GetBoolOrDefault(AutoAcceptKey, false);
        /// <inheritdoc/>
        public TimeSpan? DefaultSessionTimeout =>
            GetDurationOrNull(SessionTimeoutKey);
        /// <inheritdoc/>
        public TimeSpan? OperationTimeout =>
            GetDurationOrNull(OperationTimeoutKey);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ClientServicesConfig(IConfiguration configuration = null) :
            base(configuration) {
        }
    }
}

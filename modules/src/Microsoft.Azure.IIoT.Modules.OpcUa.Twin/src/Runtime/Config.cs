// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System.Runtime.InteropServices;
    using System;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : ConfigBase, IModuleConfig, IClientServicesConfig {

        /// <summary>
        /// Module configuration
        /// </summary>
        private const string kEdgeHubConnectionString = "EdgeHubConnectionString";
        /// <summary>Hub connection string</summary>
        public string EdgeHubConnectionString =>
            GetStringOrDefault(kEdgeHubConnectionString);
        /// <summary>Whether to bypass cert validation</summary>
        public bool BypassCertVerification =>
            GetBoolOrDefault(nameof(BypassCertVerification), false);
        /// <summary>Transports to use</summary>
        public TransportOption Transport => Enum.Parse<TransportOption>(
            GetStringOrDefault(nameof(Transport), nameof(TransportOption.Any)), true);

        /// <summary>
        /// ClientServicesConfig
        /// </summary>
        private const string kAppCertStoreType = "AppCertStoreType";
        private const string kPkiRootPath = "PkiRootPath";
        private const string kOwnCertPath = "OwnCertPath";
        private const string kTrustedCertPath = "TrustedCertPath";
        private const string kIssuerCertPath = "IssuerCertPath";
        private const string kRejectedCertPath = "RejectedCertPath";
        private const string kAutoAccept = "AutoAccept";
        private const string kOwnCertX509StorePathDefault = "OwnCertX509StorePathDefault";

        /// <inheritdoc/>
        public string AppCertStoreType => GetStringOrDefault(kAppCertStoreType,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "X509Store" : "Directory");
        /// <inheritdoc/>
        public string PkiRootPath =>
            GetStringOrDefault(kPkiRootPath, "pki");
        /// <inheritdoc/>
        public string OwnCertPath =>
            GetStringOrDefault(kOwnCertPath, PkiRootPath + "/own");
        /// <inheritdoc/>
        public string TrustedCertPath =>
            GetStringOrDefault(kTrustedCertPath, PkiRootPath + "/trusted");
        /// <inheritdoc/>
        public string IssuerCertPath =>
            GetStringOrDefault(kIssuerCertPath, PkiRootPath + "/issuer");
        /// <inheritdoc/>
        public string RejectedCertPath =>
            GetStringOrDefault(kRejectedCertPath, PkiRootPath + "/rejected");
        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault =>
            GetStringOrDefault(kOwnCertX509StorePathDefault, "CurrentUser\\UA_MachineDefault");
        /// <inheritdoc/>
        public bool AutoAccept =>
            GetBoolOrDefault(kAutoAccept, false);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}

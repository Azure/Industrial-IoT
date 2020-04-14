// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Runtime {
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System.Runtime.InteropServices;
    using System;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class Config : DiagnosticsConfig, IModuleConfig, IClientServicesConfig2,
        ISecurityConfig, ITransportQuotaConfig, IClientServicesConfig {

        /// <inheritdoc/>
        public string EdgeHubConnectionString => _module.EdgeHubConnectionString;
        /// <inheritdoc/>
        public bool BypassCertVerification => _module.BypassCertVerification;
        /// <inheritdoc/>
        public TransportOption Transport => _module.Transport;
        /// <inheritdoc/>
        public string ApplicationName => _opc.ApplicationName;
        /// <inheritdoc/>
        public string ApplicationUri => _opc.ApplicationUri;
        /// <inheritdoc/>
        public string ProductUri => _opc.ProductUri;
        /// <inheritdoc/>
        public uint DefaultSessionTimeout => _opc.DefaultSessionTimeout;
        /// <inheritdoc/>
        public int KeepAliveInterval => _opc.KeepAliveInterval;
        /// <inheritdoc/>
        public uint MaxKeepAliveCount => _opc.MaxKeepAliveCount;
        /// <inheritdoc/>
        public int MinSubscriptionLifetime => _opc.MinSubscriptionLifetime;
        /// <inheritdoc/>
        public CertificateInfo ApplicationCertificate => _opc.ApplicationCertificate;
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates => _opc.AutoAcceptUntrustedCertificates;
        /// <inheritdoc/>
        public ushort MinimumCertificateKeySize => _opc.MinimumCertificateKeySize;
        /// <inheritdoc/>
        public CertificateStore RejectedCertificateStore => _opc.RejectedCertificateStore;
        /// <inheritdoc/>
        public bool RejectSha1SignedCertificates => _opc.RejectSha1SignedCertificates;
        /// <inheritdoc/>
        public CertificateStore TrustedIssuerCertificates => _opc.TrustedIssuerCertificates;
        /// <inheritdoc/>
        public CertificateStore TrustedPeerCertificates => _opc.TrustedPeerCertificates;
        /// <inheritdoc/>
        public int ChannelLifetime => _opc.ChannelLifetime;
        /// <inheritdoc/>
        public int MaxArrayLength => _opc.MaxArrayLength;
        /// <inheritdoc/>
        public int MaxBufferSize => _opc.MaxBufferSize;
        /// <inheritdoc/>
        public int MaxByteStringLength => _opc.MaxByteStringLength;
        /// <inheritdoc/>
        public int MaxMessageSize => _opc.MaxMessageSize;
        /// <inheritdoc/>
        public int MaxStringLength => _opc.MaxStringLength;
        /// <inheritdoc/>
        public int OperationTimeout => _opc.OperationTimeout;
        /// <inheritdoc/>
        public int SecurityTokenLifetime => _opc.SecurityTokenLifetime;

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
        private const string kSessionTimeout = "SessionTimeout";
        private const string kOperationTimeout = "OperationTimeout";

        /// <inheritdoc/>
        public string AppCertStoreType => GetStringOrDefault(kAppCertStoreType,
            () => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "X509Store" : "Directory");
        /// <inheritdoc/>
        public string PkiRootPath =>
            GetStringOrDefault(kPkiRootPath, () => "pki");
        /// <inheritdoc/>
        public string OwnCertPath =>
            GetStringOrDefault(kOwnCertPath, () => PkiRootPath + "/own");
        /// <inheritdoc/>
        public string TrustedCertPath =>
            GetStringOrDefault(kTrustedCertPath, () => PkiRootPath + "/trusted");
        /// <inheritdoc/>
        public string IssuerCertPath =>
            GetStringOrDefault(kIssuerCertPath, () => PkiRootPath + "/issuer");
        /// <inheritdoc/>
        public string RejectedCertPath =>
            GetStringOrDefault(kRejectedCertPath, () => PkiRootPath + "/rejected");
        /// <inheritdoc/>
        public string OwnCertX509StorePathDefault =>
            GetStringOrDefault(kOwnCertX509StorePathDefault, () => "CurrentUser\\UA_MachineDefault");
        /// <inheritdoc/>
        bool IClientServicesConfig.AutoAcceptUntrustedCertificates =>
            GetBoolOrDefault(kAutoAccept, () => false);
        /// <inheritdoc/>
        TimeSpan? IClientServicesConfig.DefaultSessionTimeout =>
            GetDurationOrNull(kSessionTimeout);
        /// <inheritdoc/>
        TimeSpan? IClientServicesConfig.OperationTimeout =>
            GetDurationOrNull(kOperationTimeout);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {
            _module = new ModuleConfig(configuration);
            _opc = new ClientServicesConfig2(configuration);
        }

        private readonly ClientServicesConfig2 _opc;
        private readonly ModuleConfig _module;
    }
}

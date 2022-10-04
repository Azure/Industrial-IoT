// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Default client configuration
    /// </summary>
    public class ClientServicesConfig : ConfigBase, IClientServicesConfig, ISecurityConfig, ITransportQuotaConfig {

        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string ApplicationNameKey = "ApplicationName";
        public const string ApplicationUriKey = "ApplicationUri";
        public const string ProductUriKey = "ProductUri";
        public const string DefaultSessionTimeoutKey = "DefaultSessionTimeout";
        public const string MinSubscriptionLifetimeKey = "MinSubscriptionLifetime";
        public const string KeepAliveIntervalKey = "KeepAliveInterval";
        public const string MaxKeepAliveCountKey = "MaxKeepAliveCount";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public string ApplicationName =>
            GetStringOrDefault(ApplicationNameKey, () => "Microsoft.Azure.IIoT");
        /// <inheritdoc/>
        public string ApplicationUri =>
            GetStringOrDefault(ApplicationUriKey, () => $"urn:localhost:{ApplicationName}:microsoft:");
        /// <inheritdoc/>
        public string ProductUri =>
            GetStringOrDefault(ProductUriKey, () => "https://www.github.com/Azure/Industrial-IoT");
        /// <inheritdoc/>
        public uint DefaultSessionTimeout =>
            (uint)GetIntOrDefault(DefaultSessionTimeoutKey, () => 60) * 1000;
        /// <inheritdoc/>
        public int MinSubscriptionLifetime =>
            GetIntOrDefault(MinSubscriptionLifetimeKey, () => 10) * 1000;
        /// <inheritdoc/>
        public int KeepAliveInterval =>
            GetIntOrDefault(KeepAliveIntervalKey, () => 10) * 1000;
        /// <inheritdoc/>
        public uint MaxKeepAliveCount =>
            (uint)GetIntOrDefault(MaxKeepAliveCountKey, () => 10);
        /// <inheritdoc/>
        public string PkiRootPath => _security.PkiRootPath;
        /// <inheritdoc/>
        public CertificateInfo ApplicationCertificate => _security.ApplicationCertificate;
        /// <inheritdoc/>
        public bool AutoAcceptUntrustedCertificates => _security.AutoAcceptUntrustedCertificates;
        /// <inheritdoc/>
        public ushort MinimumCertificateKeySize => _security.MinimumCertificateKeySize;
        /// <inheritdoc/>
        public CertificateStore RejectedCertificateStore => _security.RejectedCertificateStore;
        /// <inheritdoc/>
        public bool RejectSha1SignedCertificates => _security.RejectSha1SignedCertificates;
        /// <inheritdoc/>
        public bool AddAppCertToTrustedStore => _security.AddAppCertToTrustedStore;
        /// <inheritdoc/>
        public bool RejectUnknownRevocationStatus => _security.RejectUnknownRevocationStatus;
        /// <inheritdoc/>
        public CertificateStore TrustedIssuerCertificates => _security.TrustedIssuerCertificates;
        /// <inheritdoc/>
        public CertificateStore TrustedPeerCertificates => _security.TrustedPeerCertificates;

        /// <inheritdoc/>
        public int ChannelLifetime => _transport.ChannelLifetime;
        /// <inheritdoc/>
        public int MaxArrayLength => _transport.MaxArrayLength;
        /// <inheritdoc/>
        public int MaxBufferSize => _transport.MaxBufferSize;
        /// <inheritdoc/>
        public int MaxByteStringLength => _transport.MaxByteStringLength;
        /// <inheritdoc/>
        public int MaxMessageSize => _transport.MaxMessageSize;
        /// <inheritdoc/>
        public int MaxStringLength => _transport.MaxStringLength;
        /// <inheritdoc/>
        public int OperationTimeout => _transport.OperationTimeout;
        /// <inheritdoc/>
        public int SecurityTokenLifetime => _transport.SecurityTokenLifetime;

        /// <inheritdoc/>
        public ClientServicesConfig(IConfiguration configuration) : base(configuration) {
            _security = new SecurityConfig(this, configuration);
            _transport = new TransportQuotaConfig(configuration);
        }

        private readonly SecurityConfig _security;
        private readonly TransportQuotaConfig _transport;
    }
}
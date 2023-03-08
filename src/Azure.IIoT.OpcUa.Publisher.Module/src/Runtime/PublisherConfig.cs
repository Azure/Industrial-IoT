// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Furly.Azure.IoT.Edge;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Wraps a configuration root
    /// </summary>
    public class PublisherConfig : ConfigBase, IClientServicesConfig
    {
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
        public string PkiRootPath => _opc.PkiRootPath;
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
        public bool AddAppCertToTrustedStore => _opc.AddAppCertToTrustedStore;
        /// <inheritdoc/>
        public bool RejectUnknownRevocationStatus => _opc.RejectUnknownRevocationStatus;
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
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public PublisherConfig(IConfiguration configuration) :
            base(configuration)
        {
            _opc = new ClientServicesConfig(configuration);
        }

        private readonly ClientServicesConfig _opc;
    }
}

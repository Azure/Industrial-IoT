// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// Client's application configuration implementation
    /// </summary>
    public class TestClientServicesConfig : IClientServicesConfig, IDisposable {

        /// <inheritdoc/>
        public string PkiRootPath { get; }
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
        public bool AutoAcceptUntrustedCertificates { get; }
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

        /// <inheritdoc/>
        public TestClientServicesConfig(IConfiguration configuration = null, bool autoAccept = false) {
            AutoAcceptUntrustedCertificates = autoAccept;
            if (configuration == null) {
                PkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                    Guid.NewGuid().ToByteArray().ToBase16String());
                configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> {
                        {"PkiRootPath", PkiRootPath},
                        {"AutoAcceptUntrustedCertificates", autoAccept.ToString()}
                    })
                    .Build();
                _opc = new ClientServicesConfig(configuration);
            }
            else {
                _opc = new ClientServicesConfig(configuration);
                PkiRootPath = _opc.PkiRootPath;
            }
        }
        /// <inheritdoc/>
        public void Dispose() {
            if (Directory.Exists(PkiRootPath)) {
                Try.Op(() => Directory.Delete(PkiRootPath, true));
            }
        }

        private readonly ClientServicesConfig _opc;
    }
}

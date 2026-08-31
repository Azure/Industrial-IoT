// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.IoTEdge.Services
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge workload api.
    /// </summary>
    public interface IIoTEdgeWorkloadApi
    {
        /// <summary>
        /// Whether the Api is available and usable.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Decrypt cipher text.
        /// </summary>
        ValueTask<ReadOnlyMemory<byte>> DecryptAsync(string initializationVector,
            ReadOnlyMemory<byte> ciphertext, CancellationToken ct = default);

        /// <summary>
        /// Encrypt plain text.
        /// </summary>
        ValueTask<ReadOnlyMemory<byte>> EncryptAsync(string initializationVector,
            ReadOnlyMemory<byte> plaintext, CancellationToken ct = default);

        /// <summary>
        /// Sign data.
        /// </summary>
        ValueTask<ReadOnlyMemory<byte>> SignAsync(ReadOnlyMemory<byte> data,
            string? keyId = null, string? algo = null, CancellationToken ct = default);

        /// <summary>
        /// Create server certificate.
        /// </summary>
        ValueTask<X509Certificate2Collection> CreateServerCertificateAsync(
            string commonName, DateTime expiration, CancellationToken ct = default);

        /// <summary>
        /// Get trust bundle.
        /// </summary>
        ValueTask<X509Certificate2Collection> GetTrustBundleAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get manifest trust bundle.
        /// </summary>
        ValueTask<X509Certificate2Collection> GetManifestTrustBundleAsync(
            CancellationToken ct = default);
    }
}

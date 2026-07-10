// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Opc.Ua.Security.Certificates;

    /// <summary>
    /// Certificate store extensions
    /// </summary>
    public static class CertificateStoreEx
    {
        /// <summary>
        /// Add to certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificates"></param>
        /// <param name="noCopy"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/>
        /// is <c>null</c>.</exception>
        public static async Task AddAsync(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates,
            bool noCopy = false, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            foreach (var cert in certificates)
            {
                await Try.Async(() => store.DeleteAsync(cert.Thumbprint, ct)).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope
                await store.AddAsync(Certificate.From(noCopy ? cert : new X509Certificate2(cert)),
                    ct: ct).ConfigureAwait(false);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
        }

        /// <summary>
        /// Remove from certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificates"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/>
        /// is <c>null</c>.</exception>
        public static async Task RemoveAsync(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            foreach (var cert in certificates)
            {
                await store.DeleteAsync(cert.Thumbprint, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Apply the configured settings provided via a CertificateStore to a
        /// CertificateTrustList.
        /// </summary>
        /// <param name="certificateTrustList"></param>
        /// <param name="certificateStore"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateTrustList"/>
        /// is <c>null</c>.</exception>
        public static void ApplyLocalConfig(
            this CertificateTrustList certificateTrustList,
            CertificateStore? certificateStore)
        {
            ArgumentNullException.ThrowIfNull(certificateTrustList);

            if (certificateStore == null)
            {
                return;
            }

            if (certificateTrustList.StorePath != certificateStore.StorePath)
            {
                certificateTrustList.StoreType = certificateStore.StoreType;
                certificateTrustList.StorePath = certificateStore.StorePath;
            }
        }

        /// <summary>
        /// Applies the configuration settings to the own app certificate.
        /// </summary>
        /// <param name="certificateIdentifiers"></param>
        /// <param name="certificateStore"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateIdentifiers"/>
        /// is <c>null</c>.</exception>
        public static void ApplyLocalConfig(
            this CertificateIdentifierCollection certificateIdentifiers,
            CertificateInfo? certificateStore)
        {
            ArgumentNullException.ThrowIfNull(certificateIdentifiers);

            if (certificateStore == null)
            {
                return;
            }

            foreach (var certificateIdentifier in certificateIdentifiers)
            {
                if (certificateIdentifier.StorePath != certificateStore.StorePath)
                {
                    certificateIdentifier.StoreType = certificateStore.StoreType;
                    certificateIdentifier.StorePath = certificateStore.StorePath;
                }
            }
        }

        /// <summary>
        /// Applies the configuration settings to the own app certificate
        /// identifiers (2.0 immutable ArrayOf collection).
        /// </summary>
        /// <param name="certificateIdentifiers"></param>
        /// <param name="certificateStore"></param>
        public static void ApplyLocalConfig(
            this ArrayOf<CertificateIdentifier> certificateIdentifiers,
            CertificateInfo? certificateStore)
        {
            if (certificateStore == null)
            {
                return;
            }

            foreach (var certificateIdentifier in certificateIdentifiers)
            {
                if (certificateIdentifier.StorePath != certificateStore.StorePath)
                {
                    certificateIdentifier.StoreType = certificateStore.StoreType;
                    certificateIdentifier.StorePath = certificateStore.StorePath;
                }
            }
        }

        /// <summary>
        /// Applies the configuration settings to the own app certificate.
        /// </summary>
        /// <param name="certificateIdentifiers"></param>
        /// <param name="options"></param>
        /// <param name="noPrivateKey"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateIdentifiers"/>
        /// is <c>null</c>.</exception>
        public static ICertificateStore OpenStore(
            this CertificateIdentifierCollection certificateIdentifiers,
            SecurityOptions options, bool noPrivateKey = false)
        {
            ArgumentNullException.ThrowIfNull(certificateIdentifiers);
            if (certificateIdentifiers.Count > 0)
            {
                Debug.Assert(certificateIdentifiers
                    .All(x => x.StorePath == certificateIdentifiers[0].StorePath));
                Debug.Assert(certificateIdentifiers
                    .All(x => x.StoreType == certificateIdentifiers[0].StoreType));
                var identifier = certificateIdentifiers[0];
                return new CertificateStoreIdentifier(identifier.StorePath ?? string.Empty,
                    identifier.StoreType ?? string.Empty, noPrivateKey).OpenStore(null!);
            }

            ArgumentNullException.ThrowIfNull(options.ApplicationCertificates);
            return new CertificateStoreIdentifier(options.ApplicationCertificates.StorePath,
                options.ApplicationCertificates.StoreType, noPrivateKey).OpenStore(null!);
        }

        /// <summary>
        /// Open store from the 2.0 immutable ArrayOf certificate identifier
        /// collection.
        /// </summary>
        /// <param name="certificateIdentifiers"></param>
        /// <param name="options"></param>
        /// <param name="noPrivateKey"></param>
        public static ICertificateStore OpenStore(
            this ArrayOf<CertificateIdentifier> certificateIdentifiers,
            SecurityOptions options, bool noPrivateKey = false)
        {
            if (certificateIdentifiers.Count > 0)
            {
                var identifier = certificateIdentifiers[0];
                return new CertificateStoreIdentifier(identifier.StorePath ?? string.Empty,
                    identifier.StoreType ?? string.Empty, noPrivateKey).OpenStore(null!);
            }

            ArgumentNullException.ThrowIfNull(options.ApplicationCertificates);
            return new CertificateStoreIdentifier(options.ApplicationCertificates.StorePath,
                options.ApplicationCertificates.StoreType, noPrivateKey).OpenStore(null!);
        }

        /// <summary>
        /// Apply the configured settings provided via a CertificateStore to a
        /// CertificateStoreIdentifier. Particularily used for rejected
        /// certificates store.
        /// </summary>
        /// <param name="certificateStoreIdentifier"></param>
        /// <param name="certificateStore"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateStore"/>
        /// is <c>null</c>.</exception>
        public static void ApplyLocalConfig(
            this CertificateStoreIdentifier certificateStoreIdentifier,
            CertificateStore? certificateStore)
        {
            ArgumentNullException.ThrowIfNull(certificateStoreIdentifier);

            if (certificateStore == null)
            {
                return;
            }

            if (certificateStoreIdentifier.StorePath != certificateStore.StorePath)
            {
                certificateStoreIdentifier.StoreType = certificateStore.StoreType;
                certificateStoreIdentifier.StorePath = certificateStore.StorePath;
            }
        }
    }
}

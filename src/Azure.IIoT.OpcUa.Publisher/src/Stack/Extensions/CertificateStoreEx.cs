// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

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
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/>
        /// is <c>null</c>.</exception>
        public static void Add(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates,
            bool noCopy = false)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            foreach (var cert in certificates)
            {
                Try.Op(() => store.Delete(cert.Thumbprint));
#pragma warning disable CA2000 // Dispose objects before losing scope
                store.Add(noCopy ? cert : new X509Certificate2(cert));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
        }

        /// <summary>
        /// Remove from certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/>
        /// is <c>null</c>.</exception>
        public static void Remove(this ICertificateStore store,
            IEnumerable<X509Certificate2> certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            foreach (var cert in certificates)
            {
                store.Delete(cert.Thumbprint);
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
                return certificateIdentifiers[0].OpenStore();
            }

            ArgumentNullException.ThrowIfNull(options.ApplicationCertificates);
            return new CertificateStoreIdentifier(options.ApplicationCertificates.StorePath,
                options.ApplicationCertificates.StoreType, noPrivateKey).OpenStore();
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

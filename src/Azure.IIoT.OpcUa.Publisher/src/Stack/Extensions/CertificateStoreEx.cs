﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Utils;
    using System;
    using System.Collections.Generic;
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
        /// <param name="certificateIdentifier"></param>
        /// <param name="certificateStore"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateIdentifier"/>
        /// is <c>null</c>.</exception>
        public static void ApplyLocalConfig(
            this CertificateIdentifier certificateIdentifier,
            CertificateInfo? certificateStore)
        {
            ArgumentNullException.ThrowIfNull(certificateIdentifier);

            if (certificateStore == null)
            {
                return;
            }

            if (certificateIdentifier.StorePath != certificateStore.StorePath)
            {
                certificateIdentifier.StoreType = certificateStore.StoreType;
                certificateIdentifier.StorePath = certificateStore.StorePath;
            }
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

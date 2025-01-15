// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate Chain extensions
    /// </summary>
    public static class X509CertificateChainModelEx
    {
        /// <summary>
        /// Convert raw buffer to certificate chain
        /// </summary>
        /// <param name="rawCertificates"></param>
        /// <returns></returns>
        public static X509CertificateChainModel ToCertificateChain(
            this byte[] rawCertificates)
        {
            var certificates = new List<X509Certificate2>();
            try
            {
                while (true)
                {
                    var cur = X509CertificateLoader.LoadCertificate(rawCertificates);
                    certificates.Add(cur);
                    if (cur.RawData.Length >= rawCertificates.Length)
                    {
                        break;
                    }
                    rawCertificates = rawCertificates.AsSpan()[cur.RawData.Length..]
                        .ToArray();
                }
                return new X509CertificateChainModel
                {
                    Chain = certificates
                        .ConvertAll(c => c.ToServiceModel())
                };
            }
            finally
            {
                certificates.ForEach(c => c.Dispose());
            }
        }

        /// <summary>
        /// Gets the leaf thumprint
        /// </summary>
        /// <param name="rawCertificates"></param>
        /// <returns></returns>
        public static string? ToThumbprint(this byte[] rawCertificates)
        {
            try
            {
                var chain = rawCertificates.ToCertificateChain()?.Chain;
                if (chain?.Count > 0)
                {
                    return chain[chain.Count - 1]?.Thumbprint;
                }
                return null;
            }
            catch
            {
                // Fall back to sha1 which was the previous thumprint algorithm
                return rawCertificates.ToSha1Hash();
            }
        }
    }
}

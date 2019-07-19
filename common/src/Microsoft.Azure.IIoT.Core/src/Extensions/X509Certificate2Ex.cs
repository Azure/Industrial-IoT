// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// X509 cert extensions
    /// </summary>
    public static class X509Certificate2Ex {

        /// <summary>
        /// Gets raw data from chain
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this X509Certificate2Collection chain) {
            var serverCertificateChain = new List<byte>();
            for (var i = 0; i < chain.Count; i++) {
                serverCertificateChain.AddRange(chain[i].RawData);
            }
            return serverCertificateChain.ToArray();
        }

        /// <summary>
        /// Parse certs out of pem certs list
        /// </summary>
        /// <param name="pemCerts"></param>
        /// <returns></returns>
        public static IEnumerable<X509Certificate2> ParsePemCerts(string pemCerts) {
            if (string.IsNullOrEmpty(pemCerts)) {
                throw new ArgumentNullException(pemCerts);
            }
            // Extract each certificate's string. The final string from the split will either be empty
            // or a non-certificate entry, so it is dropped.
            var delimiter = "-----END CERTIFICATE-----";
            var rawCerts = pemCerts.Split(new[] { delimiter }, StringSplitOptions.None);
            return rawCerts
                .Take(rawCerts.Count() - 1) // Drop the invalid entry
                .Select(c => $"{c}{delimiter}")
                .Select(Encoding.UTF8.GetBytes)
                .Select(c => new X509Certificate2(c));
        }
    }
}

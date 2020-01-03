// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Ssl {
    using System.Net.Http.Headers;
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// No ssl validation
    /// </summary>
    public class NoOpCertValidator : IHttpCertificateValidator {

        /// <inheritdoc/>
        public Func<string, bool> IsFor { get; set; }

        /// <inheritdoc/>
        public void Configure(IHttpHandlerHost host) {
            // No op
        }

        /// <inheritdoc/>
        public virtual bool Validate(HttpRequestHeaders headers,
            X509Certificate2 cert, X509Chain chain, SslPolicyErrors? errors) {
            return true;
        }
    }
}

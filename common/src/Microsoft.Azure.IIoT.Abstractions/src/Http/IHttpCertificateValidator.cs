// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Implement to validate server certificates
    /// </summary>
    public interface IHttpCertificateValidator : IHttpHandler {

        /// <summary>
        /// Validate ssl certificate
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="cert"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        bool Validate(HttpRequestHeaders headers, X509Certificate2 cert,
            X509Chain chain, SslPolicyErrors? errors);
    }
}

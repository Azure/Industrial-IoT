// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Crypto {

    /// <summary>
    /// Issuer configuration
    /// </summary>
    public interface ICertificateFactoryConfig {

        /// <summary>
        /// Crl Distribution point template
        /// </summary>
        string AuthorityCrlRootUrl { get; }

        /// <summary>
        /// Authority information access template
        /// </summary>
        string AuthorityInfoRootUrl { get; }
    }
}

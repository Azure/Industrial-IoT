// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Ssl certificate provider
    /// </summary>
    public interface ISslCertProvider
    {
        /// <summary>
        /// Certificate
        /// </summary>
        X509Certificate2? Certificate { get; }
    }
}

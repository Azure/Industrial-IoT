// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Client stack services
    /// </summary>
    public interface IClientHost {

        /// <summary>
        /// Returns the client certificate
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// Update client certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        Task UpdateClientCertificate(X509Certificate2 certificate);
    }
}

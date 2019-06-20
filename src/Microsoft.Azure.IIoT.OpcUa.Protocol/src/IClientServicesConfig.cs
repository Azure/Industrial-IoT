// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using System;


    /// <summary>
    /// Client Services configuration
    /// </summary>

    public interface IClientServicesConfig {

        /// <summary>
        /// root path of the pki security folder 
        /// </summary>
        string PkiRootFolder { get; }
        /// <summary>
        /// Path to the folder storing the application's certificate
        /// </summary>
        string ApplicationCertificateFolder { get; }
        
        /// <summary>
        /// Path to the folder storing the application's trusted certificates
        /// </summary>
        string TrustedPeerCertificatesFolder { get; }

        /// <summary>
        /// Path of the folder to store the trueste issuer (CA) certificates
        /// </summary>
        string TrustedIssuerCertificatesFolder { get; }

        /// <summary>
        /// Path of the folder to store the rejected certificates
        /// </summary>
        /// 
        string RejectedCertificatesFolder { get; }

        /// <summary>
        /// flag to force automatically acceptance of untrusted certificates
        /// </summary>
        bool AutoAcceptUntrustedCertificates { get; }

    }
}

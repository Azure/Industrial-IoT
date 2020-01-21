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
        /// application's certificate store type: Directory or X509Store
        /// </summary>
        string AppCertStoreType { get; }

        /// <summary>
        /// root path of the pki security folder
        /// </summary>
        string PkiRootPath { get; }

        /// <summary>
        /// Path to the folder storing the application's certificate
        /// </summary>
        string OwnCertPath { get; }

        /// <summary>
        /// Path to the folder storing the application's trusted certificates
        /// </summary>
        string TrustedCertPath { get; }

        /// <summary>
        /// Path of the folder to store the trueste issuer (CA) certificates
        /// </summary>
        string IssuerCertPath { get; }

        /// <summary>
        /// Path of the folder to store the rejected certificates
        /// </summary>
        ///
        string RejectedCertPath { get; }

        /// <summary>
        /// flag to force automatically acceptance of untrusted certificates
        /// </summary>
        bool AutoAcceptUntrustedCertificates { get; }

        /// <summary>
        /// the default store path for the windows X509 certificates store in case
        /// X509Store store type is used
        /// </summary>
        string OwnCertX509StorePathDefault { get; }

        /// <summary>
        /// Default session timeout for client
        /// </summary>
        TimeSpan? DefaultSessionTimeout { get; }

        /// <summary>
        /// Default operation timeout
        /// </summary>
        TimeSpan? OperationTimeout { get; }
    }
}

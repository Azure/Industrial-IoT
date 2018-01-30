// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions {
    using System;

    /// <summary>
    /// Thrown when the external resource is busy
    /// </summary>
    public class CertificateUntrustedException : ExternalDependencyException {

        public CertificateUntrustedException(string message) : 
            base(message) {
        }

        public CertificateUntrustedException(string message, Exception innerException) : 
            base(message, innerException) {
        }
    }
}
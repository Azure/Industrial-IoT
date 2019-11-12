// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {

    /// <summary>
    /// Certificate store
    /// </summary>
    public class CertificateStore {

        /// <summary>
        /// Store type
        /// </summary>
        public string StoreType { get; set; }

        /// <summary>
        /// Store path
        /// </summary>
        public string StorePath { get; set; }
    }
}
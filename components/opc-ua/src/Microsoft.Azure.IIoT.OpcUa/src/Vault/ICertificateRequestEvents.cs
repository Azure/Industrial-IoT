// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using System;

    /// <summary>
    /// Emits Certificate Request events
    /// </summary>
    public interface ICertificateRequestEvents {

        /// <summary>
        /// Register listener
        /// </summary>
        /// <returns></returns>
        Action Register(ICertificateRequestListener listener);
    }
}

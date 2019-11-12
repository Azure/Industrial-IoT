// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {

    /// <summary>
    /// Encoding configuration
    /// </summary>
    public interface IEncodingConfig {

        /// <summary>
        /// Content type encoded to
        /// </summary>
        string ContentType { get; }
    }
}
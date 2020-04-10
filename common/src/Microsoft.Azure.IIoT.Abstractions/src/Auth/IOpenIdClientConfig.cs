// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    /// <summary>
    /// Extension for openid clients
    /// </summary>
    public interface IOpenIdClientConfig : IOAuthClientConfig {

        /// <summary>
        /// Client uri
        /// </summary>
        string ClientUri { get; }
    }
}

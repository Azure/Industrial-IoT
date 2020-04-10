// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.Auth {
    /// <summary>
    /// Extension for swagger clients
    /// see https://swagger.io/docs/specification/authentication/oauth2/
    /// </summary>
    public interface IOpenApiClientConfig : IOAuthClientConfig {

        /// <summary>
        /// Redirect Uris
        /// </summary>
        List<string> RedirectUris { get; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System.Collections.Generic;

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

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Configuration interface for server authentication
    /// </summary>
    public interface IServerAuthConfig {

        /// <summary>
        /// Allow anonymous access
        /// </summary>
        bool AllowAnonymousAccess { get; }

        /// <summary>
        /// Supported providers
        /// </summary>
        IEnumerable<IOAuthServerConfig> JwtBearerProviders { get; }
    }
}

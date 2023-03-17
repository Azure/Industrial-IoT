// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth
{
    using System;

    /// <summary>
    /// Configuration interface for server token validation
    /// </summary>
    public interface IOAuthServerConfig : IOAuthConfig
    {
        /// <summary>
        /// Our service's id or url that was registered and
        /// that we must validate to be the audience of the
        /// token so to ensure the token was actually issued
        /// for us.
        /// </summary>
        string Audience { get; }
    }
}

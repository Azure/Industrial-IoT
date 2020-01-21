//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.AspNetCore.Http;
    using System.Security.Authentication;

    /// <summary>
    /// Handle error
    /// </summary>
    public interface IAuthenticationErrorHandler {

        /// <summary>
        /// Handle authentication error
        /// </summary>
        void Handle(HttpContext context, AuthenticationException ex);
    }
}

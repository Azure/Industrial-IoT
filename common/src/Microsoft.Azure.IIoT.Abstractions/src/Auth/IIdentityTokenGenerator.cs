// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;

    /// <summary>
    /// Provides a short term and transparent identity token for
    /// a user.
    /// </summary>
    public interface IIdentityTokenGenerator {

        /// <summary>
        /// Creates a opaque identity token for an identity to
        /// connect to Service.
        /// </summary>
        /// <param name="identity">The identity requesting access.
        /// </param>
        /// <param name="claims">The claim list to be put into
        /// identity token.</param>
        /// <param name="lifeTime">The lifetime of the token.
        /// </param>
        /// <returns>Client identity token</returns>
        IdentityTokenModel GenerateIdentityToken(string identity = null,
            IList<Claim> claims = null, TimeSpan? lifeTime = default);
    }
}
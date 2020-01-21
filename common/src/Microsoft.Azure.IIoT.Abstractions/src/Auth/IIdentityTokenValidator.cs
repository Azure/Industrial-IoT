// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Validates identity token
    /// </summary>
    public interface IIdentityTokenValidator {

        /// <summary>
        /// Validate token and throw if not valid
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task ValidateToken(IdentityTokenModel token);
    }
}
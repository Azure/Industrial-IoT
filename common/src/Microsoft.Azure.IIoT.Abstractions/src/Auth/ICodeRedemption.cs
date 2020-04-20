// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// Code redemption
    /// </summary>
    public interface ICodeRedemption {

        /// <summary>
        /// Scheme to use for redemption
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// Redeem token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="code"></param>
        /// <param name="scopes"></param>
        /// <returns></returns>
        Task<TokenResultModel> RedeemCodeForUserAsync(
            ClaimsPrincipal user, string code,
            IEnumerable<string> scopes);

        /// <summary>
        /// Redeem token
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task SignOutUserAsync(ClaimsPrincipal user);
    }
}
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Identity token source
    /// </summary>
    public interface IIdentityTokenStore {

        /// <summary>
        /// Get identity token
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task<IdentityTokenModel> GetIdentityTokenAsync(string identity);
    }
}
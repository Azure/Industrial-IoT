// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an identity token
    /// </summary>
    public interface IIdentityTokenProvider {

        /// <summary>
        /// Current Token
        /// </summary>
        IdentityTokenModel IdentityToken { get; }

        /// <summary>
        /// Force token update
        /// </summary>
        /// <returns></returns>
        Task ForceUpdate();
    }
}
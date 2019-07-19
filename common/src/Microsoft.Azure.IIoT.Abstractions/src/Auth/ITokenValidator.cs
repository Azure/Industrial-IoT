//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable token validator
    /// </summary>
    public interface ITokenValidator {

        /// <summary>
        /// Validate token string and returns token result model
        /// if the token was successfully authenticated.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TokenResultModel> ValidateAsync(string token,
            CancellationToken ct = default);
    }
}

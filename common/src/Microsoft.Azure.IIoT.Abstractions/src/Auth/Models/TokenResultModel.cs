// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using System;

    /// <summary>
    /// Contains the results of one token acquisition
    /// operation.
    /// </summary>
    public sealed class TokenResultModel {

        /// <summary>
        /// Gets the authority that has issued the token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets the type of the Access Token returned.
        /// </summary>
        public string TokenType { get; set; }

        /// <summary>
        /// Token requested
        /// </summary>
        public string RawToken { get; set; }

        /// <summary>
        /// Signature algorithm used for token
        /// </summary>
        public string SignatureAlgorithm { get; set; }

        /// <summary>
        /// Gets the point in time in which the Access Token
        /// returned in the AccessToken property ceases to be
        /// valid. This value is calculated based on current
        /// UTC time measured locally and the value expiresIn
        /// received from the service.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; set; }

        /// <summary>
        /// Gets an identifier for the tenant the token was
        /// acquired from. This property will be null if
        /// tenant information is not returned by the service.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets user information including user Id. Some
        /// elements in UserInfo might be null if not
        /// returned by the service.
        /// </summary>
        public UserInfoModel UserInfo { get; set; }

        /// <summary>
        /// Gets the entire Id Token if returned by the
        /// service or null if no Id Token is returned.
        /// </summary>
        public string IdToken { get; set; }

        /// <summary>
        /// Do not cache the token
        /// </summary>
        public bool Cached { get; set; }
    }
}
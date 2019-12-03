// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using System;

    /// <summary>
    /// Contains information of a single user. This information is
    /// used for token cache lookup. Also if created with userId,
    /// userId is sent to the service when login_hint is accepted.
    /// </summary>
    public sealed class UserInfoModel {

        /// <summary>
        /// Gets identifier of the user authenticated
        /// during token acquisition.
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Gets a displayable value in UserPrincipalName
        /// (UPN) format. The value can be null.
        /// </summary>
        public string DisplayableId { get; set; }

        /// <summary>
        /// Gets given name of the user if provided.
        /// If not, the value is null.
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Gets family name of the user if provided.
        /// If not, the value is null.
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets user email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets the time when the password expires.
        /// Default value is null.
        /// </summary>
        public DateTimeOffset? PasswordExpiresOn { get; set; }

        /// <summary>
        /// Gets the url where the user can change the expiring
        /// password. The value can be null.
        /// </summary>
        public Uri PasswordChangeUrl { get; set; }

        /// <summary>
        /// Gets identity provider if returned by
        /// the service. If not, the value is null.
        /// </summary>
        public string IdentityProvider { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// User model
    /// </summary>
    [DataContract]
    public class UserApiModel {

        /// <summary>
        /// Unique ID of the user
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user name for this user.
        /// </summary>
        [DataMember(Name = "name")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the email address for this user.
        /// </summary>
        [DataMember(Name = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if a user has
        /// confirmed their email address.
        /// </summary>
        [DataMember(Name = "emailConfirmed")]
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// A random value that must change whenever a users
        /// credentials change (password changed, login removed)
        /// </summary>
        [DataMember(Name = "securityStamp")]
        public string SecurityStamp { get; set; }

        /// <summary>
        /// Gets or sets a telephone number for the user.
        /// </summary>
        [DataMember(Name = "phoneNumber")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if a user has
        /// confirmed their telephone address.
        /// </summary>
        [DataMember(Name = "phoneNumberConfirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if two factor
        /// authentication is enabled for
        /// this user.
        /// </summary>
        [DataMember(Name = "twoFactorEnabled")]
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// Gets or sets the date and time, in UTC, when
        /// any user lockout ends.
        /// </summary>
        [DataMember(Name = "lockoutEnd")]
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the user
        /// could be locked out.
        /// </summary>
        [DataMember(Name = "lockoutEnabled")]
        public bool LockoutEnabled { get; set; }

        /// <summary>
        /// Gets or sets the number of failed login attempts
        /// for the current user.
        /// </summary>
        [DataMember(Name = "accessFailedCount")]
        public int AccessFailedCount { get; set; }
    }
}

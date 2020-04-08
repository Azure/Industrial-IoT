// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// User login
    /// </summary>
    [DataContract]
    public class LoginApiModel {

        /// <summary>
        /// Login provider for example Local,
        /// Facebook, Google, etc
        /// </summary>
        [DataMember(Name = "loginProvider", Order = 0)]
        public string LoginProvider { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier
        /// for the user identity user provided
        /// by the login provider.
        /// </summary>
        [DataMember(Name = "providerKey", Order = 1)]
        public string ProviderKey { get; set; }

        /// <summary>
        /// Gets or sets the display name for
        /// the provider.
        /// </summary>
        [DataMember(Name = "providerDisplayName", Order = 2)]
        public string ProviderDisplayName { get; set; }
    }
}

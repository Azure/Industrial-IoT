// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Models access to an API resource
    /// </summary>
    [DataContract]
    public class ScopeModel {

        /// <summary>
        /// Name of the scope. This is the value a client
        /// will use to request the scope.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Display name. This value will be used
        /// on the consent screen.
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// Description. This value will be used
        /// on the consent screen.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Specifies whether the user can de-select the
        /// scope on the consent screen.
        /// </summary>
        [DataMember]
        public bool Required { get; set; }

        /// <summary>
        /// Specifies whether the consent screen will
        /// emphasize this scope. Use this setting for
        /// sensitive or important scopes.
        /// </summary>
        [DataMember]
        public bool Emphasize { get; set; }

        /// <summary>
        /// Specifies whether this scope is shown in the
        /// discovery document.
        /// </summary>
        [DataMember]
        public bool ShowInDiscoveryDocument { get; set; } = true;

        /// <summary>
        /// List of user-claim types that should be
        /// included in the access token.
        /// </summary>
        [DataMember]
        public List<string> UserClaims { get; set; }
    }
}
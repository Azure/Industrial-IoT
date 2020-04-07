// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint Activation Filter model
    /// </summary>
    [DataContract]
    public class EndpointActivationFilterApiModel {

        /// <summary>
        /// Certificate trust list identifiers to use for
        /// activation, if null, all certificates are
        /// trusted.  If empty list, no certificates are
        /// trusted which is equal to no filter.
        /// </summary>
        [DataMember(Name = "trustLists", Order = 0,
            EmitDefaultValue = false)]
        public List<string> TrustLists { get; set; }

        /// <summary>
        /// Endpoint security policies to filter against.
        /// If set to null, all policies are in scope.
        /// </summary>
        [DataMember(Name = "securityPolicies", Order = 1,
           EmitDefaultValue = false)]
        public List<string> SecurityPolicies { get; set; }

        /// <summary>
        /// Security mode level to activate. If null,
        /// then <see cref="SecurityMode.Best"/> is assumed.
        /// </summary>
        [DataMember(Name = "securityMode", Order = 2,
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }
    }
}


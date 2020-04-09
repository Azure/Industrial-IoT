// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery results
    /// </summary>
    [DataContract]
    public class DiscoveryResultListApiModel {

        /// <summary>
        /// Result
        /// </summary>
        [DataMember(Name = "result", Order = 0)]
        public DiscoveryResultApiModel Result { get; set; }

        /// <summary>
        /// Events
        /// </summary>
        [DataMember(Name = "events", Order = 1)]
        public List<DiscoveryEventApiModel> Events { get; set; }
    }
}
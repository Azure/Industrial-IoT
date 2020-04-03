// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Discovery event
    /// </summary>
    [DataContract]
    public class DiscoveryEventApiModel {

        /// <summary>
        /// Timestamp of the discovery sweep.
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 0)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        [DataMember(Name = "index", Order = 1)]
        public int Index { get; set; }

        /// <summary>
        /// Discovered endpoint in form of endpoint registration
        /// </summary>
        [DataMember(Name = "registration", Order = 2)]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application to which this endpoint belongs
        /// </summary>
        [DataMember(Name = "application", Order = 3)]
        public ApplicationInfoApiModel Application { get; set; }
    }
}
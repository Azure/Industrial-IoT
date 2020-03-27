// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher event
    /// </summary>
    [DataContract]
    public class PublisherEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType")]
        public PublisherEventType EventType { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Publisher
        /// </summary>
        [DataMember(Name = "publisher",
            EmitDefaultValue = false)]
        public PublisherApiModel Publisher { get; set; }
    }
}
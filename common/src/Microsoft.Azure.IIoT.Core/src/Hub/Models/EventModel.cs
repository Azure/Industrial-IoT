// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model of an event
    /// </summary>
    [DataContract]
    public class EventModel {

        /// <summary>
        /// Properties of the event
        /// </summary>
        [DataMember(Name = "properties")]
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Payload of event
        /// </summary>
        [DataMember(Name = "payload")]
        public VariantValue Payload { get; set; }
    }
}

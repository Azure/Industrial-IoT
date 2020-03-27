// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Application event
    /// </summary>
    [DataContract]
    public class ApplicationEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType")]
        public ApplicationEventType EventType { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        [DataMember(Name = "application",
            EmitDefaultValue = false)]
        public ApplicationInfoApiModel Application { get; set; }
    }
}
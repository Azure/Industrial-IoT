// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Twin service method results model
    /// </summary>
    [DataContract]
    public class MethodResultModel {

        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status")]
        public int Status { get; set; }

        /// <summary>
        /// Response payload
        /// TODO: replace with variantvalue
        /// </summary>
        [DataMember(Name = "jsonPayload")]
        public string JsonPayload { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Read data at specified times
    /// </summary>
    [DataContract]
    public class ReadValuesAtTimesDetailsApiModel {

        /// <summary>
        /// Requested datums
        /// </summary>
        [DataMember(Name = "reqTimes", Order = 0)]
        [Required]
        public DateTime[] ReqTimes { get; set; }

        /// <summary>
        /// Whether to use simple bounds
        /// </summary>
        [DataMember(Name = "useSimpleBounds", Order = 1,
            EmitDefaultValue = false)]
        public bool? UseSimpleBounds { get; set; }
    }
}

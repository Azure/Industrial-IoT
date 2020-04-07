// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Upload start request model
    /// </summary>
    [DataContract]
    public class ModelUploadStartRequestApiModel {

        /// <summary>
        /// Desired content encoding
        /// </summary>
        [DataMember(Name = "contentEncoding", Order = 0,
            EmitDefaultValue = false)]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [DataMember(Name = "diagnostics", Order = 1,
            EmitDefaultValue = false)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}

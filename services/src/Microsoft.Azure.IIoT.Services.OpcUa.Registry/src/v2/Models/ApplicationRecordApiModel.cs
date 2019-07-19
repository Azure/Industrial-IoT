// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    public class ApplicationRecordApiModel {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="application"></param>
        public ApplicationRecordApiModel(ApplicationRecordModel application) {
            Application = new ApplicationInfoApiModel(application.Application);
            RecordId = application.RecordId;
        }

        /// <summary>
        /// Record id
        /// </summary>
        [JsonProperty(PropertyName = "recordId")]
        [Required]
        public uint RecordId { get; set; }

        /// <summary>
        /// Application information
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        [Required]
        public ApplicationInfoApiModel Application { get; set; }
    }
}
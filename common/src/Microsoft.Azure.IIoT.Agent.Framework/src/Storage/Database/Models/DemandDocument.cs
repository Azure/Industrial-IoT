// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Demand model
    /// </summary>
    public class DemandDocument {

        /// <summary>
        /// id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        public string ClassType { get; set; } = ClassTypeName;
        /// <summary/>
        public static readonly string ClassTypeName = "Demand";

        /// <summary>
        /// Identifier of the job document
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Match operator
        /// </summary>
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }
}
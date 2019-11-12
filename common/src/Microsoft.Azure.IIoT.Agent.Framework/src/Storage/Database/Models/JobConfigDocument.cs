// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Job model
    /// </summary>
    public class JobConfigDocument {

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
        public static readonly string ClassTypeName = "JobConfig";

        /// <summary>
        /// Identifier of the job document
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Job description
        /// </summary>
        public JObject Job { get; set; }
    }
}
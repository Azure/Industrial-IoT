// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Worker document
    /// </summary>
    public class WorkerSupervisorDocument {

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
        public static readonly string ClassTypeName = "WorkerSupervisor";

        /// <summary>
        /// Worker Supervisor status
        /// </summary>
        public SupervisorStatus WorkerStatus { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}
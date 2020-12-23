// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Worker document
    /// </summary>
    [DataContract]
    public class WorkerDocument {

        /// <summary>
        /// id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [DataMember(Name = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        [DataMember]
        public string ClassType { get; set; } = ClassTypeName;
        /// <summary/>
        public static readonly string ClassTypeName = "Worker";

        /// <summary>
        /// Agent id
        /// </summary>
        [DataMember]
        public string AgentId { get; internal set; }

        /// <summary>
        /// Worker status
        /// </summary>
        [DataMember]
        public WorkerStatus WorkerStatus { get; set; }

        /// <summary>
        /// Last seen
        /// </summary>
        [DataMember]
        public DateTime LastSeen { get; set; }
    }
}
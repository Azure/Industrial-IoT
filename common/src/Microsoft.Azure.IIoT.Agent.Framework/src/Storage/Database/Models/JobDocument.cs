// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Job model
    /// </summary>
    [DataContract]
    public class JobDocument {

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
        public static readonly string ClassTypeName = "Job";

        /// <summary>
        /// Identifier of the job document
        /// </summary>
        [DataMember]
        public string JobId { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Configuration type
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Job configuration
        /// </summary>
        [DataMember]
        public JobConfigDocument JobConfiguration { get; set; }

        /// <summary>
        /// Demands
        /// </summary>
        [DataMember]
        public List<DemandDocument> Demands { get; set; }

        /// <summary>
        /// Number of desired active agents
        /// </summary>
        [DataMember]
        public int DesiredActiveAgents { get; set; }

        /// <summary>
        /// Number of passive agents
        /// </summary>
        [DataMember]
        public int DesiredPassiveAgents { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [DataMember]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        [DataMember]
        public Dictionary<string, ProcessingStatusDocument> ProcessingStatus { get; set; }

        /// <summary>
        /// Updated at
        /// </summary>
        [DataMember]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Created at
        /// </summary>
        [DataMember]
        public DateTime Created { get; set; }
    }
}
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Job model
    /// </summary>
    [DataContract]
    public class JobConfigDocument {

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
        public static readonly string ClassTypeName = "JobConfig";

        /// <summary>
        /// Identifier of the job document
        /// </summary>
        [DataMember]
        public string JobId { get; set; }

        /// <summary>
        /// Job description
        /// </summary>
        [DataMember]
        public VariantValue Job { get; set; }
    }
}
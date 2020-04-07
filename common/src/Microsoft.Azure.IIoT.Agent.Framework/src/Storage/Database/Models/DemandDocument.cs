// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Demand model
    /// </summary>
    [DataContract]
    public class DemandDocument {

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
        public static readonly string ClassTypeName = "Demand";

        /// <summary>
        /// Identifier of the job document
        /// </summary>
        [DataMember]
        public string JobId { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        [DataMember]
        public string Key { get; set; }

        /// <summary>
        /// Match operator
        /// </summary>
        [DataMember]
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }
}
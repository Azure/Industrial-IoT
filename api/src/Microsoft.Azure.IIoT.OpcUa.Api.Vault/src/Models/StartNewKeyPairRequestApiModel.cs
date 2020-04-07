// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// New key pair request
    /// </summary>
    [DataContract]
    public sealed class StartNewKeyPairRequestApiModel {

        /// <summary>
        /// Entity id
        /// </summary>
        [DataMember(Name = "entityId", Order = 0)]
        public string EntityId { get; set; }

        /// <summary>
        /// Certificate group
        /// </summary>
        [DataMember(Name = "groupId", Order = 1)]
        public string GroupId { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [DataMember(Name = "certificateType", Order = 2)]
        public TrustGroupType CertificateType { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        [DataMember(Name = "subjectName", Order = 3)]
        public string SubjectName { get; set; }

        /// <summary>
        /// Domain names
        /// </summary>
        [DataMember(Name = "domainNames", Order = 4)]
        public List<string> DomainNames { get; set; }
    }
}

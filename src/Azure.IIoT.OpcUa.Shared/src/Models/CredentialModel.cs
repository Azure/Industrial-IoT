// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Credential model
    /// </summary>
    [DataContract]
    public record class CredentialModel {

        /// <summary>
        /// Type of credential
        /// </summary>
        [DataMember(Name = "type", Order = 0,
            EmitDefaultValue = false)]
        public CredentialType? Type { get; set; }

        /// <summary>
        /// Credential to pass to server.
        /// </summary>
        [DataMember(Name = "value", Order = 1,
            EmitDefaultValue = false)]
        public VariantValue Value { get; set; }
    }
}

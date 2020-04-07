// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Enum definition
    /// </summary>
    [DataContract]
    public class EnumDefinitionApiModel {

        /// <summary>
        /// The fields of the enum
        /// </summary>
        [DataMember(Name = "fields")]
        public List<EnumFieldApiModel> Fields { get; set; }
    }
}

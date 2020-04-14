// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute write
    /// </summary>
    [DataContract]
    public class WriteResponseApiModel {

        /// <summary>
        /// All results of attribute writes
        /// </summary>
        [DataMember(Name = "results", Order = 0)]
        public List<AttributeWriteResponseApiModel> Results { set; get; }
    }
}

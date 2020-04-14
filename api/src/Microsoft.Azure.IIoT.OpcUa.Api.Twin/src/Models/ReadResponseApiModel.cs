// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Result of attribute reads
    /// </summary>
    [DataContract]
    public class ReadResponseApiModel {

        /// <summary>
        /// All results of attribute reads
        /// </summary>
        [DataMember(Name = "results", Order = 0)]
        public List<AttributeReadResponseApiModel> Results { set; get; }
    }
}

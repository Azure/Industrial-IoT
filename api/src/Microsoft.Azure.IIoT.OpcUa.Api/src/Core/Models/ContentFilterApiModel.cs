// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Content filter
    /// </summary>
    [DataContract]
    public class ContentFilterApiModel {

        /// <summary>
        /// The flat list of elements in the filter AST
        /// </summary>
        [DataMember(Name = "elements", Order = 0)]
        public List<ContentFilterElementApiModel> Elements { get; set; }
    }
}
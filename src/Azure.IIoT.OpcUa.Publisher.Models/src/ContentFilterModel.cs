// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Content filter
    /// </summary>
    [DataContract]
    public sealed record class ContentFilterModel
    {
        /// <summary>
        /// The flat list of elements in the filter AST
        /// </summary>
        [DataMember(Name = "elements", Order = 0)]
        public IReadOnlyList<ContentFilterElementModel>? Elements { get; set; }
    }
}

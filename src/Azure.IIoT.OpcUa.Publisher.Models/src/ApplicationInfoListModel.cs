// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// List of registered applications
    /// </summary>
    [DataContract]
    public sealed record class ApplicationInfoListModel
    {
        /// <summary>
        /// Application infos
        /// </summary>
        [DataMember(Name = "items", Order = 0)]
        public required IReadOnlyList<ApplicationInfoModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 1,
            EmitDefaultValue = false)]
        public string? ContinuationToken { get; set; }
    }
}

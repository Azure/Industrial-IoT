// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Historic event
    /// </summary>
    [DataContract]
    public sealed record class HistoricEventModel
    {
        /// <summary>
        /// The selected fields of the event
        /// </summary>
        [DataMember(Name = "eventFields", Order = 0)]
        [SkipValidation]
        public required IReadOnlyList<JsonNode?> EventFields { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for monitored item node errors
    /// </summary>
    [DataContract]
    public record class PublishedDataSetSourceDiagnosticModel
    {
        /// <summary>
        /// Error information for nodes inside the writer source
        /// </summary>
        [DataMember(Name = "errors", Order = 1,
            EmitDefaultValue = true)]
        public IReadOnlyList<MonitoredItemNodeErrorModel>? Errors { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes how model changes are published
    /// </summary>
    [DataContract]
    public sealed record class ModelChangeHandlingOptionsModel
    {
        /// <summary>
        /// Rebrowse period
        /// </summary>
        [DataMember(Name = "rebrowseIntervalTimespan", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? RebrowseIntervalTimespan { get; set; }
    }
}

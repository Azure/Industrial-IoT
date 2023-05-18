// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Query type
    /// </summary>
    [DataContract]
    public enum QueryType
    {
        /// <summary>
        /// Query is an event filter
        /// </summary>
        [EnumMember(Value = "Event")]
        Event,

        /// <summary>
        /// Query is a query description
        /// </summary>
        [EnumMember(Value = "Query")]
        Query
    }
}

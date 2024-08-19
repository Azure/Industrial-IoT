// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Wraps a request and a published nodes entry to bind to a
    /// body more easily for api that requires an entry and additional
    /// configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public sealed record class PublishedNodesEntryRequestModel<T>
    {
        /// <summary>
        /// Published nodes entry
        /// </summary>
        [DataMember(Name = "entry", Order = 0)]
        [Required]
        public required PublishedNodesEntryModel Entry { get; init; }

        /// <summary>
        /// Request
        /// </summary>
        [DataMember(Name = "request", Order = 1)]
        public T? Request { get; init; }
    }
}

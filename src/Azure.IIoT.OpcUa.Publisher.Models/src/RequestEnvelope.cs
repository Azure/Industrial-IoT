// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Wraps a request and a connection to bind to a
    /// body more easily for api that requires a
    /// connection endpoint
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public record class RequestEnvelope<T>
    {
        /// <summary>
        /// Connection the request is targeting
        /// </summary>
        [DataMember(Name = "connection", Order = 0)]
        [Required]
        public required ConnectionModel Connection { get; set; }

        /// <summary>
        /// Request
        /// </summary>
        [DataMember(Name = "request", Order = 1)]
        public T? Request { get; set; }
    }
}

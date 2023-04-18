﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Disconnect request
    /// </summary>
    [DataContract]
    public sealed record class DisconnectRequestModel
    {
        /// <summary>
        /// This handle can be used to disconnect the
        /// connection ahead of expiration.
        /// </summary>
        [DataMember(Name = "connectionHandle", Order = 0)]
        [Required]
        public string? ConnectionHandle { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 4,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}

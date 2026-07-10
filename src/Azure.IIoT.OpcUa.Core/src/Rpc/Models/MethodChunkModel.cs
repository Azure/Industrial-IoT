// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Method invocation messaging model. The wire property names below match the
    /// former Legacy <c>MethodChunkModel</c> exactly so that the direct-method
    /// chunking protocol stays wire compatible with existing SDK clients and the
    /// service side.
    /// </summary>
    [DataContract]
    public sealed class MethodChunkModel
    {
        /// <summary>
        /// Invocation handle - null on first request
        /// and last response, assigned by server for the
        /// duration of the invocation.
        /// </summary>
        [DataMember(Name = "handle", Order = 0,
            EmitDefaultValue = false)]
        [JsonPropertyName("handle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Handle { get; set; }

        /// <summary>
        /// Real method name to call - only needed on
        /// first request
        /// </summary>
        [DataMember(Name = "method", Order = 1,
            EmitDefaultValue = false)]
        [JsonPropertyName("method")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MethodName { get; set; }

        /// <summary>
        /// Content type of payload object for anything
        /// other than application/json.  Only send in
        /// first request and first response.
        /// </summary>
        [DataMember(Name = "contentType", Order = 2,
            EmitDefaultValue = false)]
        [JsonPropertyName("contentType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Total Content length to be sent.  Sent in
        /// first request and first response.
        /// </summary>
        [DataMember(Name = "length", Order = 3,
            EmitDefaultValue = false)]
        [JsonPropertyName("length")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ContentLength { get; set; }

        /// <summary>
        /// Payload chunk or null for upload responses and
        /// response continuation requests.
        /// </summary>
        [DataMember(Name = "payload", Order = 4,
            EmitDefaultValue = false)]
        [JsonPropertyName("payload")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public byte[]? Payload { get; set; }

        /// <summary>
        /// Status code of call - in first response chunk.
        /// </summary>
        [DataMember(Name = "status", Order = 5,
            EmitDefaultValue = false)]
        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Status { get; set; }

        /// <summary>
        /// Timeout of the operation on the server sent in
        /// first request.
        /// </summary>
        [DataMember(Name = "timeout", Order = 6,
            EmitDefaultValue = false)]
        [JsonPropertyName("timeout")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Client accepted max chunk length sent in first
        /// request by client.
        /// </summary>
        [DataMember(Name = "acceptedSize", Order = 7,
            EmitDefaultValue = false)]
        [JsonPropertyName("acceptedSize")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxChunkLength { get; set; }

        /// <summary>
        /// Message properties or none if not set
        /// </summary>
        [DataMember(Name = "properties", Order = 8,
            EmitDefaultValue = false)]
        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, string>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}

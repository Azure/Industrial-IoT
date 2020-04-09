// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Method invocation messaging model.
    /// </summary>
    [DataContract]
    public class MethodChunkModel {

        /// <summary>
        /// Invocation handle - null on first request
        /// and last response, assigned by server for the
        /// duration of the invocation.
        /// </summary>
        [DataMember(Name = "handle", Order = 0,
            EmitDefaultValue = false)]
        public string Handle { get; set; }

        /// <summary>
        /// Real method name to call - only needed on
        /// first request
        /// </summary>
        [DataMember(Name = "method", Order = 1,
            EmitDefaultValue = false)]
        public string MethodName { get; set; }

        /// <summary>
        /// Content type of payload object for anything
        /// other than application/json.  Only send in
        /// first request and first response.
        /// </summary>
        [DataMember(Name = "contentType", Order = 2,
            EmitDefaultValue = false)]
        public string ContentType { get; set; }

        /// <summary>
        /// Total Content length to be sent.  Sent in
        /// first request and first response.
        /// </summary>
        [DataMember(Name = "length", Order = 3,
            EmitDefaultValue = false)]
        public int? ContentLength { get; set; }

        /// <summary>
        /// Payload chunk or null for upload responses and
        /// response continuation requests.
        /// </summary>
        [DataMember(Name = "payload", Order = 4,
            EmitDefaultValue = false)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Status code of call - in first response chunk.
        /// </summary>
        [DataMember(Name = "status", Order = 5,
            EmitDefaultValue = false)]
        public int? Status { get; set; }

        /// <summary>
        /// Timeout of the operation on the server sent in
        /// first request.
        /// </summary>
        [DataMember(Name = "timeout", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Client accepted max chunk length sent in first
        /// request by client.
        /// </summary>
        [DataMember(Name = "acceptedSize", Order = 7,
            EmitDefaultValue = false)]
        public int? MaxChunkLength { get; set; }
    }
}

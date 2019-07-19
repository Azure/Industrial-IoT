// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Method invocation messaging model.
    /// </summary>
    public class MethodChunkModel {

        /// <summary>
        /// Invocation handle - null on first request
        /// and last response, assigned by server for the
        /// duration of the invocation.
        /// </summary>
        [JsonProperty(PropertyName = "handle",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Handle { get; set; }

        /// <summary>
        /// Real method name to call - only needed on
        /// first request
        /// </summary>
        [JsonProperty(PropertyName = "method",
            NullValueHandling = NullValueHandling.Ignore)]
        public string MethodName { get; set; }

        /// <summary>
        /// Content type of payload object for anything
        /// other than application/json.  Only send in
        /// first request and first response.
        /// </summary>
        [JsonProperty(PropertyName = "contentType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContentType { get; set; }

        /// <summary>
        /// Total Content length to be sent.  Sent in
        /// first request and first response.
        /// </summary>
        [JsonProperty(PropertyName = "length",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? ContentLength { get; set; }

        /// <summary>
        /// Payload chunk or null for upload responses and
        /// response continuation requests.
        /// </summary>
        [JsonProperty(PropertyName = "payload",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Status code of call - in first response chunk.
        /// </summary>
        [JsonProperty(PropertyName = "status",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? Status { get; set; }

        /// <summary>
        /// Timeout of the operation on the server sent in
        /// first request.
        /// </summary>
        [JsonProperty(PropertyName = "timeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Client accepted max chunk length sent in first
        /// request by client.
        /// </summary>
        [JsonProperty(PropertyName = "acceptedSize",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxChunkLength { get; set; }
    }
}

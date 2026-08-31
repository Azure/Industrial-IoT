// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    /// <summary>
    /// Http options
    /// </summary>
    public class HttpEventClientOptions
    {
        /// <summary>
        /// Host name. If not specified the first part of the
        /// topic is used as host name to
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Port if different from the default scheme port.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Use multipart messages for single buffers also.
        /// For multiple buffers multi part messages are
        /// always used.
        /// </summary>
        public bool? UseMultipartForSingleBuffer { get; set; }

        /// <summary>
        /// Use PUT instead of POST requests which is the default.
        /// </summary>
        public bool? UseHttpPutMethod { get; set; }

        /// <summary>
        /// Simple authorization header to add to each request
        /// when secure scheme is used.
        /// </summary>
        public string? AuthorizationHeader { get; set; }

        /// <summary>
        /// Use http instead of https scheme. Http is insecure
        /// so no authorization header is added.
        /// </summary>
        public bool? UseHttpScheme { get; set; }

        /// <summary>
        /// Configure headers to add additional information to
        /// each request. This is called independent of scheme
        /// used.
        /// </summary>
        public Func<HttpRequestHeaders, Task>? Configure { get; set; }
    }
}

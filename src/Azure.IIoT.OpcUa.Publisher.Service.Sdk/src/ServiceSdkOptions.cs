// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration for service api
    /// </summary>
    public sealed class ServiceSdkOptions
    {
        /// <summary>
        /// Web service url
        /// </summary>
        public string? ServiceUrl { get; set; }

        /// <summary>
        /// Provider
        /// </summary>
        public Func<Task<string?>>? TokenProvider { get; set; }

        /// <summary>
        /// Use message pack or json
        /// </summary>
        public bool UseMessagePackProtocol { get; set; }

        /// <summary>
        /// Insert a message handler
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler>? HttpMessageHandler { get; set; }
    }
}

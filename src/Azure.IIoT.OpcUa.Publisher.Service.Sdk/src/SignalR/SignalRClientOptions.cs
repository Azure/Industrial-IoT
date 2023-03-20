// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Signalr client options
    /// </summary>
    public sealed class SignalRClientOptions
    {
        /// <summary>
        /// Use message pack or json
        /// </summary>
        public bool UseMessagePackProtocol { get; set; }

        /// <summary>
        /// Provider
        /// </summary>
        public Func<Task<string>> TokenProvider { get; set; }

        /// <summary>
        /// Insert the message handler
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler> HttpMessageHandler { get; set; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Access the publisher module via its device method interface.
    /// (V2 functionality)
    /// </summary>
    public sealed class PublisherMethodClient : IPublisherClient {

        /// <summary>
        /// Create method server that presumes the publisher module is named
        /// "publisher" and resides on the same gateway device as the one in
        /// the passed in identity.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public PublisherMethodClient(IJsonMethodClient client, IIdentity identity,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="logger"></param>
        public PublisherMethodClient(IJsonMethodClient client, string deviceId,
            string moduleId, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <inheritdoc/>
        public async Task<(ServiceResultModel, string)> CallMethodAsync(
            string method, string request, DiagnosticsModel diagnostics) {
            try {
                var response = await _client.CallMethodAsync(
                    _deviceId ?? _identity?.DeviceId, _moduleId ?? "publisher",
                    method, request);
                return (null, response);
            }
            catch (Exception ex) {
                return (new ServiceResultModel {
                    ErrorMessage = ex.Message,
                    Diagnostics = JToken.FromObject(ex,
                        JsonSerializer.Create(JsonConvertEx.GetSettings()))
                }, null);
            }
        }

        private readonly IJsonMethodClient _client;
        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;
    }
}

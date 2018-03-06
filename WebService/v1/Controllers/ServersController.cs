// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Servers controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.RegisterTwins)]
    public class ServersController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="servers"></param>
        public ServersController(IOpcUaServerRegistry servers) {
            _servers = servers;
        }

        /// <summary>
        /// Get all registered servers in paged form.
        /// </summary>
        /// <returns>
        /// List of servers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<ServerInfoListApiModel> ListAsync() {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME]
                    .FirstOrDefault();
            }
            var result = await _servers.ListServerInfosAsync(continuationToken);
            return new ServerInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the server data for the server identified by the
        /// specified server info model.
        /// </summary>
        /// <param name="model">Server info for the server</param>
        /// <returns>Server model</returns>
        [HttpPost]
        public async Task<ServerApiModel> FindAsync(ServerInfoApiModel model) {
            var result = await _servers.FindServerAsync(model.ToServiceModel());
            return new ServerApiModel(result);
        }

        /// <summary>
        /// Returns the server data for the server identified by the
        /// specified identifier.
        /// </summary>
        /// <param name="id">Server id for the server</param>
        /// <returns>Server model</returns>
        [HttpGet("{id}")]
        public async Task<ServerApiModel> GetAsync(string id) {
            var result = await _servers.GetServerAsync(id);
            return new ServerApiModel(result);
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaServerRegistry _servers;
    }
}
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The status service.
    /// </summary>
    [ExceptionsFilter]
    [Route(VersionInfo.PATH + "/status")]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanRead)]
    public class StatusController : Controller {

        /// <summary>
        /// Create the controller
        /// </summary>
        /// <param name="certificateGroups"></param>
        /// <param name="process"></param>
        /// <param name="logger"></param>
        public StatusController(
            ITrustGroupStore certificateGroups, IProcessIdentity process,
            ILogger logger) {
            _certificateGroups = certificateGroups;
            _logger = logger;
            _process = process;
        }

        /// <summary>
        /// Get the status.
        /// </summary>
        [HttpGet]
        public async Task<StatusResponseApiModel> GetStatusAsync() {
            var kvOk = true;
            var kvMessage = "Alive and well";
            try {
                await _certificateGroups.ListGroupsAsync();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Service status error");
                kvOk = false;
                kvMessage = ex.Message;
            }
            return new StatusResponseApiModel(kvOk, kvMessage) {
                Name = _process.ServiceId
            };
        }

        private readonly ILogger _logger;
        private readonly IProcessIdentity _process;
        private readonly ITrustGroupStore _certificateGroups;
    }
}

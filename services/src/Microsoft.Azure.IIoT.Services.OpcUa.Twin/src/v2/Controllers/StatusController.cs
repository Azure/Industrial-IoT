// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Status checks
    /// </summary>
    [Route(VersionInfo.PATH + "/status")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    public class StatusController : Controller {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="process"></param>
        public StatusController(IProcessIdentity process) {
            _process = process;
        }

        /// <summary>
        /// Return the service status in the form of the service status
        /// api model.
        /// </summary>
        /// <returns>Status object</returns>
        [HttpGet]
        public StatusResponseApiModel GetStatus() {
            // TODO: check if the dependencies are healthy
            return new StatusResponseApiModel(true, "Alive and well") {
                Name = _process.ServiceId
            };
        }

        private readonly IProcessIdentity _process;
    }
}

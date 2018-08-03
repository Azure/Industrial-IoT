// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    [Route(VersionInfo.PATH + "/[controller]"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]

    public sealed class StatusController : Controller
    {
        private readonly ILogger log;

        public StatusController(ILogger logger)
        {
            this.log = logger;
        }

        [HttpGet]
        [SwaggerOperation(operationId: "GetStatus")]
        public StatusApiModel Get()
        {
            // TODO: calculate the actual service status
            var isOk = true;

            this.log.Info("Service status request", () => new { Healthy = isOk });
            return new StatusApiModel(isOk, "Alive and well");
        }
    }
}

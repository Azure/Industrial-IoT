// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;
    using Swashbuckle.AspNetCore.Annotations;
    using System;

    [Route(VersionInfo.PATH + "/[controller]"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]

    public sealed class StatusController : Controller
    {
        private readonly ILogger log;
        private readonly ICertificateGroup certificateGroups;
        private readonly IApplicationsDatabase applicationDatabase;

        public StatusController(
            IApplicationsDatabase applicationDatabase,
            ICertificateGroup certificateGroups,
            ILogger logger
            )
        {
            this.applicationDatabase = applicationDatabase;
            this.certificateGroups = certificateGroups;
            this.log = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation(OperationId = "GetStatus")]
        public async System.Threading.Tasks.Task<StatusApiModel> GetAsync()
        {
            // TODO: calculate the actual service status
            bool applicationOk;
            string applicationMessage = "Alive and well";
            try
            {
                var apps = await applicationDatabase.QueryApplicationsAsync(0, 1, null, null, 0, null, null);
                applicationOk = apps != null;
            }
            catch (Exception ex)
            {
                applicationOk = false;
                applicationMessage = ex.Message;
            }
            this.log.Info("Service status application database", () => new { Healthy = applicationOk, Message = applicationMessage });

            bool kvOk;
            string kvMessage = "Alive and well";
            try
            {
                var groups = await certificateGroups.GetCertificateGroupIds();
                kvOk = groups.Length > 0;
                kvMessage = String.Join(",", groups);
            }
            catch (Exception ex)
            {
                kvOk = false;
                kvMessage = ex.Message;
            }
            this.log.Info("Service status KeyVault", () => new { Healthy = kvOk, Message = kvMessage });

            return new StatusApiModel(applicationOk, applicationMessage, kvOk, kvMessage);
        }
    }
}

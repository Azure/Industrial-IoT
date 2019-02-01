// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    using System;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;

    /// <inheritdoc/>
    [ApiController]
    [Route(VersionInfo.PATH + "/[controller]"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class StatusController : Controller
    {
        private readonly ILogger _log;
        private readonly ICertificateGroup _certificateGroups;
        private readonly IApplicationsDatabase _applicationDatabase;

        /// <inheritdoc/>
        public StatusController(
            IApplicationsDatabase applicationDatabase,
            ICertificateGroup certificateGroups,
            ILogger logger
            )
        {
            this._applicationDatabase = applicationDatabase;
            this._certificateGroups = certificateGroups;
            this._log = logger;
        }

        /// <summary>
        /// returns the status
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async System.Threading.Tasks.Task<StatusApiModel> GetStatusAsync()
        {
            bool applicationOk;
            string applicationMessage = "Alive and well";
            try
            {
                var apps = await _applicationDatabase.QueryApplicationsAsync(0, 1, null, null, 0, null, null, true);
                applicationOk = apps != null;
            }
            catch (Exception ex)
            {
                applicationOk = false;
                applicationMessage = ex.Message;
            }
            this._log.Info("Service status application database", () => new { Healthy = applicationOk, Message = applicationMessage });

            bool kvOk;
            string kvMessage = "Alive and well";
            try
            {
                var groups = await _certificateGroups.GetCertificateGroupIds();
                kvOk = groups.Length > 0;
                kvMessage = String.Join(",", groups);
            }
            catch (Exception ex)
            {
                kvOk = false;
                kvMessage = ex.Message;
            }
            this._log.Info("Service status OpcVault", () => new { Healthy = kvOk, Message = kvMessage });

            return new StatusApiModel(applicationOk, applicationMessage, kvOk, kvMessage);
        }
    }
}

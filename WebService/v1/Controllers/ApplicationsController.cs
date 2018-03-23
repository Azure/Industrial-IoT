// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Controllers {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Auth;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Filters;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.v1.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Applications controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    public class ApplicationsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="applications"></param>
        public ApplicationsController(IOpcUaApplicationRegistry applications) {
            _applications = applications;
        }

        /// <summary>
        /// Register new server in the application registry
        /// </summary>
        /// <param name="request">Server registration request</param>
        /// <returns>Application registration response</returns>
        [HttpPost]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ApplicationRegistrationResponseApiModel> PostAsync(
            [FromBody] ServerRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _applications.RegisterAsync(
                request.ToServiceModel());
            return new ApplicationRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Register new application in the application registry
        /// using raw information from application info.
        /// </summary>
        /// <param name="request">Application registration request</param>
        /// <returns>Application registration response</returns>
        [HttpPut]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ApplicationRegistrationResponseApiModel> PutAsync(
            [FromBody] ApplicationRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _applications.RegisterAsync(
                request.ToServiceModel());
            return new ApplicationRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Update existing application. Note that Id field in request
        /// must not be null and application registration must exist.
        /// </summary>
        /// <param name="request">Twin update request</param>
        [HttpPatch]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task PatchAsync(
            [FromBody] ApplicationRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _applications.UpdateApplicationAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Delete application by application identifier.
        /// </summary>
        /// <param name="id">The identifier of the application</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task DeleteAsync(string id) {
            await _applications.UnregisterApplicationAsync(id);
        }

        /// <summary>
        /// Get all registered applications in paged form.
        /// </summary>
        /// <returns>
        /// List of servers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<ApplicationInfoListApiModel> ListAsync() {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME]
                    .FirstOrDefault();
            }
            var result = await _applications.ListApplicationsAsync(continuationToken);

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the applications for the information in the
        /// specified application query info model.
        /// </summary>
        /// <param name="query">Application query</param>
        /// <returns>Applications</returns>
        [HttpPost("query")]
        public async Task<ApplicationInfoListApiModel> FindAsync(
            [FromBody] ApplicationRegistrationQueryApiModel query) {
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel());

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Query using Uri query specification.
        /// </summary>
        /// <param name="query">Application Query</param>
        /// <returns>Applications</returns>
        [HttpGet("query")]
        public async Task<ApplicationInfoListApiModel> QueryAsync(
            [FromQuery] ApplicationRegistrationQueryApiModel query) {
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel());

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the application data for the application identified by the
        /// specified application identifier.
        /// </summary>
        /// <param name="id">Application id for the server</param>
        /// <returns>Application model</returns>
        [HttpGet("{id}")]
        public async Task<ApplicationRegistrationApiModel> GetAsync(string id) {
            var result = await _applications.GetApplicationAsync(id);

            // TODO: Filter results based on RBAC

            return new ApplicationRegistrationApiModel(result);
        }

        /// <summary>
        /// The returned application certificate allows a user to determine
        /// whether endpoints should be trusted.
        /// </summary>
        /// <param name="id">Endpoint identifier</param>
        /// <returns>Public certificate of the server</returns>
        [HttpGet("{id}/certificate")]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ActionResult> GetCertificateAsync(string id) {
            var result = await _applications.GetApplicationAsync(id);
            if (result == null) {
                throw new ResourceNotFoundException($"Application {id} not found.");
            }
            var certificate = new X509Certificate2(result.Application.Certificate);
            if (certificate == null) {
                throw new ResourceNotFoundException($"Certificate not available in {id}");
            }

            return File(certificate.GetRawCertData(),
                MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaApplicationRegistry _applications;
    }
}
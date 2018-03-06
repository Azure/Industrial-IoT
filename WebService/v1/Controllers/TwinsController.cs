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
    /// Twins controller
    /// </summary>
    [Route(ServiceInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    [Authorize(Policy = Policy.BrowseTwins)]
    public class TwinsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="twins"></param>
        public TwinsController(IOpcUaTwinRegistry twins) {
            _twins = twins;
        }

        /// <summary>
        /// Returns the twin registration with the specified identifier.
        /// </summary>
        /// <param name="id">twin identifier</param>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <returns>Twin registration</returns>
        [HttpGet("{id}")]
        public async Task<TwinRegistrationApiModel> GetAsync(string id,
            [FromQuery] bool? onlyServerState) {
            var result = await _twins.GetTwinAsync(id, onlyServerState ?? false);

            // TODO: Redact username/token in twom based on policy/permission
            // TODO: Filter twins based on RBAC

            return new TwinRegistrationApiModel(result);
        }

        /// <summary>
        /// Get all registered twins in paged form.
        /// </summary>
        /// <param name="onlyServerState">Whether to include only server
        /// state, or display current client state of the twin if
        /// available</param>
        /// <returns>
        /// List of twins and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<TwinRegistrationListApiModel> ListAsync(
            [FromQuery] bool? onlyServerState) {
            string continuationToken = null;
            if (Request.Headers.ContainsKey(CONTINUATION_TOKEN_NAME)) {
                continuationToken = Request.Headers[CONTINUATION_TOKEN_NAME].FirstOrDefault();
            }
            var result = await _twins.ListTwinsAsync(continuationToken,
                onlyServerState ?? false);

            // TODO: Redact username/token/certificates based on policy/permission
            // TODO: Filter twins based on RBAC

            return new TwinRegistrationListApiModel(result);
        }

        /// <summary>
        /// Register new twin in the twin registry
        /// </summary>
        /// <param name="request">Twin registration request</param>
        /// <returns>Twin registration response</returns>
        [HttpPost]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<TwinRegistrationResponseApiModel> RegisterAsync(
            [FromBody]TwinRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: if token type is not "none", but user/token not, take from current claims

            var result = await _twins.RegisterTwinAsync(request.ToServiceModel());
            return new TwinRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Update existing twin. Note that Id field in request
        /// must not be null and twin registration must exist.
        /// </summary>
        /// <param name="request">Twin update request</param>
        [HttpPatch]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task PatchAsync(
            [FromBody] TwinRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // TODO: permissions

            await _twins.UpdateTwinAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Delete twin by twin identifier.
        /// </summary>
        /// <param name="id">The identifier of the twin</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task DeleteAsync(string id) {

            // TODO: permissions

            await _twins.DeleteTwinAsync(id);
        }

        /// <summary>
        /// Downloads a file with the public server certificate for the twin.
        /// This allows a user to inspect the certificate to see if it can be trusted.
        /// If the user trusts the certificate the twin can be patched with the
        /// IsTrusted property set to true.
        /// </summary>
        /// <param name="id">Endpoint identifier</param>
        /// <returns>Public certificate of the server</returns>
        [HttpGet("{id}/certificate/server")]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ActionResult> GetServerCertificateAsync(string id) {

            // TODO: permissions

            var twin = await _twins.GetTwinAsync(id, false);
            if (twin == null) {
                throw new ResourceNotFoundException($"Twin {id} not found.");
            }
            var certificate = new X509Certificate2(twin.Server.ServerCertificate);
            if (certificate == null) {
                throw new ResourceNotFoundException($"Certificate not available in {id}");
            }

            return File(certificate.GetRawCertData(),
                MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
        }

        private const string CONTINUATION_TOKEN_NAME = "x-ms-continuation";
        private readonly IOpcUaTwinRegistry _twins;
    }
}